using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Checkpoint)), CanEditMultipleObjects]
public sealed class CheckpointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (targets.Cast<Checkpoint>().Any(p => p.spawnPoint == null))
            EditorGUILayout.HelpBox("请将重生点子对象拖到 Spawn Point。", MessageType.Warning);
        var point = (Checkpoint)target;
        if (Application.isPlaying || !point.gameObject.scene.IsValid() ||
            (!string.IsNullOrEmpty(point.gameObject.scene.path) && !point.gameObject.scene.path.EndsWith(".unity"))) return;
        int count = Object.FindObjectsOfType<Checkpoint>(true).Count(p => p.isInitialSpawn && p.gameObject.activeInHierarchy);
        if (count > 1)
            EditorGUILayout.HelpBox("多个检查点勾选了初始出生点，关卡无法开始。请只保留一个。", MessageType.Error);
        else if (count == 0)
            EditorGUILayout.HelpBox("自动初始化关卡需要勾选一个初始出生点；旧地图已配置起点时可继续使用原设置。", MessageType.Info);
    }
}
