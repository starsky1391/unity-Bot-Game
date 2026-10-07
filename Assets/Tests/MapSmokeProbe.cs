#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace HollowDemo
{
    public sealed class MapSmokeProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            SaveStore.testPath=System.IO.Path.GetFullPath("Temp/MapSmoke/progress.json");var game=DemoGame.Instance;
            while(game.Activation==null)yield return null;
            var platform=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/DropPlatform.prefab"),new Vector3(10000,10000),Quaternion.identity);
            game.NewGame();yield return new WaitForSecondsRealtime(.6f);
            var world=game.World;var point=world.MapPosition(game.Player.transform.position);
            int visible=world.GroundRects.Sum(r=>game.MapFog.VisibleRects(0,r).Count());
            if(world.GroundRects.Any(r=>r.Contains(world.MapPosition(platform.transform.position))))throw new Exception("MAP: drop platform included");
            var terrainCount=world.GroundRects.Count;
            game.Activation.ActivateAt(game.Player.transform.position);
            if(world.GroundRects.Count!=terrainCount)throw new Exception("MAP: culling removed map ground");
            foreach(var rect in world.GroundRects)
                foreach(var area in game.MapFog.VisibleRects(0,rect))
                    if(!game.MapFog.Visible(0,area.center))throw new Exception("MAP: fog leaked hidden ground");
            game.SetScreen(GameScreen.Map);yield return null;yield return null;
            var graphic=FindObjectOfType<MapCanvasGraphic>();
            if(graphic==null || !graphic.isActiveAndEnabled)throw new Exception("MAP: graphic missing or inactive");
            if(!game.MapFog.Visible(0,point))throw new Exception("MAP: player fog missing");
            if(visible==0)throw new Exception("MAP: no visible Ground near spawn");
            Debug.Log("DEMO_SMOKE_OK: Ground map, fog clipping, platform exclusion and culling");
        }
    }
}
#endif
