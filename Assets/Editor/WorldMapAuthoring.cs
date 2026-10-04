using System.Collections.Generic;
using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WorldMapAuthoring
{
    [MenuItem("洞穴 Demo/合并为整体地图")]
    public static void Convert()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        foreach(var ui in Object.FindObjectsOfType<DemoUI>())
        { var label=ui.transform.Find("房间信息"); if(label!=null) Object.DestroyImmediate(label.gameObject); }
        var prefabPath="Assets/Prefabs/UI/GameInterface.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)!=null)
        { var prefab=PrefabUtility.LoadPrefabContents(prefabPath); var label=prefab.transform.Find("房间信息"); if(label!=null) Object.DestroyImmediate(label.gameObject); PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath); PrefabUtility.UnloadPrefabContents(prefab); }
        if (Object.FindObjectOfType<WorldMap>() != null) { EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); return; }
        var definitions = Object.FindObjectsOfType<RoomVolume>().OrderBy(r => r.id).ToArray();
        var world = new GameObject("整体地图").AddComponent<WorldMap>();
        var regions = new List<WorldMap.LegacyFogRegion>();
        var roofs = new List<Vector2>();
        float end = 0, baseline = 0, bottom = 0;
        foreach (var definition in definitions)
        {
            var oldScene = EditorSceneManager.OpenScene(definition.scenePath, OpenSceneMode.Additive);
            var room = oldScene.GetRootGameObjects().Single().GetComponent<RoomContent>();
            var grounds = room.GetComponentsInChildren<GroundSurface>();
            Physics2D.SyncTransforms();
            var bounds = grounds.Select(g => g.GetComponent<Collider2D>().bounds).ToArray();
            float left = bounds.Min(b => b.min.x), right = bounds.Max(b => b.max.x);
            float floor = bounds.Where(b => b.size.x > 2 && b.size.x > b.size.y).Min(b => b.max.y);
            float ceiling = bounds.Max(b => b.max.y);
            if (definition.id == 0)
            {
                baseline = floor;
                world.transform.position = new Vector3(left, floor);
                end = left;
            }
            float newLeft = definition.id == 0 ? left : end + 3;
            room.transform.position += new Vector3(newLeft - left, baseline - floor);
            SceneManager.MoveGameObjectToScene(room.gameObject, scene);
            room.transform.SetParent(world.transform, true);
            room.name = "区域" + (definition.id + 1);
            Physics2D.SyncTransforms();
            bounds = grounds.Select(g => g.GetComponent<Collider2D>().bounds).ToArray();
            float localLeft = newLeft - world.transform.position.x;
            float localRight = localLeft + right - left;
            if (definition.id > 0)
            {
                Bridge(world.transform, (end + newLeft) * .5f, baseline, 3);
                roofs.Add(new Vector2(localLeft - 3, 3));
                roofs.Add(new Vector2(localLeft, 3));
            }
            var upper = bounds.Where(b => b.size.x >= 2 && b.min.y > baseline + 3 && b.max.y >= baseline + ceiling - floor - 3).ToArray();
            var cuts = upper.SelectMany(b => new[] { b.min.x, b.max.x }).Concat(new[] { newLeft, newLeft + right-left }).Distinct().OrderBy(x => x).ToArray();
            for (int i=0; i<cuts.Length-1; i++)
            {
                float mid = (cuts[i]+cuts[i+1])*.5f;
                float height = upper.Where(b => mid >= b.min.x && mid <= b.max.x).Select(b => b.min.y-baseline).DefaultIfEmpty(3).Min();
                roofs.Add(new Vector2(cuts[i]-world.transform.position.x,height));
                roofs.Add(new Vector2(cuts[i+1]-world.transform.position.x,height));
            }
            if (definition.id == 0)
            {
                world.startPoint = room.startEntrance.transform;
                world.startPoint.name = "起点";
            }
            if (room.finish != null) world.finish = room.finish;
            foreach (var entrance in room.GetComponentsInChildren<RoomEntrance>(true))
                if (entrance.transform == world.startPoint) Object.DestroyImmediate(entrance);
                else Object.DestroyImmediate(entrance.gameObject);
            foreach (var exit in room.GetComponentsInChildren<RoomExit>(true)) Object.DestroyImmediate(exit.gameObject);
            Object.DestroyImmediate(room.cameraMin.gameObject);
            Object.DestroyImmediate(room.cameraMax.gameObject);
            foreach (var ground in grounds)
            {
                var b = ground.GetComponent<Collider2D>().bounds;
                bool seam = (definition.id > 0 && b.min.x <= newLeft+.1f) || (definition.id < definitions.Length-1 && b.max.x >= newLeft+right-left-.1f);
                if (seam && b.size.x < 2 && b.min.y < baseline+3 && b.max.y > baseline+.1f)
                {
                    float top = b.max.y;
                    if(top <= baseline+3) Object.DestroyImmediate(ground.gameObject);
                    else { ground.transform.position = new Vector3(b.center.x,(top+baseline+3)*.5f); ground.transform.localScale = new Vector3(b.size.x,top-baseline-3,1); }
                }
            }
            var oldMin = new Vector2(definition.map.outline.Min(p=>p.x),definition.map.outline.Min(p=>p.y));
            var oldMax = new Vector2(definition.map.outline.Max(p=>p.x),definition.map.outline.Max(p=>p.y));
            regions.Add(new WorldMap.LegacyFogRegion { id=definition.id,oldBounds=Rect.MinMaxRect(oldMin.x,oldMin.y,oldMax.x,oldMax.y),mapBounds=new Rect(localLeft*world.mapScale,0,(right-left)*world.mapScale,(ceiling-floor)*world.mapScale) });
            bottom = Mathf.Min(bottom,bounds.Min(b=>b.min.y)-baseline);
            end = newLeft+right-left;
            Object.DestroyImmediate(room);
            EditorSceneManager.CloseScene(oldScene,true);
        }
        world.legacyFogRegions=regions.ToArray();
        var polygon = new List<Vector2>{Vector2.zero,new Vector2(end-world.transform.position.x,0)};
        roofs.Reverse(); polygon.AddRange(roofs); world.outline=polygon.ToArray();
        world.fallBoundary=new GameObject("坠落死亡线").transform;
        world.fallBoundary.SetParent(world.transform,false); world.fallBoundary.localPosition=new Vector3(0,bottom-8);
        Physics2D.SyncTransforms();
        if(!world.TryGetSpawn(world.startPoint,out var spawn))
        {
            world.startPoint.position=world.GetComponentsInChildren<Checkpoint>().First().spawnPoint.position;
            if(!world.TryGetSpawn(world.startPoint,out spawn)) throw new System.Exception("起点下方没有地面");
        }
        world.startPoint.position=spawn;
        foreach(var definition in definitions) Object.DestroyImmediate(definition.gameObject);
        SceneManager.SetActiveScene(scene);
        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scene.path,true)};
        AssetDatabase.SaveAssets();
        Debug.Log("DEMO_WORLD_MAP_OK: continuous map, relative spawn and fog; IDs preserved.");
    }
    static void Bridge(Transform parent,float x,float floor,float width)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Ground.prefab");
        foreach(float y in new[]{floor-.5f,floor+3.5f})
        {var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name="连接通道";go.transform.SetParent(parent,true);go.transform.position=new Vector3(x,y);go.transform.localScale=new Vector3(width,1,1);}
    }
}

