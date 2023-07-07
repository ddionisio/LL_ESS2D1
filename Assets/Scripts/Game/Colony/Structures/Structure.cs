using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolSpawnComplete, M8.IPoolDespawn {
    [System.Serializable]
    public struct WaypointGroup {
        public string name;
        public StructureWaypoint[] waypoints;
    }

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

    [M8.Animator.TakeSelector]
    public string takeSpawn;
    [M8.Animator.TakeSelector]
    public string takeDamage;
    [M8.Animator.TakeSelector]
    public string takeDestroyed;
    [M8.Animator.TakeSelector]
    public string takeDemolish;

    [Header("Dimensions")]
    [SerializeField]
    WaypointGroup[] _waypointGroups;

    public StructureData data { get; private set; }

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
                var prevHitpoints = mCurHitpoints;
                mCurHitpoints = val;

                //damaged completely?
                if(mCurHitpoints == 0)
                    state = StructureState.Destroyed;
                else if(mCurHitpoints < prevHitpoints)
                    state = StructureState.Damage;

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

                RefreshWaypoints();
            }
        }
    }

    public Vector2 up { 
        get { return transform.up; }
        set { transform.up = value; }
    }

    public Collider2D coll { get; private set; }
    public M8.PoolDataController poolCtrl { get; private set; }

    protected Coroutine mRout;

    private StructureState mState;
    private int mCurHitpoints;

    private Dictionary<string, StructureWaypoint[]> mWorldWaypoints;

    public StructureWaypoint[] GetWaypoints(string waypointName) {
        if(mWorldWaypoints == null)
            return null;

        StructureWaypoint[] ret;
        mWorldWaypoints.TryGetValue(waypointName, out ret);

        return ret;
    }

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
                if(activeGO) activeGO.SetActive(true);

                AnimateToState(takeSpawn, StructureState.Active);
                break;

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

            case StructureState.Damage:
                mRout = StartCoroutine(DoDamage());
                break;

            case StructureState.Destroyed:
                if(animator && !string.IsNullOrEmpty(takeDestroyed))
                    animator.Play(takeDestroyed);
                break;

            case StructureState.Demolish:
                mRout = StartCoroutine(DoDemolish());
                break;

            case StructureState.None:
                if(activeGO) activeGO.SetActive(false);
                if(constructionGO) constructionGO.SetActive(false);

                mCurHitpoints = 0;
                break;
        }
    }

    protected void AnimateToState(string take, StructureState toState) {
        mRout = StartCoroutine(DoAnimation(take, toState));
    }
        
    void M8.IPoolInit.OnInit() {
        coll = GetComponent<Collider2D>();
        poolCtrl = GetComponent<M8.PoolDataController>();

        //generate waypoints access
        mWorldWaypoints = new Dictionary<string, StructureWaypoint[]>(_waypointGroups.Length);

        for(int i = 0; i < _waypointGroups.Length; i++) {
            var waypointGrp = _waypointGroups[i];

            var waypointGrpPts = waypointGrp.waypoints;

            var waypoints = new StructureWaypoint[waypointGrpPts.Length];
            System.Array.Copy(waypointGrpPts, waypoints, waypoints.Length);

            mWorldWaypoints.Add(waypointGrp.name, waypoints);
        }

        //initial states
        mState = StructureState.None;
        if(activeGO) activeGO.SetActive(false);
        if(constructionGO) constructionGO.SetActive(false);

        mCurHitpoints = 0;

        Init();
    }

    void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
        if(parms != null) {
            if(parms.ContainsKey(StructureSpawnParams.data))
                data = parms.GetValue<StructureData>(StructureSpawnParams.data);

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

        if(mWorldWaypoints != null) { //clear out waypoint marks
            for(int i = 0; i < _waypointGroups.Length; i++) {
                var wps = _waypointGroups[i].waypoints;
                for(int j = 0; j < wps.Length; j++)
                    wps[j].ClearMarks();
            }
        }

        data = null;

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

    IEnumerator DoAnimation(string take, StructureState toState) {
        if(animator && !string.IsNullOrEmpty(take))
            yield return animator.PlayWait(take);

        mRout = null;

        state = toState;
    }

    IEnumerator DoDamage() {
        if(animator && !string.IsNullOrEmpty(takeDamage))
            animator.Play(takeDamage);

        var curTime = 0f;
        var delay = GameData.instance.structureDamageDelay;

        while(curTime < delay) {
            yield return null;

            curTime += Time.deltaTime;
        }

        mRout = null;

        state = StructureState.Active;
    }

    IEnumerator DoDemolish() {
        if(animator && !string.IsNullOrEmpty(takeDemolish))
            yield return animator.PlayWait(takeDemolish);

        mRout = null;

        poolCtrl.Release();
    }

    private void RefreshWaypoints() {
        var pos = position;

        for(int i = 0; i < _waypointGroups.Length; i++) {
            var wps = _waypointGroups[i].waypoints;
            for(int j = 0; j < wps.Length; j++)
                wps[j].RefreshWorldPoint(pos);
        }
    }

    private void StopCurrentRout() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }
}
