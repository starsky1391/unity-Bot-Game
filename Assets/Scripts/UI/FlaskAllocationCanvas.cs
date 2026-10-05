using UnityEngine;
using UnityEngine.UI;
namespace HollowDemo {
 public sealed class FlaskAllocationCanvas : MonoBehaviour {
  public GameObject panel;
  public Text counts;
  public Button moreBlood,moreMana,close;
  void Awake(){foreach(var text in GetComponentsInChildren<Text>(true))text.font=ChineseFont.Shared;moreBlood.onClick.AddListener(()=>DemoGame.Instance.ChangeFlaskAllocation(-1));moreMana.onClick.AddListener(()=>DemoGame.Instance.ChangeFlaskAllocation(1));close.onClick.AddListener(()=>DemoGame.Instance.SetScreen(GameScreen.None));var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=1;}
  void LateUpdate(){var game=DemoGame.Instance;panel.SetActive(game.Screen==GameScreen.Checkpoint);counts.text="血瓶 "+game.BloodFlaskCapacity+"   /   蓝瓶 "+game.ManaFlaskCapacity+"\n容器总容量 "+game.FlaskCapacity;}
 }
}
