using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoomAuthoring
{
    public static void Configure()
    {
        var shell = EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        var definitions = Object.FindObjectsOfType<RoomVolume>().OrderBy(r => r.id).ToArray();
        float origin = 0;
        foreach (var definition in definitions)
        {
            definition.map.origin = new Vector2(origin, 0);
            origin += definition.map.outline.Max(p => p.x) - definition.map.outline.Min(p => p.x);
            EditorUtility.SetDirty(definition.map);
            var scene = EditorSceneManager.OpenScene(definition.scenePath, OpenSceneMode.Additive);
            Wrap(scene, definition);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }
        EditorSceneManager.SaveScene(shell);
        AssetDatabase.SaveAssets();
        Debug.Log("DEMO_ROOM_REFERENCES_OK: roots, named entrances, checkpoint spawns and camera anchors configured; persistence IDs preserved.");
    }

    public static void Wrap(Scene scene, RoomVolume definition)
    {
        if (scene.GetRootGameObjects().Any(r => r.GetComponent<RoomContent>())) return;
        var roots = scene.GetRootGameObjects();
        var root = new GameObject("Room" + definition.id);
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = new Vector2(definition.bounds.xMin, 0);
        foreach (var child in roots) child.transform.SetParent(root.transform, true);
        var content = root.AddComponent<RoomContent>();
        content.roomId = definition.id;
        content.cameraMin = Anchor(root.transform, "相机边界/左下", definition.bounds.min);
        content.cameraMax = Anchor(root.transform, "相机边界/右上", definition.bounds.max);
        content.checkpoint = root.GetComponentInChildren<Checkpoint>();
        if (content.checkpoint.spawnPoint == null)
            content.checkpoint.spawnPoint = Anchor(content.checkpoint.transform, "重生点", content.checkpoint.transform.position + Vector3.right * 1.5f);
        Entrance(root.transform, "LeftEntrance", new Vector2(definition.bounds.xMin + 1.8f, 1), 1);
        Entrance(root.transform, "RightEntrance", new Vector2(definition.bounds.xMax - 1.8f, 1), -1);
        if (definition.id == 0) Entrance(root.transform, "StartEntrance", new Vector2(0, 1), 1);
        if (definition.id == 4) content.finish = Anchor(root.transform, "终点", new Vector2(183, 1));
        foreach (var exit in root.GetComponentsInChildren<RoomExit>())
            exit.targetEntrance = exit.targetRoom > definition.id ? "LeftEntrance" : "RightEntrance";
        foreach (var npc in root.GetComponentsInChildren<Npc>())
            if (npc.shop != null) npc.mapLandmarkId = "merchant_arli";
    }

    static Transform Anchor(Transform parent, string name, Vector2 position)
    {
        var anchor = new GameObject(name).transform;
        anchor.SetParent(parent, false);
        anchor.position = position;
        return anchor;
    }

    static void Entrance(Transform parent, string id, Vector2 position, int facing)
    {
        var entrance = Anchor(parent, id, position).gameObject.AddComponent<RoomEntrance>();
        entrance.entranceId = id;
        entrance.facing = facing;
    }
}
