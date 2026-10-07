using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class SkillAuthoring {
 [MenuItem("洞穴 Demo/匹配冲刺动画时长")]
 public static void MatchDashAnimationDuration(){
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Prefabs/Animation/PlayerController(1).controller");
  var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="sprint").state;
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");var player=Object.FindObjectOfType<PlayerMotor>();
  state.speed=state.motion.averageDuration/player.dashDuration;EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
  var temporary=new GameObject("冲刺时长验证",typeof(SpriteRenderer),typeof(Animator));var animator=temporary.GetComponent<Animator>();animator.runtimeAnimatorController=controller;
  animator.SetBool("IsDashing",true);animator.Play("sprint",0,0);animator.Update(0);animator.Update(player.dashDuration);
  if(animator.GetCurrentAnimatorStateInfo(0).normalizedTime<.99f)throw new System.Exception("冲刺动画未完整播放");
  Object.DestroyImmediate(temporary);Debug.Log("DEMO_DASH_DURATION_OK: speed="+state.speed+", duration="+player.dashDuration);
 }
 [MenuItem("洞穴 Demo/修复主角冲刺动画过渡")]
 public static void FixDashTransitions(){
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Prefabs/Animation/PlayerController(1).controller");
  var states=controller.layers[0].stateMachine.states.Select(s=>s.state).ToArray();
  var dash=states.Single(s=>s.name=="sprint");
  foreach(var state in states)foreach(var transition in state.transitions){
   if(transition.destinationState==dash || state==dash && transition.destinationState!=null && (transition.destinationState.name=="Idle" || transition.destinationState.name=="jump")){
    transition.hasExitTime=false;transition.duration=0;
    if(state==dash && !transition.conditions.Any(c=>c.parameter=="IsDashing" && c.mode==AnimatorConditionMode.IfNot))transition.AddCondition(AnimatorConditionMode.IfNot,0,"IsDashing");
   }
   transition.interruptionSource=TransitionInterruptionSource.SourceThenDestination;
  }
  EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
  var temporary=new GameObject("冲刺动画验证",typeof(SpriteRenderer),typeof(Animator));var animator=temporary.GetComponent<Animator>();animator.runtimeAnimatorController=controller;
  foreach(var start in new[]{"Idle","jump","attack"}){
   animator.SetBool("IsAttacking",false);animator.SetBool("IsDashing",false);animator.SetBool("IsGrounded",start=="Idle");animator.Play(start,0,0);animator.Update(0);
   animator.SetBool("IsDashing",true);animator.Update(.02f);
   if(!animator.GetCurrentAnimatorStateInfo(0).IsName("sprint"))throw new System.Exception("无法立即进入冲刺: "+start);
   animator.SetBool("IsGrounded",false);animator.SetBool("IsDashing",false);animator.Update(.02f);
   if(!animator.GetCurrentAnimatorStateInfo(0).IsName("jump"))throw new System.Exception("空中冲刺不能返回跳跃");
  }
  animator.Rebind();animator.SetBool("IsGrounded",true);animator.SetBool("IsDashing",true);animator.Play("sprint",0,0);animator.Update(0);animator.SetBool("IsDashing",false);animator.Update(.02f);
  if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))throw new System.Exception("地面冲刺不能返回待机");
  Object.DestroyImmediate(temporary);Debug.Log("DEMO_DASH_TRANSITIONS_OK: immediate entry and grounded/air exits");
 }
 public static void Configure(){
 var mana=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Resources/Items/mana_flask.asset");if(mana==null){mana=ScriptableObject.CreateInstance<ItemDefinition>();mana.id="mana_flask";mana.displayName="蓝瓶";mana.description="检查点分配的循环蓝瓶，恢复蓝量";mana.kind=ItemKind.ManaFlask;mana.stackLimit=1;mana.retainWhenEmpty=true;mana.manaRecovery=50;mana.color=new Color(.25f,.5f,1);AssetDatabase.CreateAsset(mana,"Assets/Resources/Items/mana_flask.asset");}
 var orb=new GameObject("白球",typeof(SpriteRenderer),typeof(SkillOrb));orb.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Circle.png");orb.GetComponent<SpriteRenderer>().sortingOrder=8;orb.transform.localScale=Vector3.one*.4f;PrefabUtility.SaveAsPrefabAsset(orb,"Assets/Prefabs/Player/SkillOrb.prefab");Object.DestroyImmediate(orb);
 var kill=new GameObject("接触即死区域",typeof(SpriteRenderer),typeof(InstantDeathZone));kill.GetComponent<BoxCollider2D>().size=Vector2.one;kill.GetComponent<BoxCollider2D>().isTrigger=true;kill.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Square.png");kill.GetComponent<SpriteRenderer>().color=new Color(.7f,.1f,.2f,.4f);kill.transform.localScale=new Vector3(5,1,1);PrefabUtility.SaveAsPrefabAsset(kill,"Assets/Prefabs/World/InstantDeathZone.prefab");Object.DestroyImmediate(kill);
 var platform=new GameObject("可下跳平台",typeof(SpriteRenderer),typeof(DropPlatform),typeof(GroundSurface));platform.layer=8;platform.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Square.png");platform.GetComponent<SpriteRenderer>().color=new Color(.35f,.65f,.7f);platform.GetComponent<BoxCollider2D>().size=Vector2.one;platform.GetComponent<BoxCollider2D>().usedByEffector=true;platform.GetComponent<PlatformEffector2D>().useOneWay=true;platform.GetComponent<PlatformEffector2D>().surfaceArc=160;platform.GetComponent<GroundSurface>().canClimb=false;platform.transform.localScale=new Vector3(5,.25f,1);PrefabUtility.SaveAsPrefabAsset(platform,"Assets/Prefabs/World/DropPlatform.prefab");Object.DestroyImmediate(platform);
 var player=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player/Player.prefab");player.GetComponent<PlayerMotor>().skillPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/SkillOrb.prefab").GetComponent<SkillOrb>();PrefabUtility.SaveAsPrefabAsset(player,"Assets/Prefabs/Player/Player.prefab");PrefabUtility.UnloadPrefabContents(player);
 const string hudPath="Assets/Prefabs/UI/PlayerHUD.prefab";var root=PrefabUtility.LoadPrefabContents(hudPath);var hud=root.GetComponent<PlayerHudCanvas>();hud.crystalCount.color=Color.white;hud.crystalCount.font=ChineseFont.Shared;hud.crystalCount.fontSize=24;hud.crystalCount.horizontalOverflow=HorizontalWrapMode.Overflow;hud.crystalCount.verticalOverflow=VerticalWrapMode.Overflow;hud.crystalCount.text="0";
 if(hud.manaFill==null){var bg=Rect(hud.panel.transform,"蓝量背景",new Vector2(80,-43),new Vector2(140,10));bg.anchorMin=bg.anchorMax=new Vector2(0,1);bg.gameObject.AddComponent<Image>().color=new Color(.1f,.15f,.25f,.8f);var fill=Rect(bg,"蓝量",Vector2.zero,new Vector2(140,10));hud.manaFill=fill.gameObject.AddComponent<Image>();hud.manaFill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Square.png");hud.manaFill.type=Image.Type.Filled;hud.manaFill.fillMethod=Image.FillMethod.Horizontal;hud.manaFill.color=new Color(.25f,.55f,1);}
 hud.manaFill.transform.parent.GetComponent<RectTransform>().anchoredPosition=new Vector2(80,-43);
 PrefabUtility.SaveAsPrefabAsset(root,hudPath);PrefabUtility.UnloadPrefabContents(root);
 var ui=new GameObject("检查点 Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(FlaskAllocationCanvas));ui.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;ui.GetComponent<Canvas>().sortingOrder=30;ui.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,720);ui.transform.localScale=Vector3.one*.02f;var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;var view=ui.GetComponent<FlaskAllocationCanvas>();view.panel=Rect(ui.transform,"血瓶蓝瓶分配",Vector2.zero,new Vector2(480,290)).gameObject;view.panel.AddComponent<Image>().color=new Color(.035f,.065f,.09f,.95f);Label(view.panel.transform,"标题","检查点 · 容器分配",new Vector2(0,100),new Vector2(440,45),26);view.counts=Label(view.panel.transform,"分配数量","血瓶 3 / 蓝瓶 0",new Vector2(0,35),new Vector2(440,70),22);view.moreBlood=Button(view.panel.transform,"增加血瓶",new Vector2(-105,-45));view.moreMana=Button(view.panel.transform,"增加蓝瓶",new Vector2(105,-45));view.close=Button(view.panel.transform,"继续游戏",new Vector2(0,-105));PrefabUtility.SaveAsPrefabAsset(ui,"Assets/Prefabs/UI/FlaskAllocation.prefab");Object.DestroyImmediate(ui);
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");var game=Object.FindObjectOfType<DemoGame>();game.manaFlaskItem=mana;var motor=Object.FindObjectOfType<PlayerMotor>();motor.skillPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/SkillOrb.prefab").GetComponent<SkillOrb>();PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
 var sceneHud=Object.FindObjectOfType<PlayerHudCanvas>();sceneHud.manaFill=sceneHud.transform.Find("血量与道具栏/蓝量背景/蓝量").GetComponent<Image>();PrefabUtility.RecordPrefabInstancePropertyModifications(sceneHud);
 if(Object.FindObjectOfType<FlaskAllocationCanvas>()==null)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/FlaskAllocation.prefab"),game.transform);
 var demo=Object.FindObjectOfType<DemoUI>();foreach(var text in demo.GetComponentsInChildren<Text>(true))if(text.text.Contains("按键")&&text.text.Contains("Space"))text.text+="\nU：方向白球    S＋Space：下跳平台";
 EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("DEMO_SKILL_AUTHORING_OK");
 }
 static RectTransform Rect(Transform parent,string name,Vector2 pos,Vector2 size){var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var r=obj.GetComponent<RectTransform>();r.anchoredPosition=pos;r.sizeDelta=size;return r;}
 static Text Label(Transform parent,string name,string value,Vector2 pos,Vector2 size,int fontSize){var t=Rect(parent,name,pos,size).gameObject.AddComponent<Text>();t.text=value;t.font=ChineseFont.Shared;t.fontSize=fontSize;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false;return t;}
 static Button Button(Transform parent,string name,Vector2 pos){var rect=Rect(parent,name,pos,new Vector2(180,42));rect.gameObject.AddComponent<Image>().color=new Color(.12f,.27f,.34f);var button=rect.gameObject.AddComponent<Button>();Label(rect,name,name,Vector2.zero,new Vector2(180,42),20);return button;}
}
