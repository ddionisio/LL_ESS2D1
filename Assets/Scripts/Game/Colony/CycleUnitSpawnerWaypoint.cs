using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleUnitSpawnerWaypoint : CycleUnitSpawnerBase {
    public enum MoveDirAxis {
        None,
        Horizontal,
        Vertical
    }

    [SerializeField]
    Waypoint[] _waypoints;
    [SerializeField]
    bool _isGround;
    [SerializeField]
    MoveDirAxis _moveDirAxis = MoveDirAxis.Horizontal;

    private int mCurWaypointInd;

    protected override void ApplySpawnParams(M8.GenericParams parms) {
        var wp = _waypoints[mCurWaypointInd];

        Vector2 pos;

        if(_isGround)
            pos = wp.groundPoint.position;
        else
            pos = wp.point;

        parms[UnitSpawnParams.spawnPoint] = pos;

        //determine direction of movement based on location of waypoint relative to camera's
        if(_moveDirAxis != MoveDirAxis.None) {
            var colonyCtrl = ColonyController.instance;
            var camPos = colonyCtrl.mainCameraTransform.position;
            var dir = DirType.None;

            switch(_moveDirAxis) {
                case MoveDirAxis.Horizontal:
                    if(pos.x < camPos.x)
                        dir = DirType.Right;
                    else
                        dir = DirType.Left;
                    break;

                case MoveDirAxis.Vertical:
                    if(pos.y < camPos.y)
                        dir = DirType.Up;
                    else
                        dir = DirType.Down;
                    break;
            }

            parms[UnitSpawnParams.moveDirType] = dir;
        }

        mCurWaypointInd++;
        if(mCurWaypointInd == _waypoints.Length)
            mCurWaypointInd = 0;
    }

    protected override void Init() {
        base.Init();

        for(int i = 0; i < _waypoints.Length; i++)
            _waypoints[i].RefreshWorldPoint(transform.position);

        M8.ArrayUtil.Shuffle(_waypoints);
    }
}
