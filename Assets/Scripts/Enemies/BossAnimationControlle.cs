using UnityEngine;
using HollowDemo;

[RequireComponent(typeof(WaveBoss))]
public sealed class BossAnimationControlle : MonoBehaviour
{
    [Header("Animator 参数名（Animator Controller 中需建同名 Bool 参数）")]
    public string pCoreExposed = "CoreExposed";

    [Header("备用 Animator（WaveBoss.bossAnimator 为空时使用）")]
    public Animator anim;

    WaveBoss boss;

    void Awake()
    {
        boss = GetComponent<WaveBoss>();
    }

    void Update()
    {
        Animator target = boss.bossAnimator != null ? boss.bossAnimator : anim;
        if (target == null) return;
        target.SetBool(pCoreExposed, boss.CanHitCore);
    }
}
