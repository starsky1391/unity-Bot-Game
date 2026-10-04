using UnityEngine;

namespace HollowDemo
{
    public sealed class Checkpoint : Interactable
    {
        [HideInInspector] public int roomId;
        [HideInInspector] public string persistentId;
        public bool activated;
        public Transform spawnPoint;
        public override string Prompt => "[E] 休息并记录检查点";
        public override void Interact(DemoGame game) => game.ActivateCheckpoint(this);
    }

}
