using UnityEngine;

namespace HollowDemo
{
    public sealed class Checkpoint : Interactable
    {
        [HideInInspector] public int roomId;
        [HideInInspector] public string persistentId;
        public bool activated;
        [InspectorName("初始出生点")]
        [Tooltip("每个关卡只勾选一个；仅决定出生位置，不会自动激活检查点")]
        public bool isInitialSpawn;
        public Transform spawnPoint;
        public override string Prompt => "[E] 休息并记录检查点";
        public override void Interact(DemoGame game) { game.ActivateCheckpoint(this); game.SetScreen(GameScreen.Checkpoint); }
    }

}
