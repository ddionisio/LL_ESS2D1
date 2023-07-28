using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleUnitSpawnerWaypoint : CycleUnitSpawnerBase {
    [SerializeField]
    Waypoint[] _waypoints;
    [SerializeField]
    bool _isGround;

    private int mCurWaypointInd;

    protected override void ApplySpawnParams(M8.GenericParams parms) {
        var wp = _waypoints[mCurWaypointInd];

        if(_isGround)
            parms[UnitSpawnParams.spawnPoint] = wp.groundPoint.position;
        else
            parms[UnitSpawnParams.spawnPoint] = wp.point;

        mCurWaypointInd++;
        if(mCurWaypointInd == _waypoints.Length)
            mCurWaypointInd = 0;
    }

    public override void Init() {
        base.Init();

        for(int i = 0; i < _waypoints.Length; i++)
            _waypoints[i].RefreshWorldPoint(transform.position);

        M8.ArrayUtil.Shuffle(_waypoints);
    }
}
