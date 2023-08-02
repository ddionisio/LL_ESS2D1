using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorFear : SensorBase {
    [Header("Fear Info")]
    public int fearLevel = 1;

    protected override bool Process(Collider2D coll) {
        var unit = coll.GetComponent<Unit>();
        if(unit) {
            if(unit.data.courageLevel < fearLevel)
                unit.RetreatFrom(transform.position);
        }

        return true;
    }
}
