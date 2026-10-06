using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ActivationAuthoring
{
 public static void Configure(){var scene=EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");var game=Object.FindObjectOfType<DemoGame>();game.mapScene="bot map";EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/HollowGeometry.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Rooms/bot map.unity",true)};if(game.GetComponent<WorldActivation>()==null)game.gameObject.AddComponent<WorldActivation>();game.GetComponent<WorldActivation>().chunkSize=new Vector2(20,16);game.GetComponent<WorldActivation>().preloadMargin=new Vector2(12,10);EditorSceneManager.SaveScene(scene);Debug.Log("DEMO_ACTIVATION_AUTHORING_OK");}
}
