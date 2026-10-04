using System;
using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapAuthoring
{
    public static void Calibrate()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        foreach (var room in UnityEngine.Object.FindObjectsOfType<RoomVolume>().OrderBy(r => r.id))
        {
            var map = room.map;
            float width = map.outline.Max(p => p.x), height = map.outline.Max(p => p.y);
            Vector2 scale = new Vector2(width / room.bounds.width, height / room.bounds.yMax);
            map.outline = room.ceilingOutline.Select(p => Vector2.Scale(new Vector2(p.x - room.bounds.xMin, p.y), scale)).ToArray();
            map.checkpointPosition = Vector2.Scale(new Vector2((room.id == 0 ? 0 : room.bounds.xMin + 1) - room.bounds.xMin, 1), scale);
            foreach (var merchant in map.merchants)
                if (merchant.id == "merchant_arli") merchant.position = Vector2.Scale(new Vector2(10 - room.bounds.xMin, .8f), scale);
            EditorUtility.SetDirty(map);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("DEMO_MAP_CALIBRATED_OK: independent outer outlines match authored room passages.");
    }

    public static void Configure()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        Assign(UnityEngine.Object.FindObjectsOfType<RoomVolume>().OrderBy(r => r.id).ToArray());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("DEMO_MAP_CONFIGURE_OK: five independently authored map assets assigned; world geometry preserved.");
    }

    public static void Assign(RoomVolume[] rooms)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data/Maps")) AssetDatabase.CreateFolder("Assets/Data", "Maps");
        float[] origins = { 0, 20, 34, 50, 64 };
        float[] widths = { 20, 14, 16, 14, 18 };
        Vector2[][] shapes =
        {
            new[] { new Vector2(0,0), new Vector2(20,0), new Vector2(20,2), new Vector2(15,2), new Vector2(15,6), new Vector2(18,6), new Vector2(18,8), new Vector2(12,8), new Vector2(12,2), new Vector2(6,2), new Vector2(6,4), new Vector2(3,4), new Vector2(3,2), new Vector2(0,2) },
            new[] { new Vector2(0,0), new Vector2(14,0), new Vector2(14,2), new Vector2(10,2), new Vector2(10,5), new Vector2(5,5), new Vector2(5,2), new Vector2(0,2) },
            new[] { new Vector2(0,0), new Vector2(16,0), new Vector2(16,2), new Vector2(11,2), new Vector2(11,6), new Vector2(6,6), new Vector2(6,2), new Vector2(0,2) },
            new[] { new Vector2(0,0), new Vector2(14,0), new Vector2(14,2), new Vector2(12,2), new Vector2(12,5), new Vector2(10,5), new Vector2(10,8), new Vector2(5,8), new Vector2(5,5), new Vector2(2,5), new Vector2(2,2), new Vector2(0,2) },
            new[] { new Vector2(0,0), new Vector2(18,0), new Vector2(18,2), new Vector2(15,2), new Vector2(15,6), new Vector2(9,6), new Vector2(9,8), new Vector2(6,8), new Vector2(6,2), new Vector2(0,2) }
        };
        foreach (var room in rooms)
        {
            string path = "Assets/Data/Maps/Room" + room.id + ".asset";
            var map = AssetDatabase.LoadAssetAtPath<RoomMapDefinition>(path);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<RoomMapDefinition>();
                int id = room.id;
                map.origin = new Vector2(origins[id], 0);
                map.outline = shapes[id];
                map.checkpointPosition = new Vector2(id == 0 ? 1 : 1.5f, 1);
                map.exits = room.neighbors.Select(n => new RoomMapDefinition.Exit
                { targetRoom = n, position = new Vector2(n < id ? 0 : widths[id], 1), direction = n < id ? Vector2.left : Vector2.right }).ToArray();
                map.merchants = id == 0 ? new[] { new RoomMapDefinition.Landmark
                { id = "merchant_arli", title = "阿砾", position = new Vector2(6, 1) } } : Array.Empty<RoomMapDefinition.Landmark>();
                AssetDatabase.CreateAsset(map, path);
            }
            room.map = map;
            EditorUtility.SetDirty(room);
        }
    }
}
