using UnityEngine;
using System.Collections.Generic;

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
            var hints = new List<string>();
            if (doubleJump && !player.enableDoubleJump) hints.Add("二段跳：在空中再次按 Space");
            if (wallClimb && !player.enableWallClimb) hints.Add("爬墙：贴墙按 W / S，按 Space 蹬墙跳");
            if (dash && !player.enableDash) hints.Add("冲刺：按 Shift");
            if (grapple && !player.enableGrapple) hints.Add("钩爪：WASD 选择方向，按 K 抓取高亮钩点");
            player.enableDoubleJump |= doubleJump;
            player.enableWallClimb |= wallClimb;
            player.enableDash |= dash;
            player.enableGrapple |= grapple;
            game.RecordPickup(persistentId);
            gameObject.SetActive(false);
            game.SaveProgress();
            if (hints.Count > 0)
            {
                var keys = string.Join("\n", hints);
                if (AbilityHintCanvas.Instance != null) AbilityHintCanvas.Instance.Show(keys);
                else game.ShowNotice(keys);
            }
        }
    }
}
