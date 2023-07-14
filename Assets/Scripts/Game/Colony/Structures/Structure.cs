using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.EventSystems;

public class Structure : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolSpawnComplete, M8.IPoolDespawn, IPointerClickHandler {
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
    [SerializeField]
    bool _isDemolishable;
    
    [Header("Toggle Display")]
    public GameObject activeGO; //once placed/spawned
    public GameObject constructionGO; //while being built

    [Header("Overlay Display")]
    public Transform overlayActionAnchor; //set actions widget position here

    [Header("Animations")]
    public M8.Animator.Animate animator;

    [M8.Animator.TakeSelector]
    public string takeSpawn;
    [M8.Animator.TakeSelector]
    public string takeIdle;
    [M8.Animator.TakeSelector]
    public string takeDamage;
    [M8.Animator.TakeSelector]
    public string takeMoving;
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

                stateChangedCallback?.Invoke(mState);
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

    public bool isMovable { 
        get { 
            return moveCtrl && !moveCtrl.isLocked && !(state == StructureState.Destroyed || state == StructureState.Demolish); 
        } 
    }

    public bool isDemolishable {
        get {
            return _isDemolishable && !(state == StructureState.Demolish);
        }
    }

    public StructureAction actionFlags {
        get {
            var ret = StructureAction.None;

            if(isDemolishable)
                ret |= StructureAction.Demolish;

            if(moveCtrl)
                ret |= StructureAction.Move;

            return ret;
        }
    }

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

    /// <summary>
    /// In world position
    /// </summary>
    public Vector2 overlayAnchorPosition {
        get {
            Vector2 ret;

            if(overlayActionAnchor)
                ret = overlayActionAnchor.position;
            else //fail-safe, just use top of collider bounds
                ret = new Vector2(position.x + boxCollider.offset.x, position.y + boxCollider.offset.y + boxCollider.size.y*0.5f);

            return ret;
        }
    }

    public BoxCollider2D boxCollider { get; private set; }
    public M8.PoolDataController poolCtrl { get; private set; }

    public MovableBase moveCtrl { get; private set; }

    public bool isClicked { get; private set; }
        
    public event System.Action<StructureStatusInfo> statusUpdateCallback;
    public event System.Action<StructureState> stateChangedCallback;

    protected Coroutine mRout;

    private StructureState mState;
    private int mCurHitpoints;

    private Vector2 mMoveToPos;

    private Dictionary<string, StructureWaypoint[]> mWorldWaypoints;

    private StructureStatusInfo[] mStatusInfos;

    public StructureWaypoint[] GetWaypoints(string waypointName) {
        if(mWorldWaypoints == null)
            return null;

        StructureWaypoint[] ret;
        mWorldWaypoints.TryGetValue(waypointName, out ret);

        return ret;
    }

    public StructureStatusInfo GetStatusInfo(StructureStatus status) {
        return mStatusInfos[(int)status];
    }

    public float GetStatusProgress(StructureStatus status) {
        return mStatusInfos[(int)status].progress;
    }

    public void SetStatusProgress(StructureStatus status, float progress) {
        var inf = mStatusInfos[(int)status];

        var newProg = Mathf.Clamp01(progress);

        if(inf.progress != newProg) {
            inf.progress = newProg;

            mStatusInfos[(int)status] = inf;

            statusUpdateCallback?.Invoke(inf);
        }
    }

    public StructureStatusState GetStatusState(StructureStatus status) {
        return mStatusInfos[(int)status].state;
    }

    public void SetStatusState(StructureStatus status, StructureStatusState state) {
        var inf = mStatusInfos[(int)status];

        if(inf.state != state) {
            inf.state = state;

            mStatusInfos[(int)status] = inf;

            statusUpdateCallback?.Invoke(inf);
        }
    }

    public void MoveTo(Vector2 toPos) {
        if(!isMovable) return;

        mMoveToPos = toPos;

        state = StructureState.Moving;
    }

    public void Demolish() {
        if(!isDemolishable) return;

        state = StructureState.Demolish;
    }

    protected virtual void Init() { }

    protected virtual void Deinit() { }

    protected virtual void Spawned() { }

    protected virtual void ClearCurrentState() {
        StopCurrentRout();

        if(isClicked) {
            isClicked = false;
            GameData.instance.signalStructureClick?.Invoke(this);
        }

        switch(mState) {
            case StructureState.Demolish:
                if(constructionGO) constructionGO.SetActive(false);
                break;
        }
    }

    protected virtual void ApplyCurrentState() {

        var addPlacementBlocker = false;
        var physicsActive = true;

        switch(mState) {
            case StructureState.Spawning:
                if(activeGO) activeGO.SetActive(true);

                AnimateToState(takeSpawn, StructureState.Active);

                physicsActive = false;
                addPlacementBlocker = true;
                break;

            case StructureState.Active:
                if(activeGO) activeGO.SetActive(true);

                if(animator && !string.IsNullOrEmpty(takeIdle))
                    animator.Play(takeIdle);
                break;

            case StructureState.Construction:
                if(activeGO) activeGO.SetActive(false);
                if(constructionGO) constructionGO.SetActive(true);

                buildTimeLeft = buildTime;
                buildTimeScale = 0f;

                mRout = StartCoroutine(DoConstruction());

                physicsActive = false;
                addPlacementBlocker = true;
                break;

            case StructureState.MoveReady:
                //animation?

                physicsActive = false;
                break;

            case StructureState.Moving:
                mRout = StartCoroutine(DoMove());

                physicsActive = false;
                addPlacementBlocker = true;
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

                physicsActive = false;
                break;

            case StructureState.None:
                if(animator)
                    animator.Stop();

                if(activeGO) activeGO.SetActive(false);
                if(constructionGO) constructionGO.SetActive(false);

                mCurHitpoints = 0;

                physicsActive = false;
                break;
        }

        if(boxCollider)
            boxCollider.enabled = physicsActive;

        var structureCtrl = ColonyController.instance.structureController;

        if(addPlacementBlocker) {
            if(state == StructureState.Moving) //special case
                structureCtrl.PlacementAddBlocker(this, mMoveToPos);
            else
                structureCtrl.PlacementAddBlocker(this);
        }
        else
            structureCtrl.PlacementRemoveBlocker(this);
    }

    protected void AnimateToState(string take, StructureState toState) {
        mRout = StartCoroutine(DoAnimation(take, toState));
    }
        
    void M8.IPoolInit.OnInit() {        
        poolCtrl = GetComponent<M8.PoolDataController>();

        moveCtrl = GetComponent<MovableBase>();

        boxCollider = GetComponent<BoxCollider2D>();
        if(boxCollider)
            boxCollider.enabled = false;

        //generate waypoints access
        mWorldWaypoints = new Dictionary<string, StructureWaypoint[]>(_waypointGroups.Length);

        for(int i = 0; i < _waypointGroups.Length; i++) {
            var waypointGrp = _waypointGroups[i];

            var waypointGrpPts = waypointGrp.waypoints;

            var waypoints = new StructureWaypoint[waypointGrpPts.Length];
            System.Array.Copy(waypointGrpPts, waypoints, waypoints.Length);

            mWorldWaypoints.Add(waypointGrp.name, waypoints);
        }

        //initialize status
        var statusEnumNames = System.Enum.GetNames(typeof(StructureStatus));

        mStatusInfos = new StructureStatusInfo[statusEnumNames.Length];

        for(int i = 0; i < mStatusInfos.Length; i++)
            mStatusInfos[i] = new StructureStatusInfo((StructureStatus)i);

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

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        isClicked = true;
        GameData.instance.signalStructureClick?.Invoke(this);
    }

    IEnumerator DoConstruction() {
        if(buildTimeLeft > 0f) {
            while(buildTimeLeft > 0f) {
                yield return null;

                buildTimeLeft -= Time.deltaTime * buildTimeScale;
                if(buildTimeLeft < 0f)
                    buildTimeLeft = 0f;
            }
        }
        else
            yield return null;

        mRout = null;

        mCurHitpoints = hitpoints;

        state = StructureState.Active;
    }

    IEnumerator DoAnimation(string take, StructureState toState) {
        if(animator && !string.IsNullOrEmpty(take))
            yield return animator.PlayWait(take);
        else
            yield return null;

        mRout = null;

        state = toState;
    }

    IEnumerator DoDamage() {
        if(animator && !string.IsNullOrEmpty(takeDamage))
            animator.Play(takeDamage);

        var delay = GameData.instance.structureDamageDelay;
        if(delay > 0f) {
            var curTime = 0f;

            while(curTime < delay) {
                yield return null;

                curTime += Time.deltaTime;
            }
        }
        else
            yield return null;

        mRout = null;

        state = StructureState.Active;
    }

    IEnumerator DoDemolish() {
        if(animator && !string.IsNullOrEmpty(takeDemolish))
            yield return animator.PlayWait(takeDemolish);
        else
            yield return null;

        mRout = null;

        poolCtrl.Release();
    }

    IEnumerator DoMove() {
        yield return null;

        if(animator && !string.IsNullOrEmpty(takeMoving))
            animator.Play(takeMoving);

        moveCtrl.Move(mMoveToPos);

        while(moveCtrl.isMoving)
            yield return null;

        mRout = null;

        state = StructureState.Active;
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
