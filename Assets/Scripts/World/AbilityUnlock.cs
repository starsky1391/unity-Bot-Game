using UnityEngine;

namespace HollowDemo
{
    public sealed class AbilityUnlock : Interactable
    {
        [HideInInspector] public string persistentId;
        [Header("解锁能力")]
        public bool doubleJump = true;
        public bool wallClimb, dash, grapple;
        public override string Prompt => "[E] 解锁能力";
        public override void Interact(DemoGame game)
        {
            if (game.PickupCollected(persistentId)) return;
            var player = game.Player;
            player.enableDoubleJump |= doubleJump;
            player.enableWallClimb |= wallClimb;
            player.enableDash |= dash;
            player.enableGrapple |= grapple;
            game.RecordPickup(persistentId);
            gameObject.SetActive(false);
            game.SaveProgress();
            game.ShowNotice("能力已解锁");
        }
    }
}
