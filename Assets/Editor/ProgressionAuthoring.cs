using System.IO;
using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class ProgressionAuthoring
{
 public static void Configure(){
 Directory.CreateDirectory("Assets/Resources/Settings");
 var flask=Item("flask","血瓶","检查点补满的循环血瓶",ItemKind.RefillableFlask,2);flask.stackLimit=1;flask.retainWhenEmpty=true;
 var empty=Item("empty_flask","空白容器","检查点互动时增加一格血瓶容量",ItemKind.EmptyFlask,0);
 var crystal=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Resources/Items/crystal.asset");crystal.kind=ItemKind.Currency;EditorUtility.SetDirty(crystal);
 const string settings="Assets/Resources/Settings/CrystalDrops.asset";
 var drops=AssetDatabase.LoadAssetAtPath<CrystalDropSettings>(settings);if(drops==null){drops=ScriptableObject.CreateInstance<CrystalDropSettings>();AssetDatabase.CreateAsset(drops,settings);}
 foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Enemies","Assets/Prefabs/Bosses"}).Select(AssetDatabase.GUIDToAssetPath)){
 var root=PrefabUtility.LoadPrefabContents(path);foreach(var enemy in root.GetComponentsInChildren<EnemyBrain>(true))enemy.crystalDrops=drops;PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
 const string hudPath="Assets/Prefabs/UI/PlayerHUD.prefab";var hudRoot=PrefabUtility.LoadPrefabContents(hudPath);var hud=hudRoot.GetComponent<PlayerHudCanvas>();
 if(hud.crystalPanel==null){var panel=new GameObject("晶石栏",typeof(RectTransform));panel.transform.SetParent(hudRoot.transform,false);var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-28,-28);rect.sizeDelta=new Vector2(150,42);hud.crystalPanel=panel;
 var icon=new GameObject("晶石图标",typeof(RectTransform),typeof(Image));icon.transform.SetParent(panel.transform,false);var ir=icon.GetComponent<RectTransform>();ir.anchoredPosition=new Vector2(-50,0);ir.sizeDelta=new Vector2(20,20);ir.localRotation=Quaternion.Euler(0,0,45);var image=icon.GetComponent<Image>();image.color=new Color(.75f,.5f,1);image.raycastTarget=false;
 var count=new GameObject("晶石数量",typeof(RectTransform),typeof(Text));count.transform.SetParent(panel.transform,false);var cr=count.GetComponent<RectTransform>();cr.anchoredPosition=new Vector2(20,0);cr.sizeDelta=new Vector2(100,42);hud.crystalCount=count.GetComponent<Text>();hud.crystalCount.font=ChineseFont.Shared;hud.crystalCount.fontSize=24;hud.crystalCount.alignment=TextAnchor.MiddleLeft;hud.crystalCount.text="0";hud.crystalCount.raycastTarget=false;}
 PrefabUtility.SaveAsPrefabAsset(hudRoot,hudPath);PrefabUtility.UnloadPrefabContents(hudRoot);
 var pickup=PrefabUtility.LoadPrefabContents("Assets/Prefabs/World/PickupPotion.prefab");pickup.name="空白容器";pickup.GetComponent<Pickup>().item=empty;pickup.GetComponent<Pickup>().count=1;pickup.GetComponent<Pickup>().persistentId="";pickup.GetComponent<SpriteRenderer>().color=new Color(.8f,.9f,1);PrefabUtility.SaveAsPrefabAsset(pickup,"Assets/Prefabs/World/PickupEmptyFlask.prefab");PrefabUtility.UnloadPrefabContents(pickup);
 var water=new GameObject("伤害水池",typeof(SpriteRenderer),typeof(DamageWater));water.transform.localScale=new Vector3(5,1,1);water.GetComponent<BoxCollider2D>().isTrigger=true;water.GetComponent<BoxCollider2D>().size=Vector2.one;var sr=water.GetComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Square.png");sr.color=new Color(.2f,.6f,.9f,.45f);PrefabUtility.SaveAsPrefabAsset(water,"Assets/Prefabs/World/DamageWater.prefab");Object.DestroyImmediate(water);
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");var game=Object.FindObjectOfType<DemoGame>();game.flaskItem=flask;
 foreach(var enemy in Object.FindObjectsOfType<EnemyBrain>(true)){enemy.crystalDrops=drops;PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);}
 var sceneHud=Object.FindObjectOfType<PlayerHudCanvas>(true);sceneHud.crystalPanel=sceneHud.transform.Find("晶石栏").gameObject;sceneHud.crystalCount=sceneHud.crystalPanel.GetComponentInChildren<Text>();PrefabUtility.RecordPrefabInstancePropertyModifications(sceneHud);
 var world=Object.FindObjectOfType<WorldMap>();Physics2D.SyncTransforms();if(!world.GetComponentsInChildren<Pickup>(true).Any(p=>p.persistentId=="empty_flask_demo_1")){
 var start=world.startPoint.position;var platform=world.GetComponentsInChildren<GroundSurface>().Select(g=>g.GetComponent<Collider2D>()).Where(c=>c.bounds.center.x>start.x+4&&c.bounds.center.x<start.x+22&&c.bounds.size.x>2&&c.bounds.size.x<8&&c.bounds.max.y>start.y+1&&c.bounds.max.y<start.y+7).OrderBy(c=>c.bounds.max.y).First();var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/PickupEmptyFlask.prefab"),world.transform);obj.transform.position=new Vector3(platform.bounds.center.x+.5f,platform.bounds.max.y+.6f);obj.GetComponent<Pickup>().persistentId="empty_flask_demo_1";PrefabUtility.RecordPrefabInstancePropertyModifications(obj.GetComponent<Pickup>());}
 EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("DEMO_PROGRESSION_AUTHORING_OK");}
 static ItemDefinition Item(string id,string name,string description,ItemKind kind,int healing){string path="Assets/Resources/Items/"+id+".asset";var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);if(item==null){item=ScriptableObject.CreateInstance<ItemDefinition>();item.id=id;item.displayName=name;item.description=description;item.kind=kind;item.healing=healing;item.color=new Color(.8f,.9f,1);AssetDatabase.CreateAsset(item,path);}EditorUtility.SetDirty(item);return item;}
}
