using System.Linq;
using HollowDemo;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RoomSnapshotProbe
{
    public static void Report()
    {
        foreach (string path in System.IO.Directory.GetFiles("Assets/Diagnostics", "Snapshot*.unity").Concat(new[] { "Assets/Scenes/Rooms/Room0.unity" }))
        {
            EditorSceneManager.OpenScene(path);
            var room = Object.FindObjectOfType<RoomContent>();
            if (room != null && room.roomId == 0)
            {
                Physics2D.SyncTransforms();
                var colliders = room.GetComponentsInChildren<GroundSurface>(true).Select(g => g.GetComponent<Collider2D>()).ToArray();
                var bounds = colliders[0].bounds;
                foreach (var collider in colliders) bounds.Encapsulate(collider.bounds);
                var spawn = room.startEntrance.transform.position;
                Debug.Log("SNAPSHOT_ROOM: path=" + path + " root=" + room.transform.position + " entry=" + spawn + " cameraBounds=" + room.Bounds + " terrainBounds=" + bounds + " checkpoint=" + room.checkpoint.spawnPoint.position);
                foreach (Transform child in room.transform)
                    Debug.Log("SNAPSHOT_CHILD: " + child.name + " world=" + child.position + " local=" + child.localPosition);
                if (path.Contains("Snapshot"))
                {
                    UnityEditor.EditorSettings.serializationMode = UnityEditor.SerializationMode.ForceText;
                    EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                }
                foreach (var collider in colliders.Where(c => c.bounds.min.x <= spawn.x && c.bounds.max.x >= spawn.x))
                    Debug.Log("SNAPSHOT_FLOOR_AT_ENTRY: " + collider.name + " " + collider.bounds);
            }
            var game = Object.FindObjectOfType<DemoGame>();
            if (game != null) Debug.Log("SNAPSHOT_SHELL: DemoGame=" + game.transform.position + " player=" + Object.FindObjectOfType<PlayerMotor>().transform.position + " camera=" + Camera.main.transform.position);
        }
        Debug.Log("SNAPSHOT_REPORT_OK");
    }
}
