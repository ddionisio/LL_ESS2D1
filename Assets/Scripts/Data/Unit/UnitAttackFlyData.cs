using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitFly", menuName = "Game/Unit/Attack Fly")]
public class UnitAttackFlyData : UnitData {
    [Header("Fly Info")]
    public float flyWaitRadius;
    public float flyWaitDelay;

    public float flyWaitMoveDelay;
    public DG.Tweening.Ease flyWaitMoveEase = DG.Tweening.Ease.InOutSine;

    public float flyGrabDelay = 1f;
    public DG.Tweening.Ease flyGrabEase = DG.Tweening.Ease.OutSine;
}
