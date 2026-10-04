using System;
using System.IO;
using UnityEngine;

namespace HollowDemo
{
    [Serializable]
    public sealed class SaveData
    {
        [Serializable] public sealed class Stack { public int slot; public string itemId; public int count; }
        [Serializable] public sealed class MapCell { public int room, x, y; }
        public MapCell[] exploredMapCells;
        public int version = 1;
        public int checkpointRoom = -1;
        public string checkpointId;
        public string[] activatedCheckpointIds = Array.Empty<string>();
        public bool completed;
        public int[] exploredRooms;
        public int[] activatedCheckpoints;
        public string[] collectedPickups;
        public string[] defeatedEnemies = Array.Empty<string>();
        public string[] discoveredLandmarks = Array.Empty<string>();
        public Stack[] inventory;
    }

    public static class SaveStore
    {
#if UNITY_EDITOR
        public static string testPath;
#endif
        public static string Path
        {
            get
            {
#if UNITY_EDITOR
                if (testPath != null) return testPath;
#endif
                return System.IO.Path.Combine(Application.persistentDataPath, "progress.json");
            }
        }
        public static bool Exists => File.Exists(Path);
        public static SaveData Read() => JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));
        public static void Write(SaveData data)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string temporary = Path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            if (File.Exists(Path)) File.Replace(temporary, Path, null);
            else File.Move(temporary, Path);
        }
    }
}
