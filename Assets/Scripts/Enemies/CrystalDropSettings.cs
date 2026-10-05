using UnityEngine;
namespace HollowDemo
{
    [CreateAssetMenu(menuName="空洞原型/怪物晶石奖励")]
    public sealed class CrystalDropSettings:ScriptableObject
    {
        [Min(0)] public int amount=3;
    }
}
