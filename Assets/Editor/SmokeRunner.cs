using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class SmokeRunner
{
    const string Key="HollowDemo.SmokeTest";
    static double deadline;
    static SmokeRunner(){EditorApplication.playModeStateChanged+=Changed;if(SessionState.GetBool(Key,false)) Arm();}
    public static void RunBoss(){SessionState.SetBool("HollowDemo.BossSmoke",true);Run();}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");SessionState.SetBool(Key,true);Arm();EditorApplication.isPlaying=true;}
    static void Arm(){deadline=EditorApplication.timeSinceStartup+90;EditorApplication.update-=Watchdog;EditorApplication.update+=Watchdog;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
    static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){ if(SessionState.GetBool("HollowDemo.BossSmoke",false)) new GameObject("Boss Smoke Probe").AddComponent<HollowDemo.BossSmokeProbe>(); else new GameObject("Smoke Probe").AddComponent<HollowDemo.SmokeProbe>(); }}
    static void Watchdog(){if(SessionState.GetBool(Key,false)&&EditorApplication.timeSinceStartup>deadline){Debug.LogError("DEMO_SMOKE_TIMEOUT");Finish(1);}}
    static void Log(string message,string trace,LogType type){if(!SessionState.GetBool(Key,false))return;if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)Finish(1);else if(message.StartsWith("DEMO_SMOKE_OK"))Finish(0);}
    static void Finish(int code){SessionState.SetBool(Key,false);SessionState.SetBool("HollowDemo.BossSmoke",false);EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
}



