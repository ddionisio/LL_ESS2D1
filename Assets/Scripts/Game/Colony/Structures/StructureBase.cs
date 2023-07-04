using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StructureBase : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolSpawnComplete, M8.IPoolDespawn {

    [Header("Data")]    
    public int hitpoints; //for damageable, set to 0 for invulnerable

    //for buildable
    public float buildTime; //if buildable, set to 0 for spawning building (e.g. house)
    public float repairPerHitTime;

    [Header("Toggle Display")]
    public GameObject activeGO; //once placed/spawned
    public GameObject constructionGO; //while being built

    [Header("Animations")]
    public M8.Animator.Animate animator;

    [Header("Dimensions")]
    [SerializeField]
    Bounds _placementBounds;
    [SerializeField]
    Vector2 _spawnPoint; //local space, place to spawn any units
    [SerializeField]
    Vector2[] _waypoints; //local space, contains waypoints
    [SerializeField]
    Vector2[] _actionPoints; //local space, contains points for where units can take action
        
    public StructureState state { 
        get { return mState; }

        set {
            if(mState != value) {
                ClearCurrentState();

                mState = value;

                ApplyCurrentState();
            }
        }
    }

    public int hitpointsCurrent { 
        get { return mCurHitpoints; }
        set {
            var val = Mathf.Clamp(value, 0, hitpoints);
            if(mCurHitpoints != val) {
                mCurHitpoints = val;

                //damaged completely?

                //signal
            }
        }
    }

    public float buildTimeLeft { get; private set; }
    public float buildTimeScale { get; set; } //increased by engineers

    public bool isBuildable { get { return buildTime > 0f; } }
    public bool isDamageable { get { return hitpoints > 0; } }

    public Vector2 position {
        get { return transform.position; }
        set {
            if(position != value) {
                transform.position = value;

                RefreshPoints();
            }
        }
    }

    public Vector2 up { 
        get { return transform.up; }
        set { transform.up = value; }
    }

    public Collider2D coll { get; private set; }

    public Bounds placementBounds { get { return _placementBounds; } }
        
    /// <summary>
    /// In world space
    /// </summary>
    public GroundPoint spawnPoint { get; private set; }

    /// <summary>
    /// In world space
    /// </summary>
    public GroundPoint[] waypoints { get; private set; }

    /// <summary>
    /// In world space
    /// </summary>
    public GroundPoint[] actionPoints { get; private set; }

    protected Coroutine mRout;

    private StructureState mState;
    private int mCurHitpoints;

    protected virtual void Init() { }

    protected virtual void Deinit() { }

    protected virtual void Spawned() { }

    protected virtual void ClearCurrentState() {
        StopCurrentRout();

        switch(mState) {
            case StructureState.Demolish:
                if(constructionGO) constructionGO.SetActive(false);
                break;
        }
    }

    protected virtual void ApplyCurrentState() {

        switch(mState) {
            case StructureState.Spawning:
            case StructureState.Active:
                if(activeGO) activeGO.SetActive(true);
                break;

            case StructureState.Construction:
                if(activeGO) activeGO.SetActive(false);
                if(constructionGO) constructionGO.SetActive(true);

                buildTimeLeft = buildTime;
                buildTimeScale = 0f;

                mRout = StartCoroutine(DoConstruction());
                break;

            case StructureState.None:
                if(activeGO) activeGO.SetActive(false);
                if(constructionGO) constructionGO.SetActive(false);

                mCurHitpoints = 0;
                break;
        }
    }

    void M8.IPoolInit.OnInit() {
        coll = GetComponent<Collider2D>();

        mState = StructureState.None;
        if(activeGO) activeGO.SetActive(false);
        if(constructionGO) constructionGO.SetActive(false);

        mCurHitpoints = 0;

        Init();
    }

    void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
        if(parms != null) {
            if(parms.ContainsKey(StructureSpawnParams.spawnPoint))
                position = parms.GetValue<Vector2>(StructureSpawnParams.spawnPoint);
        }

        //set initial states
        Spawned();
    }

    void M8.IPoolSpawnComplete.OnSpawnComplete() {
        //determine state after spawn
        if(isBuildable)
            state = StructureState.Construction;
        else
            state = StructureState.Spawning;
    }

    void M8.IPoolDespawn.OnDespawned() {
        state = StructureState.None;

        Deinit();
    }

    IEnumerator DoConstruction() {
        while(buildTimeLeft > 0f) {
            yield return null;

            buildTimeLeft -= Time.deltaTime * buildTimeScale;
            if(buildTimeLeft < 0f)
                buildTimeLeft = 0f;
        }

        mRout = null;

        mCurHitpoints = hitpoints;

        state = StructureState.Active;
    }

    private void RefreshPoints() {
        GroundPoint outputPt;

        var pos = position;

        //spawn pt
        GroundPoint.GetGroundPoint(pos + _spawnPoint, out outputPt);
        spawnPoint = outputPt;

        //waypoints
        if(waypoints == null || waypoints.Length != _waypoints.Length) waypoints = new GroundPoint[_waypoints.Length];
        for(int i = 0; i < _waypoints.Length; i++) {
            GroundPoint.GetGroundPoint(pos + _waypoints[i], out outputPt);
            waypoints[i] = outputPt;
        }

        //action points
        if(actionPoints == null || actionPoints.Length != _actionPoints.Length) actionPoints = new GroundPoint[_actionPoints.Length];
        for(int i = 0; i < _actionPoints.Length; i++) {
            GroundPoint.GetGroundPoint(pos + _actionPoints[i], out outputPt);
            actionPoints[i] = outputPt;
        }
    }

    private void StopCurrentRout() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }
}
