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
            var val = Mathf.Clamp(value, 0, hitpointsMax);
            if(mCurHitpoints != val) {
                var prevHitpoints = mCurHitpoints;
                mCurHitpoints = val;

                if(mCurHitpoints > prevHitpoints) { //healed?
                    //remove damage display
                    if(mCurHitpoints == hitpointsMax && damagedGO)
                        damagedGO.SetActive(false);
                }
                else if(state != StructureState.Demolish) { //already being demolished
                    //damaged completely?
                    if(mCurHitpoints == 0)
                        state = StructureState.Destroyed;
                    //perform damage
                    else if(mCurHitpoints < prevHitpoints)
                        state = StructureState.Damage;
                }

                //signal
            }
        }
    }

    public int hitpointsMax { get { return data.hitpoints; } }

    public bool isBuildable { get { return data.buildTime > 0f; } }

    public bool isDamageable { get { return data.hitpoints > 0 && (state == StructureState.Active || state == StructureState.Repair || (state == StructureState.Demolish && mIsDemolishInProcess)); } }

    public bool isMovable { 
        get { 
            return moveCtrl && !moveCtrl.isLocked && !(state == StructureState.Destroyed || state == StructureState.Demolish); 
        } 
    }

    public bool isDemolishable {
        get {
            return data.isDemolishable && !(state == StructureState.Demolish);
        }
    }

    public virtual StructureAction actionFlags {
        get {
            var ret = StructureAction.None;

            if(isDemolishable)
                ret |= StructureAction.Demolish;

            if(moveCtrl)
                ret |= StructureAction.Move;

            //cancellable actions
            var canCancel = false;

            switch(state) {
                case StructureState.Demolish:
                    if(mIsDemolishInProcess)
                        canCancel = true;
                    break;
            }

            if(canCancel)
                ret |= StructureAction.Cancel;

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
    public int workCapacity { get { return data.workCapacity; } }
    public bool workIsFull { get { return workCount >= data.workCapacity; } }
        
    public event System.Action<StructureStatusInfo> statusUpdateCallback;
    public event System.Action<StructureState> stateChangedCallback;

    protected Coroutine mRout;

    private StructureState mState;
    private int mCurHitpoints;

    private bool mIsDemolishInProcess; //during demolish state

    private Dictionary<string, StructureWaypoint[]> mWorldWaypoints;

    private StructureStatusInfo[] mStatusInfos;

    protected int mTakeSpawnInd = -1;
    protected int mTakeIdleInd = -1;
    protected int mTakeDamageInd = -1;
    protected int mTakeMovingInd = -1;
    protected int mTakeDestroyedInd = -1;
    protected int mTakeDemolishInd = -1;

    public StructureWaypoint[] GetWaypoints(string waypointName) {
        if(mWorldWaypoints == null)
            return null;

        StructureWaypoint[] ret;
        mWorldWaypoints.TryGetValue(waypointName, out ret);

        return ret;
    }

    public StructureWaypoint GetWaypointRandom(string waypointName) {
        if(mWorldWaypoints == null)
            return null;

        StructureWaypoint[] ret;
        if(mWorldWaypoints.TryGetValue(waypointName, out ret))
            return ret.Length > 0 ? ret[Random.Range(0, ret.Length)] : null;

        return null;
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

            if(inf.state != StructureStatusState.None) {
                inf.state = StructureStatusState.None;
                inf.progress = 0f;

                mStatusInfos[i] = inf;

                statusUpdateCallback?.Invoke(inf);
            }
        }
    }

    public void MoveTo(Vector2 toPos) {
        if(!isMovable) return;

        moveCtrl.moveDestination = toPos;

        state = StructureState.Moving;
    }

    public void Demolish() {
        if(!isDemolishable) return;

        state = StructureState.Demolish;
    }

    /// <summary>
    /// Cancel any action that is cancellable
    /// </summary>
    public void CancelAction() {
        switch(state) {
            case StructureState.Demolish:
                if(mIsDemolishInProcess) {
                    if(mCurHitpoints == 0)
                        state = StructureState.Destroyed;
                    else
                        state = StructureState.Active;
                }
                break;
        }
    }

    public virtual void WorkAdd() {
        if(!workIsFull)
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

        if(moveCtrl) moveCtrl.Cancel();

        if(isClicked) {
            isClicked = false;
            GameData.instance.signalStructureClick?.Invoke(this);
        }

        switch(mState) {
            case StructureState.Construction:
                if(constructionGO) constructionGO.SetActive(false);

                SetStatusState(StructureStatus.Construct, StructureStatusState.None);
                break;

            case StructureState.Repair:
                if(repairGO) repairGO.SetActive(false);

                SetStatusState(StructureStatus.Construct, StructureStatusState.None);
                break;

            case StructureState.Destroyed:
                SetStatusState(StructureStatus.Construct, StructureStatusState.None);
                break;

            case StructureState.Demolish:
                SetStatusState(StructureStatus.Demolish, StructureStatusState.None);
                break;
        }
    }

    protected virtual void ApplyCurrentState() {

        var addPlacementBlocker = false;
        var physicsActive = true;

        switch(mState) {
            case StructureState.Spawning:
                if(activeGO) activeGO.SetActive(true);

                AnimateToState(mTakeSpawnInd, StructureState.Active);

                physicsActive = false;
                addPlacementBlocker = true;
                break;

            case StructureState.Active:
                if(activeGO) activeGO.SetActive(true);

                if(damagedGO) damagedGO.SetActive(mCurHitpoints < hitpointsMax);

                if(mTakeIdleInd != -1)
                    animator.Play(mTakeIdleInd);
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
                if(data.isReparable) {
                    SetStatusState(StructureStatus.Construct, StructureStatusState.Require);

                    if(mTakeDestroyedInd != -1)
                        animator.Play(mTakeDestroyedInd);
                }
                else //release after destroyed animation (NOTE: make sure that "takeDestroyed" is not a loop animation)
                    AnimateToRelease(mTakeDestroyedInd);
                break;

            case StructureState.Demolish:
                if(damagedGO) damagedGO.SetActive(true);

                mRout = StartCoroutine(DoDemolish());
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

                up = Vector2.up;

                physicsActive = false;
                break;
        }

        if(boxCollider)
            boxCollider.enabled = physicsActive;

        var structureCtrl = ColonyController.instance.structurePaletteController;

        if(addPlacementBlocker) {
            if(state == StructureState.Moving && !moveCtrl) //special case
                structureCtrl.PlacementAddBlocker(this, moveCtrl.moveDestination);
            else
                structureCtrl.PlacementAddBlocker(this);
        }
        else
            structureCtrl.PlacementRemoveBlocker(this);
    }

    protected void AnimateToState(int takeInd, StructureState toState) {
        mRout = StartCoroutine(DoAnimationToState(takeInd, toState));
    }

    protected void AnimateToRelease(int takeInd) {
        mRout = StartCoroutine(DoAnimationToRelease(takeInd));
    }
        
    void M8.IPoolInit.OnInit() {        
        poolCtrl = GetComponent<M8.PoolDataController>();

        moveCtrl = GetComponent<MovableBase>();

        boxCollider = GetComponent<BoxCollider2D>();
        if(boxCollider)
            boxCollider.enabled = false;

        //initialize animation
        if(animator) {
            mTakeSpawnInd = animator.GetTakeIndex(takeSpawn);
            mTakeIdleInd = animator.GetTakeIndex(takeIdle);
            mTakeDamageInd = animator.GetTakeIndex(takeDamage);
            mTakeMovingInd = animator.GetTakeIndex(takeMoving);
            mTakeDestroyedInd = animator.GetTakeIndex(takeDestroyed);
            mTakeDemolishInd = animator.GetTakeIndex(takeDemolish);
        }

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
                transform.position = parms.GetValue<Vector2>(StructureSpawnParams.spawnPoint);
        }

        mCurHitpoints = hitpointsMax;

        RefreshWaypoints();

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
        var buildTime = data.buildTime;
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

        SetStatusState(StructureStatus.Construct, StructureStatusState.None);

        state = StructureState.Active;
    }

    IEnumerator DoRepair() {
        var gameDat = GameData.instance;

        var repairScalePerWork = gameDat.structureRepairScalePerWork;
        var repairPerHPDelay = gameDat.structureRepairPerHitDelay;

        var curHP = mCurHitpoints;
        var repairCount = hitpointsMax - curHP;

        var totalDelay = repairCount * repairPerHPDelay;

        var curTime = 0f;

        while(curTime < totalDelay && mCurHitpoints < hitpointsMax) {
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

    IEnumerator DoAnimationToState(int takeInd, StructureState toState) {
        if(takeInd != -1)
            yield return animator.PlayWait(takeInd);
        else
            yield return null;

        mRout = null;

        state = toState;
    }

    IEnumerator DoAnimationToRelease(int takeInd) {
        if(takeInd != -1)
            yield return animator.PlayWait(takeInd);
        else
            yield return null;

        mRout = null;

        poolCtrl.Release();
    }

    IEnumerator DoDamage() {
        if(mTakeDamageInd != -1)
            animator.Play(mTakeDamageInd);

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

        if(mTakeMovingInd != -1)
            animator.Play(mTakeMovingInd);

        moveCtrl.Move();

        while(moveCtrl.isMoving)
            yield return null;

        mRout = null;

        state = StructureState.Active;
    }

    IEnumerator DoDemolish() {
        var delay = GameData.instance.structureDemolishDelay;
        if(delay > 0f) {
            SetStatusState(StructureStatus.Demolish, StructureStatusState.Progress);

            mIsDemolishInProcess = true;

            //wait
            var curTime = 0f;
            while(curTime < delay) {
                yield return null;

                curTime += Time.deltaTime;

                SetStatusProgress(StructureStatus.Demolish, Mathf.Clamp01(curTime / delay));
            }

            mIsDemolishInProcess = false;

            SetStatusState(StructureStatus.Demolish, StructureStatusState.None);
        }
        else
            yield return null;

        if(boxCollider)
            boxCollider.enabled = false;

        //proceed to release
        if(mTakeDemolishInd != -1)
            yield return animator.PlayWait(mTakeDemolishInd);

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
