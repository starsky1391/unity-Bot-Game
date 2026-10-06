using System.Collections.Generic;
using System.IO;
using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
public static class BossAuthoring
{
    const string ArenaPath="Assets/Prefabs/Bosses/BossArena.prefab",UiPath="Assets/Prefabs/UI/BossInterface.prefab";
    [MenuItem("洞穴 Demo/添加 Boss 两状态动画器")]
    public static void AddWaveAnimator()
    {
        const string folder="Assets/Animations/Boss";
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/BossIdle.anim");
        if(idle==null){idle=new AnimationClip{name="待机"};AssetDatabase.CreateAsset(idle,folder+"/BossIdle.anim");}
        var exposed=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/BossCoreExposed.anim");
        if(exposed==null){exposed=new AnimationClip{name="核心暴露"};AssetDatabase.CreateAsset(exposed,folder+"/BossCoreExposed.anim");}
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"/WaveBoss.controller");
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(folder+"/WaveBoss.controller");
            controller.AddParameter("CoreExposed",AnimatorControllerParameterType.Bool);
            var machine=controller.layers[0].stateMachine;
            var idleState=machine.AddState("待机");idleState.motion=idle;
            var exposedState=machine.AddState("核心暴露");exposedState.motion=exposed;
            machine.defaultState=idleState;
            var open=idleState.AddTransition(exposedState);open.hasExitTime=false;open.duration=0;open.AddCondition(AnimatorConditionMode.If,0,"CoreExposed");
            var close=exposedState.AddTransition(idleState);close.hasExitTime=false;close.duration=0;close.AddCondition(AnimatorConditionMode.IfNot,0,"CoreExposed");
        }
        const string path="Assets/Prefabs/Bosses/WaveBossArena.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        var waves=root.GetComponent<WaveBoss>();var core=root.GetComponent<BossArena>().boss;
        var animator=core.GetComponent<Animator>();if(animator==null)animator=core.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
        waves.bossAnimator=animator;
        PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);AssetDatabase.SaveAssets();
        Debug.Log("DEMO_WAVE_ANIMATOR_OK");
    }
    [MenuItem("洞穴 Demo/生成三波召唤 Boss 预制体")]
    public static void CreateWaveBoss()
    {
        var root=new GameObject("三波召唤 Boss");
        var zone=root.AddComponent<BoxCollider2D>();zone.isTrigger=true;zone.size=new Vector2(24,10);zone.offset=new Vector2(0,4);
        var arena=root.AddComponent<BossArena>();arena.bossName="召唤核心";
        var waves=root.AddComponent<WaveBoss>();
        waves.patrolPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Patrol.prefab").GetComponent<EnemyBrain>();
        waves.shooterPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shooter.prefab").GetComponent<EnemyBrain>();
        waves.pursuerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Pursuer.prefab").GetComponent<EnemyBrain>();
        var core=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bosses/ShadowGuard.prefab"),root.transform);
        core.name="可攻击核心";core.transform.localPosition=new Vector3(0,3);core.transform.localScale=Vector3.one*1.4f;
        arena.boss=core.GetComponent<EnemyBrain>();arena.boss.kind=EnemyKind.Patrol;arena.boss.maxHealth=24;arena.boss.persistentId="wave_boss";
        core.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;core.GetComponent<Rigidbody2D>().gravityScale=0;
        core.GetComponent<Collider2D>().isTrigger=true;
        core.GetComponent<SpriteRenderer>().color=arena.boss.baseColor=new Color(.65f,.3f,1);
        arena.spawnPoint=core.transform;
        Ground(root.transform,"地面",new Vector2(0,-.5f),new Vector2(25,1));
        arena.leftBarrier=Ground(root.transform,"左侧空气墙",new Vector2(-12,4),new Vector2(.5f,8));
        arena.rightBarrier=Ground(root.transform,"右侧空气墙",new Vector2(12,4),new Vector2(.5f,8));
        foreach(var wall in new[]{arena.leftBarrier,arena.rightBarrier}){wall.GetComponent<GroundSurface>().canClimb=false;wall.GetComponent<SpriteRenderer>().color=new Color(.6f,.4f,1,.2f);wall.SetActive(false);}
        waves.groundSpawns=new Transform[3];waves.flyingSpawns=new Transform[2];
        for(int n=0;n<5;n++){var point=new GameObject(n<3?"地面召唤点 "+(n+1):"飞行召唤点 "+(n-2)).transform;point.SetParent(root.transform,false);point.localPosition=n<3?new Vector3(-7+n*7,1.1f):new Vector3(n==3?-5:5,6);if(n<3)waves.groundSpawns[n]=point;else waves.flyingSpawns[n-3]=point;}
        foreach(string name in new[]{"下坠砖块","落点预警"}){var go=new GameObject(name);go.transform.SetParent(root.transform,false);var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Square.png");sprite.sortingOrder=5;sprite.color=name=="下坠砖块"?new Color(.75f,.7f,.65f):new Color(1,.15f,.1f,.85f);sprite.enabled=false;if(name=="下坠砖块")waves.brick=sprite;else waves.warning=sprite;}
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UiPath),root.transform);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Bosses/WaveBossArena.prefab");Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
        AddWaveAnimator();
        Debug.Log("DEMO_WAVE_BOSS_PREFAB_OK");
    }
    [MenuItem("洞穴 Demo/添加 Boss 展示区")]
    public static void Configure()
    {
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        var world=Object.FindObjectOfType<WorldMap>();
        if(world.GetComponentInChildren<BossArena>()!=null) return;
        Directory.CreateDirectory("Assets/Prefabs/Bosses");
        var source=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Enemies/Charger.prefab");
        source.name="暗影守卫";source.transform.localScale=new Vector3(1.8f,2.2f,1);
        var brain=source.GetComponent<EnemyBrain>();brain.maxHealth=24;brain.warningTime=.8f;brain.attackTime=.65f;brain.recoveryTime=1.2f;brain.detectionRange=30;brain.baseColor=new Color(.45f,.32f,.7f);brain.persistentId="boss_shadow_guard";brain.roomId=4;
        source.GetComponent<SpriteRenderer>().color=brain.baseColor;
        PrefabUtility.SaveAsPrefabAsset(source,"Assets/Prefabs/Bosses/ShadowGuard.prefab");PrefabUtility.UnloadPrefabContents(source);
        var root=new GameObject("Boss 展示区");var arena=root.AddComponent<BossArena>();
        var trigger=root.GetComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.offset=new Vector2(0,3.5f);trigger.size=new Vector2(25,7);
        Ground(root.transform,"地面",new Vector2(0,-.5f),new Vector2(29,1));
        Ground(root.transform,"顶棚",new Vector2(0,8.5f),new Vector2(29,1));
        Ground(root.transform,"终点墙",new Vector2(14.5f,4),new Vector2(1,8));
        arena.leftBarrier=Ground(root.transform,"左侧空气墙",new Vector2(-14,4),new Vector2(.6f,8.25f));
        arena.rightBarrier=Ground(root.transform,"右侧空气墙",new Vector2(14,4),new Vector2(.6f,8.25f));
        foreach(var gate in new[]{arena.leftBarrier,arena.rightBarrier}){gate.GetComponent<GroundSurface>().canClimb=false;gate.GetComponent<SpriteRenderer>().color=new Color(.7f,.5f,1,.18f);gate.SetActive(false);}
        arena.spawnPoint=new GameObject("Boss 出生点").transform;arena.spawnPoint.SetParent(root.transform,false);arena.spawnPoint.localPosition=new Vector3(7,1.2f);
        var boss=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bosses/ShadowGuard.prefab"),root.transform);
        boss.transform.localPosition=arena.spawnPoint.localPosition;arena.boss=boss.GetComponent<EnemyBrain>();
        PrefabUtility.SaveAsPrefabAsset(root,ArenaPath);Object.DestroyImmediate(root);
        Physics2D.SyncTransforms();
        var grounds=world.GetComponentsInChildren<GroundSurface>().Select(g=>g.GetComponent<Collider2D>()).ToArray();
        float right=grounds.Max(c=>c.bounds.max.x);
        float floor=grounds.Where(c=>c.bounds.size.x>2 && c.bounds.size.x>c.bounds.size.y && c.bounds.max.x>right-20).Min(c=>c.bounds.max.y);
        foreach(var wall in grounds.Where(c=>c.bounds.max.x>=right-.1f && c.bounds.size.x<2 && c.bounds.min.y<floor+3).ToArray())
        {var b=wall.bounds;if(b.max.y<=floor+3) Object.DestroyImmediate(wall.gameObject);else{wall.transform.position=new Vector3(b.center.x,(b.max.y+floor+3)*.5f);wall.transform.localScale=new Vector3(b.size.x,b.max.y-floor-3,1);}}
        Ground(world.transform,"Boss 连接通道地面",new Vector2(right+2,floor-.5f),new Vector2(4,1),true);
        Ground(world.transform,"Boss 连接通道顶棚",new Vector2(right+2,floor+3.5f),new Vector2(4,1),true);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ArenaPath),world.transform);
        instance.transform.position=new Vector3(right+18,floor,0);
        var checkpoint=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Checkpoint.prefab"),world.transform);
        checkpoint.name="Boss 区域前检查点";checkpoint.transform.position=new Vector3(right+1.2f,floor+.9f);
        checkpoint.GetComponent<Checkpoint>().persistentId="checkpoint_boss_showcase";checkpoint.GetComponent<Checkpoint>().roomId=4;
        PrefabUtility.RecordPrefabInstancePropertyModifications(checkpoint.GetComponent<Checkpoint>());
        foreach(var gate in world.GetComponentsInChildren<Transform>().Where(t=>t.name=="Finish Gate"))
            if(gate.GetComponent<Collider2D>()!=null) gate.GetComponent<Collider2D>().enabled=false;
        var finish=new GameObject("终点");finish.transform.SetParent(instance.transform,false);finish.transform.localPosition=new Vector3(11.5f,1.2f);finish.transform.localScale=new Vector3(.6f,2.4f,1);
        var sprite=finish.AddComponent<SpriteRenderer>();sprite.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Square.png");sprite.color=new Color(.25f,1,.75f,.65f);
        world.finish=finish.transform;
        float x=right-world.transform.position.x,y=floor-world.transform.position.y;
        var outline=new List<Vector2>{world.outline[0],new Vector2(x+32.5f,y),new Vector2(x+32.5f,y+8),new Vector2(x+4,y+8),new Vector2(x+4,y+3),new Vector2(x,y+3)};
        outline.AddRange(world.outline.Skip(2));world.outline=outline.ToArray();
        CreateUi();
        var ui=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UiPath),Object.FindObjectOfType<DemoGame>().transform);
        ui.transform.localPosition=Vector3.zero;
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("DEMO_BOSS_AREA_OK: appended arena with movable spawn/gates, pre-arena checkpoint and editable Canvas intro/health.");
    }
    static GameObject Ground(Transform parent,string name,Vector2 position,Vector2 size,bool worldPosition=false)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Ground.prefab"),parent);
        go.name=name;if(worldPosition)go.transform.position=position;else go.transform.localPosition=position;go.transform.localScale=new Vector3(size.x,size.y,1);return go;
    }
    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;}
    static Text Text(Transform parent,string name,string value,Vector2 position,Vector2 size,int fontSize)
    {var rect=Rect(parent,name,position,size);var text=rect.gameObject.AddComponent<Text>();text.text=value;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.95f,.85f,.65f);text.raycastTarget=false;return text;}
    static Image Image(Transform parent,string name,Vector2 position,Vector2 size,Color color)
    {var image=Rect(parent,name,position,size).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
    static void CreateUi()
    {
        var root=new GameObject("Boss Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(BossCanvas));
        root.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,720);root.transform.localScale=Vector3.one*.02f;
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=25;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var controller=root.GetComponent<BossCanvas>();
        var intro=Rect(root.transform,"Boss 名称展示",new Vector2(0,40),new Vector2(700,150));controller.introduction=intro.gameObject.AddComponent<CanvasGroup>();controller.introduction.blocksRaycasts=false;controller.introduction.interactable=false;controller.introduction.alpha=1;
        Image(intro,"上边线",new Vector2(0,55),new Vector2(520,2),new Color(.8f,.65f,.45f,.65f));
        Image(intro,"下边线",new Vector2(0,-55),new Vector2(520,2),new Color(.8f,.65f,.45f,.65f));
        controller.introTitle=Text(intro,"Boss 名称","暗影守卫",Vector2.zero,new Vector2(650,100),42);
        var hud=Rect(root.transform,"Boss 血条",new Vector2(0,-280),new Vector2(700,85));controller.healthPanel=hud.gameObject;
        controller.hudTitle=Text(hud,"名称","暗影守卫",new Vector2(0,28),new Vector2(650,35),23);
        Image(hud,"血条边框",new Vector2(0,-8),new Vector2(680,24),new Color(.6f,.5f,.4f));
        Image(hud,"血条底色",new Vector2(0,-8),new Vector2(674,18),new Color(.08f,.06f,.09f));
        controller.healthFill=Image(hud,"当前血量",new Vector2(0,-8),new Vector2(674,18),new Color(.7f,.3f,.45f));
        controller.healthFill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Square.png");controller.healthFill.type=UnityEngine.UI.Image.Type.Filled;controller.healthFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
        controller.healthText=Text(hud,"数值","24 / 24",new Vector2(0,-8),new Vector2(650,24),15);
        PrefabUtility.SaveAsPrefabAsset(root,UiPath);Object.DestroyImmediate(root);
    }
}

