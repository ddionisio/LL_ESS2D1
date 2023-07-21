using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Waypoint {
    [SerializeField]
    Vector2 _point;

    public Vector2 pointLocal { get { return _point; } }

    /// <summary>
    /// World space
    /// </summary>
    public Vector2 point { get { return mWorldPt; } }

    /// <summary>
    /// World space
    /// </summary>
    public GroundPoint groundPoint {
        get {
            if(!mGroundPt.HasValue) {
                GroundPoint gpt;
                GroundPoint.GetGroundPoint(mWorldPt, out gpt);
                mGroundPt = gpt;
                return gpt;
            }

            return mGroundPt.Value;
        }
    }

    public bool isMarked { get { return mMark > 0; } }

    public void AddMark() {
        mMark++;
    }

    public void RemoveMark() {
        if(mMark > 0)
            mMark--;
    }

    public void ClearMarks() {
        mMark = 0;
    }

    /// <summary>
    /// Refresh world point with given origin, and reset ground point
    /// </summary>
    public void RefreshWorldPoint(Vector2 origin) {
        mWorldPt = origin + _point;
        mGroundPt = null;
    }

    private int mMark;
    private Vector2 mWorldPt;
    private GroundPoint? mGroundPt;
}

[System.Serializable]
public struct WaypointGroup {
    public string name;
    public Waypoint[] waypoints;
}

public class WaypointControl {
    public Waypoint[] waypoints { get; private set; }

    private Waypoint[] mShuffleWaypoints;
    private int mShuffleInd = 0;

    public WaypointControl(Waypoint[] aWaypoints) {
        waypoints = aWaypoints;

        mShuffleWaypoints = new Waypoint[waypoints.Length];
        System.Array.Copy(waypoints, mShuffleWaypoints, mShuffleWaypoints.Length);
        M8.ArrayUtil.Shuffle(mShuffleWaypoints);
    }

    public Waypoint GetUnmarkedWaypoint() {
        for(int i = 0; i < waypoints.Length; i++) {
            var wp = waypoints[i];
            if(!wp.isMarked)
                return wp;
        }

        return null;
    }

    public Waypoint GetRandomWaypoint(bool checkMarked) {
        if(mShuffleInd >= mShuffleWaypoints.Length) {
            M8.ArrayUtil.Shuffle(mShuffleWaypoints);
            mShuffleInd = 0;
        }

        Waypoint ret;

        if(checkMarked) {
            ret = null;
            for(int i = 0; i < mShuffleWaypoints.Length; i++) {
                var wp = mShuffleWaypoints[mShuffleInd];

                mShuffleInd++;
                if(mShuffleInd >= mShuffleWaypoints.Length)
                    mShuffleInd = 0;

                if(!wp.isMarked) {
                    ret = wp;
                    break;
                }
            }

            //everything is marked, just return with the current shuffle index
            if(ret == null) {
                ret = mShuffleWaypoints[mShuffleInd];
                mShuffleInd++;
            }
        }
        else {
            ret = mShuffleWaypoints[mShuffleInd];
            mShuffleInd++;
        }

        return ret;
    }
}