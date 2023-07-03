using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StructureBase : MonoBehaviour {
    [Header("Data")]
    [M8.EnumMask]
    public StructureFlags flags;

    //for damageable
    public int hitpoints;

    //for buildable
    public float buildTime;
    public float repairPerHitTime;

    [Header("Toggle Display")]
    public GameObject activeGO; //once placed/spawned
    public GameObject ghostGO; //placement
    public GameObject constructionGO; //while being built

    [Header("Points")]
    public Transform spawnPointRoot; //place to spawn any units
    public Transform waypointsRoot; //root that contains waypoints
    public Transform actionPointsRoot; //root that contains points for where units can take action

    private BoxCollider2D mBoxColl;
}
