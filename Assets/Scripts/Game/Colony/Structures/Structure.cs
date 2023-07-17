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

    public int workCapacity = 2; //how many can work on this structure

    public bool isReparable;

    [SerializeField]
    bool _isDemolishable;
    
    [Header("Toggle Display")]
    public GameObject activeGO; //once placed/spawned
    public GameObject constructionGO; //while being built
    public GameObject damagedGO; //when hp < hp max
    public GameObject repairGO; //when there are workers actively repairing (workCount > 0)

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
                
                if(mCurHitpoints > prevHitpoints) { //healed?
                    //remove damage display
                    if(mCurHitpoints == hitpoints && damagedGO)
                        damagedGO.SetActive(false);
                }
                else if(mCurHitpoints == 0) //damaged completely?
                    state = StructureState.Destroyed;
                else if(mCurHitpoints < prevHitpoints) //perform damage
                    state = StructureState.Damage;

                //signal
            }
        }
    }

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

    /// <summary>
    /// Current works going on this structure (added/subtracted by Units)
    /// </summary>
    public int workCount { get; private set; }
        
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

    public void ResetAllStatus() {
        for(int i = 0; i < mStatusInfos.Length; i++) {
            var inf = mStatusInfos[i];

            inf.state = StructureStatusState.None;
            inf.progress = 0f;

            mStatusInfos[i] = inf;

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

    public virtual void WorkAdd() {
        if(workCount < workCapacity)
            workCount++;
    }

    public virtual void WorkRemove() {
        if(workCount > 0)
            workCount--;
    }

    protected virtual void Init() { }

    protected virtual void Despawned() { }

    protected virtual void Spawned() { }

    protected virtual void ClearCurrentState() {
        StopCurrentRout();

        if(isClicked) {
            isClicked = false;
            GameData.instance.signalStructureClick?.Invoke(this);
        }

        switch(mState) {
            case StructureState.Construction:
                if(constructionGO) constructionGO.SetActive(false);
                break;

            case StructureState.Repair:
                if(repairGO) repairGO.SetActive(false);
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

            case StructureState.Repair:
                mRout = StartCoroutine(DoRepair());
                break;

            case StructureState.Damage:
                if(damagedGO) damagedGO.SetActive(true);

                mRout = StartCoroutine(DoDamage());
                break;

            case StructureState.Destroyed:
                if(damagedGO) damagedGO.SetActive(true);

                //if repairable, simply play animation and show repair status
                if(isReparable) {
                    SetStatusState(StructureStatus.Construct, StructureStatusState.Require);

                    if(animator && !string.IsNullOrEmpty(takeDestroyed))
                        animator.Play(takeDestroyed);
                }
                else //release after destroyed animation (NOTE: make sure that "takeDestroyed" is not a loop animation)
                    AnimateToRelease(takeDestroyed);
                break;

            case StructureState.Demolish:
                if(damagedGO) damagedGO.SetActive(true);

                AnimateToRelease(takeDemolish);

                physicsActive = false;
                break;

            case StructureState.None:
                ResetAllStatus();

                if(animator)
                    animator.Stop();

                if(activeGO) activeGO.SetActive(false);
                if(constructionGO) constructionGO.SetActive(false);
                if(damagedGO) damagedGO.SetActive(false);
                if(repairGO) repairGO.SetActive(false);

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
        mRout = StartCoroutine(DoAnimationToState(take, toState));
    }

    protected void AnimateToRelease(string take) {
        mRout = StartCoroutine(DoAnimationToRelease(take));
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
        if(damagedGO) damagedGO.SetActive(false);
        if(repairGO) repairGO.SetActive(false);

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

        Despawned();
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        GameData.instance.signalClickCategory?.Invoke(GameData.clickCategoryStructure);

        isClicked = true;
        GameData.instance.signalStructureClick?.Invoke(this);
    }

    IEnumerator DoConstruction() {
        var workTimeLeft = buildTime;

        if(workTimeLeft > 0f) {
            var timeScalePerWork = GameData.instance.structureBuildScalePerWork;

            while(workTimeLeft > 0f) {
                //no worker?
                if(workCount == 0) {
                    SetStatusState(StructureStatus.Construct, StructureStatusState.Require);

                    while(workCount == 0)
                        yield return null;

                    SetStatusState(StructureStatus.Construct, StructureStatusState.Progress);
                }

                //progress
                yield return null;

                var buildTimeScale = timeScalePerWork * workCount;

                workTimeLeft -= Time.deltaTime * buildTimeScale;
                if(workTimeLeft < 0f)
                    workTimeLeft = 0f;

                SetStatusProgress(StructureStatus.Construct, 1.0f - Mathf.Clamp01(workTimeLeft/buildTime));
            }
        }
        else
            yield return null;

        mRout = null;

        mCurHitpoints = hitpoints;

        SetStatusState(StructureStatus.Construct, StructureStatusState.None);

        state = StructureState.Active;
    }

    IEnumerator DoRepair() {
        var gameDat = GameData.instance;

        var repairScalePerWork = gameDat.structureRepairScalePerWork;
        var repairPerHPDelay = gameDat.structureRepairPerHitDelay;

        var curHP = mCurHitpoints;
        var repairCount = hitpoints - curHP;

        var totalDelay = repairCount * repairPerHPDelay;

        var curTime = 0f;

        while(curTime < totalDelay && mCurHitpoints < hitpoints) {
            //no worker?
            if(workCount == 0) {
                SetStatusState(StructureStatus.Construct, StructureStatusState.Require);

                while(workCount == 0)
                    yield return null;

                SetStatusState(StructureStatus.Construct, StructureStatusState.Progress);
            }

            //progress
            yield return null;

            var repairTimeScale = repairScalePerWork * workCount;

            curTime += Time.deltaTime * repairTimeScale;

            var t = Mathf.Clamp01(curTime / totalDelay);

            SetStatusProgress(StructureStatus.Construct, t);

            hitpointsCurrent = curHP + Mathf.FloorToInt(repairCount * t);
        }

        mRout = null;

        SetStatusState(StructureStatus.Construct, StructureStatusState.None);

        if(mCurHitpoints == 0) //fail-safe
            state = StructureState.Destroyed;
        else
            state = StructureState.Active;
    }

    IEnumerator DoAnimationToState(string take, StructureState toState) {
        if(animator && !string.IsNullOrEmpty(take))
            yield return animator.PlayWait(take);
        else
            yield return null;

        mRout = null;

        state = toState;
    }

    IEnumerator DoAnimationToRelease(string take) {
        if(animator && !string.IsNullOrEmpty(take))
            yield return animator.PlayWait(take);
        else
            yield return null;

        mRout = null;

        poolCtrl.Release();
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
