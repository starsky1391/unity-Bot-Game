#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace HollowDemo
{
    public sealed class HudIconsSmokeProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            SaveStore.testPath=System.IO.Path.GetFullPath("Temp/HudIcons/progress.json");var game=DemoGame.Instance;
            while(game.Activation==null)yield return null;
            game.NewGame();yield return new WaitForSecondsRealtime(.6f);
            var player=game.Player;player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
            var hud=FindObjectOfType<PlayerHudCanvas>();
            if(hud.healthSquares.Any(i=>i.sprite==null || i.sprite.name!="coin" || !i.preserveAspect))throw new Exception("HUD: coin sprite binding");
            game.EquippedItems[0]=Resources.Load<ItemDefinition>("Items/flask");game.EquippedItems[1]=Resources.Load<ItemDefinition>("Items/mana_flask");
            yield return null;yield return null;
            if(!hud.itemIcons.Any(i=>i.sprite!=null && i.sprite.name=="HPbottle") || !hud.itemIcons.Any(i=>i.sprite!=null && i.sprite.name=="MPbottle"))throw new Exception("HUD: bottle icon binding");
            yield return new WaitForSeconds(1.1f);
            player.Damage(1,player.transform.position);yield return null;yield return null;
            var lost=hud.healthSquares[player.Health];
            if(lost.material!=hud.emptyHealthMaterial || lost.material.shader.name!="HollowDemo/UI Health Grayscale")throw new Exception("HUD: missing health grayscale");
            player.Heal(1);yield return null;yield return null;
            if(lost.material.shader.name=="HollowDemo/UI Health Grayscale" || lost.color!=Color.white)throw new Exception("HUD: recovered health original color");
            Debug.Log("DEMO_SMOKE_OK: health coin, gray lost health and flask icons");
        }
    }
}
#endif
