using System;
using UnityEngine;

namespace HollowDemo
{
    [CreateAssetMenu(menuName = "空洞原型/对话")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Choice
        {
            public string text;
            public int nextNode = -1;
            public bool openShop;
        }
        [Serializable]
        public sealed class Node
        {
            public string speaker;
            [TextArea(2, 8)] public string text;
            public Choice[] choices = Array.Empty<Choice>();
        }
        public Node[] nodes = Array.Empty<Node>();
    }
}
