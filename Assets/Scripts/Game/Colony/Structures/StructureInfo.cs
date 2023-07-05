using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StructureState {
    None,
    Active,
    Spawning,
    Construction,
    Damage, //when hitpoint is decreased (used as immunity window)
    Destroyed, //when hitpoint reaches 0
    Demolish, //when moving/deconstruct via UI
}

public struct StructureSpawnParams {
    public const string spawnPoint = "structureSpawnPt";
}

[System.Serializable]
public class StructureWaypoint {
    [SerializeField]
    Vector2 _point;

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