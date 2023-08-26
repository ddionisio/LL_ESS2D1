using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableGround : MovableBase {
    [Header("Ground Info")]
    public bool applyUpVector;

    private GroundPoint mGroundPt;

    protected override float MoveInit(Vector2 from, Vector2 to) {
        return Mathf.Abs(to.x - from.x);
    }

    protected override Vector2 MoveUpdate(Vector2 from, Vector2 to, float t) {
        if(GroundPoint.GetGroundPoint(Mathf.Lerp(from.x, to.x, t), out mGroundPt)) {
            isWater = mGroundPt.isWater;

            if(applyUpVector)
                transform.up = mGroundPt.up;

            return mGroundPt.position;
        }
        else //fail-safe
            return Vector2.Lerp(from, to, t);
    }
}
