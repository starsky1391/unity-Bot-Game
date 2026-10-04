using UnityEngine;

namespace HollowDemo
{
    public sealed class Npc : Interactable
    {
        public string displayName;
        public string mapLandmarkId;
        public DialogueDefinition dialogue;
        public ShopDefinition shop;
        public override string Prompt => "[E] 与" + displayName + "交谈";
        public override void Interact(DemoGame game) => game.StartDialogue(this);
    }
}
