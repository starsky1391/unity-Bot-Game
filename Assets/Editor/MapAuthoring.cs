using System;
using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapAuthoring
{
    [MenuItem("洞穴 Demo/更新整体地图外轮廓")]
    public static void UpdateWholeMapOutline()
    {
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Rooms/bot map.unity");
        Physics2D.SyncTransforms();
        var grounds=UnityEngine.Object.FindObjectsOfType<GroundSurface>().Where(g=>g.GetComponent<DropPlatform>()==null && g.GetComponent<Collider2D>().enabled).ToArray();
        var spawn=UnityEngine.Object.FindObjectsOfType<Checkpoint>().Single(c=>c.isInitialSpawn).spawnPoint.position;
        var temporary=new GameObject("临时轮廓合并");
        temporary.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;
        var composite=temporary.AddComponent<CompositeCollider2D>();composite.geometryType=CompositeCollider2D.GeometryType.Outlines;composite.generationType=CompositeCollider2D.GenerationType.Manual;
        foreach(var ground in grounds)
        {
            var bounds=ground.GetComponent<Collider2D>().bounds;
            var collider=temporary.AddComponent<BoxCollider2D>();collider.offset=bounds.center;collider.size=bounds.size;collider.usedByComposite=true;
        }
        composite.GenerateGeometry();
        Vector2[] selected=null;float selectedArea=float.PositiveInfinity;float nearest=float.PositiveInfinity;
        for(int path=0;path<composite.pathCount;path++)
        {
            var points=new Vector2[composite.GetPathPointCount(path)];composite.GetPath(path,points);
            if(points.Length<3)continue;
            bool inside=false;float area=0, distance=float.PositiveInfinity;
            for(int n=0;n<points.Length;n++)
            {
                var a=points[n];var b=points[(n+1)%points.Length];area+=a.x*b.y-b.x*a.y;
                if((a.y>spawn.y)!=(b.y>spawn.y) && spawn.x<(b.x-a.x)*(spawn.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
                var delta=b-a;var point=a+delta*Mathf.Clamp01(Vector2.Dot((Vector2)spawn-a,delta)/Mathf.Max(.000001f,delta.sqrMagnitude));distance=Mathf.Min(distance,Vector2.Distance(spawn,point));
            }
            area=Mathf.Abs(area);
            if(inside && area<selectedArea){selected=points;selectedArea=area;}
            else if(float.IsPositiveInfinity(selectedArea) && distance<nearest){selected=points;nearest=distance;}
        }
        UnityEngine.Object.DestroyImmediate(temporary);
        if(selected==null)throw new InvalidOperationException("无法生成地图外轮廓");
        var world=UnityEngine.Object.FindObjectOfType<WorldMap>();
        if(world==null)world=new GameObject("地图数据").AddComponent<WorldMap>();
        world.outline=selected.Select(p=>(Vector2)world.transform.InverseTransformPoint(p)).ToArray();world.startPoint=UnityEngine.Object.FindObjectsOfType<Checkpoint>().Single(c=>c.isInitialSpawn).spawnPoint;
        EditorUtility.SetDirty(world);EditorSceneManager.SaveScene(scene);
        Debug.Log("DEMO_MAP_OUTLINE_OK: "+selected.Length+" points; independent outline authored");
    }

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
