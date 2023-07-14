using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StructureState {
    None,
    Active,
    Spawning,
    Construction,
    Repair, //repairing
    Damage, //when hitpoint is decreased (used as immunity window)
    MoveReady, //during placement when move action is selected
    Moving, //for moveable structures, start moving
    Destroyed, //when hitpoint reaches 0
    Demolish, //when moving/deconstruct via UI
}

public enum StructureStatus {
    Construct, //build/repair
    Food,
    Water,
    Power,
}

public enum StructureStatusState {
    None,
    Progress,
    Require
}

[System.Flags]
public enum StructureAction {
    None = 0x0,

    Move = 0x1,
    Demolish = 0x2,

    //Upgrade?
}

public struct StructureSpawnParams {
    public const string data = "structureData"; //StructureData
    public const string spawnPoint = "structureSpawnP"; //Vector2
    public const string spawnNormal = "structureSpawnN"; //Vector2
}

public struct StructureStatusInfo {
    public StructureStatus type;
    public StructureStatusState state;
    public float progress; //[0, 1]

    public StructureStatusInfo(StructureStatus aType) {
        type = aType;
        state = StructureStatusState.None;
        progress = 0f;
    }
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