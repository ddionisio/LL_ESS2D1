using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableJump : MovableBase {
    [Header("Jump Info")]
    public M8.RangeFloat heightRange;

    private Vector2 mMidPoint;

    protected override void MoveStart(Vector2 from, Vector2 to) {
        var topY = Mathf.Max(from.y, to.y);

        mMidPoint = new Vector2(Mathf.Lerp(from.x, to.x, 0.5f), topY + heightRange.random);
    }

    protected override Vector2 MoveUpdate(Vector2 from, Vector2 to, float t) {
        return M8.MathUtil.Bezier(from, mMidPoint, to, t);
    }
}
