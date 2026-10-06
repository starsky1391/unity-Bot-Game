using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class SmokeRunner
{
    const string Key="HollowDemo.SmokeTest";
    static double deadline;
    static SmokeRunner(){EditorApplication.playModeStateChanged+=Changed;if(SessionState.GetBool(Key,false)) Arm();}
    public static void RunAbility(){SessionState.SetBool("HollowDemo.AbilitySmoke",true);Run();}
    public static void RunActivation(){SessionState.SetBool("HollowDemo.ActivationSmoke",true);Run();}
    public static void RunGrappleAim(){SessionState.SetBool("HollowDemo.GrappleAimSmoke",true);Run();}
    public static void RunGrappleJump(){SessionState.SetBool("HollowDemo.GrappleJumpSmoke",true);Run();}
    public static void RunOrb(){SessionState.SetBool("HollowDemo.OrbSmoke",true);RunSkill();}
    public static void RunDrop(){SessionState.SetBool("HollowDemo.DropSmoke",true);RunSkill();}
    public static void RunSkill(){SessionState.SetBool("HollowDemo.SkillSmoke",true);Run();}
    public static void RunProgression(){SessionState.SetBool("HollowDemo.ProgressionSmoke",true);Run();}
    public static void RunBoss(){SessionState.SetBool("HollowDemo.BossSmoke",true);Run();}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");if(SessionState.GetBool("HollowDemo.ActivationSmoke",false)){var fixture=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/PickupPotion.prefab"));fixture.transform.position=new Vector3(200,25);fixture.GetComponent<HollowDemo.Pickup>().persistentId="activation_test_pickup";}if(SessionState.GetBool("HollowDemo.AbilitySmoke",false)){var fixture=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/AbilityUnlock.prefab"));var unlock=fixture.GetComponent<HollowDemo.AbilityUnlock>();unlock.persistentId="ability_test";unlock.grapple=true;}SessionState.SetBool(Key,true);Arm();EditorApplication.isPlaying=true;}
    static void Arm(){deadline=EditorApplication.timeSinceStartup+90;EditorApplication.update-=Watchdog;EditorApplication.update+=Watchdog;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
    static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){ if(SessionState.GetBool("HollowDemo.AbilitySmoke",false)) new GameObject("Ability Smoke Probe").AddComponent<HollowDemo.AbilitySmokeProbe>(); else if(SessionState.GetBool("HollowDemo.ActivationSmoke",false)) new GameObject("Activation Smoke Probe").AddComponent<HollowDemo.ActivationSmokeProbe>(); else if(SessionState.GetBool("HollowDemo.GrappleAimSmoke",false)) new GameObject("Grapple Aim Smoke Probe").AddComponent<HollowDemo.GrappleAimSmokeProbe>(); else if(SessionState.GetBool("HollowDemo.GrappleJumpSmoke",false)) new GameObject("Grapple Jump Smoke Probe").AddComponent<HollowDemo.GrappleJumpSmokeProbe>(); else if(SessionState.GetBool("HollowDemo.SkillSmoke",false)) new GameObject("Skill Smoke Probe").AddComponent<HollowDemo.SkillSmokeProbe>(); else if(SessionState.GetBool("HollowDemo.ProgressionSmoke",false)) new GameObject("Progression Smoke Probe").AddComponent<HollowDemo.ProgressionSmokeProbe>(); else if(SessionState.GetBool("HollowDemo.BossSmoke",false)) new GameObject("Boss Smoke Probe").AddComponent<HollowDemo.BossSmokeProbe>(); else new GameObject("Smoke Probe").AddComponent<HollowDemo.SmokeProbe>(); }}
    static void Watchdog(){if(SessionState.GetBool(Key,false)&&EditorApplication.timeSinceStartup>deadline){Debug.LogError("DEMO_SMOKE_TIMEOUT");Finish(1);}}
    static void Log(string message,string trace,LogType type){if(!SessionState.GetBool(Key,false))return;if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)Finish(1);else if(message.StartsWith("DEMO_SMOKE_OK"))Finish(0);}
    static void Finish(int code){SessionState.SetBool(Key,false);SessionState.SetBool("HollowDemo.DropSmoke",false);SessionState.SetBool("HollowDemo.OrbSmoke",false);SessionState.SetBool("HollowDemo.AbilitySmoke",false);SessionState.SetBool("HollowDemo.ActivationSmoke",false);SessionState.SetBool("HollowDemo.GrappleAimSmoke",false);SessionState.SetBool("HollowDemo.GrappleJumpSmoke",false);SessionState.SetBool("HollowDemo.SkillSmoke",false);SessionState.SetBool("HollowDemo.ProgressionSmoke",false);SessionState.SetBool("HollowDemo.BossSmoke",false);EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
}



