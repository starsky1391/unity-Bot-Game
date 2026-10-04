using System.IO;
using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class WorldLabelAuthoring
{
    const string PrefabPath="Assets/Prefabs/UI/ObjectName.prefab";
    [MenuItem("洞穴 Demo/文字名称跟随对象")]
    public static void Configure()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        var world=Object.FindObjectOfType<WorldMap>();
        foreach(var old in world.GetComponentsInChildren<WorldLabel>(true))
            if(old.label==null)
            {
                while(old.transform.childCount>0) old.transform.GetChild(0).SetParent(old.transform.parent,true);
                Object.DestroyImmediate(old.gameObject);
            }
        foreach(var group in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="文字标记" && t.childCount==0).ToArray())
            Object.DestroyImmediate(group.gameObject);
        foreach(var path in new[]{"Assets/Prefabs/NPCs/Guide.prefab","Assets/Prefabs/NPCs/Merchant.prefab","Assets/Prefabs/World/Checkpoint.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            var npc=root.GetComponent<Npc>();
            Attach(root.transform,npc==null?"检查点":npc.displayName);
            PrefabUtility.SaveAsPrefabAsset(root,path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        foreach(var npc in world.GetComponentsInChildren<Npc>(true)) Attach(npc.transform,npc.displayName);
        foreach(var checkpoint in world.GetComponentsInChildren<Checkpoint>(true)) Attach(checkpoint.transform,"检查点");
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("DEMO_OBJECT_NAMES_OK: editable Canvas names under NPCs/checkpoints; standalone terrain labels removed.");
    }
    public static void Attach(Transform owner,string caption)
    {
        var current=owner.GetComponentInChildren<WorldLabel>(true);
        if(current!=null){current.text=caption;current.label.text=caption;EditorUtility.SetDirty(current);return;}
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if(prefab==null)
        {
            Directory.CreateDirectory("Assets/Prefabs/UI");
            var root=new GameObject("对象名称",typeof(RectTransform),typeof(Canvas),typeof(WorldLabel));
            root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            root.GetComponent<Canvas>().sortingOrder=10;
            root.GetComponent<RectTransform>().sizeDelta=new Vector2(250,40);
            root.transform.localScale=Vector3.one*.01f;
            var go=new GameObject("文字",typeof(RectTransform),typeof(Text));
            go.transform.SetParent(root.transform,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var label=go.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=24;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.5f,.73f,.8f);label.raycastTarget=false;
            root.GetComponent<WorldLabel>().label=label;
            prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            Object.DestroyImmediate(root);
        }
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,owner);
        instance.name="名称";
        instance.transform.localPosition=new Vector3(0,1.7f/Mathf.Abs(owner.lossyScale.y),0);
        instance.transform.localScale=new Vector3(.01f/Mathf.Abs(owner.lossyScale.x),.01f/Mathf.Abs(owner.lossyScale.y),.01f);
        var component=instance.GetComponent<WorldLabel>();component.text=caption;component.label.text=caption;
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        PrefabUtility.RecordPrefabInstancePropertyModifications(component.label);
    }
}


