using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.EventSystems;

public class Structure : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolSpawnComplete, M8.IPoolDespawn, IPointerClickHandler {
    [Header("Toggle Display")]
    public GameObject activeGO; //once placed/spawned
    public GameObject constructionGO; //while being built
    public GameObject damagedGO; //when hp < hp max
    public GameObject repairGO; //when there are workers actively repairing (workCount > 0)

    [Header("Animations")]
    public M8.Animator.Animate animator;

    [M8.Animator.TakeSelector]
    public int takeSpawn = -1;
    [M8.Animator.TakeSelector]
    public int takeIdle = -1;
    [M8.Animator.TakeSelector]
    public int takeDamage = -1;
    [M8.Animator.TakeSelector]
    public int takeMoving = -1;
    [M8.Animator.TakeSelector]
    public int takeMoveFinish = -1;
    [M8.Animator.TakeSelector]
    public int takeDestroyed = -1;
    [M8.Animator.TakeSelector]
    public int takeDemolish = -1;
    [M8.Animator.TakeSelector]
    public int takeVictory = -1;

    [Header("SFX")]
    [M8.SoundPlaylist]
    public string sfxConstructComplete;
    [M8.SoundPlaylist]
    public string sfxRepairComplete;
    [M8.SoundPlaylist]
    public string sfxHit;
    [M8.SoundPlaylist]
    public string sfxDemolish;
    [M8.SoundPlaylist]
    public string sfxDestroy;

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
                //prevent getting damaged (fail-safe, maybe check from outside instead?)
                if(val < mCurHitpoints && !isDamageable)
                    return;

                var prevHitpoints = mCurHitpoints;
                mCurHitpoints = val;

                HitpointsChanged(prevHitpoints);

                //signal
            }
        }
    }

    public int hitpointsMax { get { return data.hitpoints; } }

    public bool isBuildable { get { return data.buildTime > 0f; } }

    public bool isReparable { get { return data.isReparable; } }

    public bool isDamageable { get { return data.hitpoints > 0 && (state == StructureState.Active || state == StructureState.Repair || (state == StructureState.Demolish && mIsDemolishInProcess)); } }

    public bool canEngineer { get { return state == StructureState.Construction || ((state == StructureState.Active || state == StructureState.Destroyed || state == StructureState.Repair) && hitpointsCurrent < hitpointsMax && isReparable); } }

    public bool isMovable { 
        get { 
            return moveCtrl && !moveCtrl.isLocked && !(state == StructureState.Destroyed || state == StructureState.Demolish || state == StructureState.Construction); 
        } 
    }

    public bool isDemolishable {
        get {
            return data.isDemolishable && !(state == StructureState.Demolish || state == StructureState.Construction);
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
                case StructureState.Construction:
                    canCancel = true;
                    break;
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
    public Vector2 clickPosition { get { return mClickPosition; } }

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

    /// <summary>
    /// Check if this unit has been marked (used for targeting by AI)
    /// </summary>
    public int markCount { get { return mMark; } }

    public event System.Action<StructureStatusInfo> statusUpdateCallback;
    public event System.Action<StructureState> stateChangedCallback;

    protected Coroutine mRout;

    private StructureState mState;
    private int mCurHitpoints;

    private bool mIsDemolishInProcess; //during demolish state

    private Dictionary<string, WaypointControl> mWorldWaypoints;

    private StructureStatusInfo[] mStatusInfos;

    private int mMark;

    private Vector2 mClickPosition;

    private float mLastWaterCheckTime;

    public void AddMark() {
        mMark++;
    }

    public void RemoveMark() {
        if(mMark > 0)
            mMark--;
    }

    public bool IsTouchingUnit(Unit unit) {
        if(!(boxCollider && unit.boxCollider)) return false;

        return boxCollider.bounds.Intersects(unit.boxCollider.bounds);
    }

    public Waypoint GetWaypointAny() {
        if(_waypointGroups == null || _waypointGroups.Length == 0)
            return null;

        var waypointName = _waypointGroups[Random.Range(0, _waypointGroups.Length)].name;
        return GetWaypointRandom(waypointName, false);
    }

    public Waypoint[] GetWaypoints(string waypointName) {
        if(mWorldWaypoints == null || !mWorldWaypoints.ContainsKey(waypointName))
            return null;

        return mWorldWaypoints[waypointName].waypoints;
    }

    public Waypoint GetWaypointRandom(string waypointName, bool checkMarked) {
        if(mWorldWaypoints == null)
            return null;

        WaypointControl ret;
        if(mWorldWaypoints.TryGetValue(waypointName, out ret))
            return ret.GetRandomWaypoint(checkMarked);

        return null;
    }

    public Waypoint GetWaypointUnmarked(string waypointName) {
        if(mWorldWaypoints == null)
            return null;

        WaypointControl ret;
        if(mWorldWaypoints.TryGetValue(waypointName, out ret))
            return ret.GetUnmarkedWaypoint();

        return null;
    }

    public Waypoint GetWaypointUnmarkedClosest(string waypointName, float x) {
        if(mWorldWaypoints == null)
            return null;

        WaypointControl ret;
        if(mWorldWaypoints.TryGetValue(waypointName, out ret)) {
            Waypoint wpClosest = null;
            float distClosest = 0f;

            for(int i = 0; i < ret.waypoints.Length; i++) {
                var wp = ret.waypoints[i];
                if(!wp.isMarked) {
                    var dist = Mathf.Abs(wp.point.x - x);
                    if(wpClosest == null || dist < distClosest) {
                        wpClosest = wp;
                        distClosest = dist;
                    }
                }
            }

            return wpClosest;
        }

        return null;
    }

    public int GetWaypointMarkCount(string waypointName) {
        if(mWorldWaypoints == null)
            return 0;

        int markCount = 0;

        WaypointControl ret;
        if(mWorldWaypoints.TryGetValue(waypointName, out ret)) {
            for(int i = 0; i < ret.waypoints.Length; i++) {
                var wp = ret.waypoints[i];
                if(wp.isMarked)
                    markCount++;
            }
        }

        return markCount;
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

    public void SetStatusStateAndProgress(StructureStatus status, StructureStatusState state, float progress) {
        var inf = mStatusInfos[(int)status];

        if(inf.state != state || inf.progress != progress) {
            inf.state = state;
            inf.progress = progress;

            mStatusInfos[(int)status] = inf;

            statusUpdateCallback?.Invoke(inf);
        }
    }

    public void ResetAllStatus() {
        for(int i = 0; i < mStatusInfos.Length; i++) {
            var inf = mStatusInfos[i];

            if(inf.state != StructureStatusState.None || inf.progress != 0f) {
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
            case StructureState.Construction:
                poolCtrl.Release();
                break;

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

    /// <summary>
    /// Called after unit reaches its owner structure during RetreatToBase state. (use for hazzard evacuation)
    /// </summary>
    public virtual void UnitRetreatToBaseRequest(Unit unit) { }

    protected virtual void Init() { }

    protected virtual void Despawned() { }

    protected virtual void Spawned() { }

    protected virtual void ClearCurrentState() {
        StopCurrentRout();

        ResetAllStatus();

        if(moveCtrl) moveCtrl.Cancel();

        if(isClicked) {
            isClicked = false;
            GameData.instance.signalStructureClick?.Invoke(this);
        }

        switch(mState) {
            case StructureState.Moving:
                RefreshWaypoints();
                break;

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

                if(mCurHitpoints < hitpointsMax) {
                    if(damagedGO) damagedGO.SetActive(true);

                    if(isReparable)
                        SetStatusState(StructureStatus.Construct, StructureStatusState.Require);
                }
                else {
                    if(damagedGO) damagedGO.SetActive(false);
                }

                if(takeIdle != -1)
                    animator.Play(takeIdle);

                mLastWaterCheckTime = Time.time;
                break;

            case StructureState.Construction:
                if(activeGO) activeGO.SetActive(false);
                if(constructionGO) constructionGO.SetActive(true);

                mRout = StartCoroutine(DoConstruction());
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
                if(repairGO) repairGO.SetActive(true);

                mRout = StartCoroutine(DoRepair());
                break;

            case StructureState.Damage:
                if(damagedGO) damagedGO.SetActive(true);

                if(isReparable)
                    SetStatusState(StructureStatus.Construct, StructureStatusState.Require);

                mRout = StartCoroutine(DoDamage());
                break;

            case StructureState.Destroyed:
                if(damagedGO) damagedGO.SetActive(true);

                //if repairable, simply play animation and show repair status
                if(isReparable) {
                    SetStatusState(StructureStatus.Construct, StructureStatusState.Require);

                    if(takeDestroyed != -1)
                        animator.Play(takeDestroyed);
                }
                else //release after destroyed animation (NOTE: make sure that "takeDestroyed" is not a loop animation)
                    AnimateToRelease(takeDestroyed);
                break;

            case StructureState.Demolish:
                if(damagedGO) damagedGO.SetActive(true);

                mRout = StartCoroutine(DoDemolish());
                break;

            case StructureState.Victory:
                physicsActive = false;

                if(takeVictory != -1)
                    animator.Play(takeVictory);
                break;

            case StructureState.None:
                if(animator)
                    animator.Stop();

                if(activeGO) activeGO.SetActive(false);
                if(constructionGO) constructionGO.SetActive(false);
                if(damagedGO) damagedGO.SetActive(false);
                if(repairGO) repairGO.SetActive(false);

                mCurHitpoints = 0;
                workCount = 0;

                up = Vector2.up;

                mMark = 0;

                physicsActive = false;
                break;
        }

        if(boxCollider)
            boxCollider.enabled = physicsActive;

        SetPlacementBlocker(addPlacementBlocker);
    }

    protected virtual void HitpointsChanged(int previousHitpoints) {
        if(mCurHitpoints > previousHitpoints) { //healed?
                                                //remove damage display
            if(mCurHitpoints == hitpointsMax && damagedGO)
                damagedGO.SetActive(false);
        }
        else if(state != StructureState.Demolish) { //already being demolished
                                                    //damaged completely?
            if(mCurHitpoints == 0) {
                if(!string.IsNullOrEmpty(sfxDestroy))
                    M8.SoundPlaylist.instance.Play(sfxDestroy, false);

                state = StructureState.Destroyed;
            }
            //perform damage
            else if(mCurHitpoints < previousHitpoints) {
                if(!string.IsNullOrEmpty(sfxHit))
                    M8.SoundPlaylist.instance.Play(sfxHit, false);

                state = StructureState.Damage;
            }
        }
    }

    protected virtual void Update() {
        //check if we are on water
        if(data && !data.isWaterImmune && state == StructureState.Active) {
            var time = Time.time;
            var elapsed = time - mLastWaterCheckTime;
            if(elapsed >= GameData.instance.structureWaterCheckDelay) {
                mLastWaterCheckTime = time;

                var bounds = boxCollider.bounds;
                var coll = Physics2D.OverlapBox(bounds.center, bounds.size, 0f, GameData.instance.waterLayerMask);
                if(coll) {
                    hitpointsCurrent--;
                }
            }
        }
    }

    protected void SetPlacementBlocker(bool addPlacementBlocker) {
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

        //generate waypoints access
        mWorldWaypoints = new Dictionary<string, WaypointControl>(_waypointGroups.Length);

        for(int i = 0; i < _waypointGroups.Length; i++) {
            var waypointGrp = _waypointGroups[i];

            var waypointGrpPts = waypointGrp.waypoints;

            var waypoints = new Waypoint[waypointGrpPts.Length];
            System.Array.Copy(waypointGrpPts, waypoints, waypoints.Length);

            mWorldWaypoints.Add(waypointGrp.name, new WaypointControl(waypoints));
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

        if(GameData.instance.signalVictory) GameData.instance.signalVictory.callback += OnVictory;

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
        if(GameData.instance.signalVictory) GameData.instance.signalVictory.callback -= OnVictory;

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
        if(GameData.instance.structureDisableInput)
            return;

        GameData.instance.signalClickCategory?.Invoke(GameData.clickCategoryStructure);

        /*Vector2 ret;

            if(overlayActionAnchor)
                ret = overlayActionAnchor.position;
            else //fail-safe, just use top of collider bounds
                ret = new Vector2(position.x + boxCollider.offset.x, position.y + boxCollider.offset.y + boxCollider.size.y*0.5f);

            return ret;*/
        var boxBounds = boxCollider.bounds;

        mClickPosition.x = boxBounds.center.x;
        mClickPosition.y = Mathf.Clamp(eventData.pointerCurrentRaycast.worldPosition.y, boxBounds.min.y, boxBounds.max.y);

        isClicked = true;
        GameData.instance.signalStructureClick?.Invoke(this);
    }

    void OnVictory() {
        if(state == StructureState.None)
            state = StructureState.Victory;
    }

    IEnumerator DoConstruction() {
        var buildTime = data.buildTime;
        var workTimeLeft = buildTime;

        if(workTimeLeft > 0f) {
            var timeScalePerWork = GameData.instance.structureBuildScalePerWork;

            while(workTimeLeft > 0f) {                
                var _workCount = data.isAutoBuild ? workCount + 1 : workCount;

				//no worker?
				if(_workCount == 0) {
                    SetStatusState(StructureStatus.Construct, StructureStatusState.Require);

                    while(workCount == 0)
						yield return null;

                    SetStatusState(StructureStatus.Construct, StructureStatusState.Progress);
                }

                //progress
                yield return null;

				_workCount = data.isAutoBuild ? workCount + 1 : workCount;

				var buildTimeScale = timeScalePerWork * _workCount;

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

        if(!string.IsNullOrEmpty(sfxConstructComplete))
            M8.SoundPlaylist.instance.Play(sfxConstructComplete, false);

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

        yield return null; //guarantee one frame

        SetStatusState(StructureStatus.Construct, StructureStatusState.Progress);

        while(workCount > 0 && curTime < totalDelay && mCurHitpoints < hitpointsMax) {
            //progress
            yield return null;

            var repairTimeScale = repairScalePerWork * workCount;

            curTime += Time.deltaTime * repairTimeScale;

            var t = Mathf.Clamp01(curTime / totalDelay);

            SetStatusProgress(StructureStatus.Construct, t);

            hitpointsCurrent = curHP + Mathf.FloorToInt(repairCount * t);
        }
                
        mRout = null;

        if(mCurHitpoints == 0)
            state = StructureState.Destroyed;
        else {
            if(!string.IsNullOrEmpty(sfxRepairComplete))
                M8.SoundPlaylist.instance.Play(sfxRepairComplete, false);

            state = StructureState.Active;
        }
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
        if(takeDamage != -1)
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

        if(takeMoving != -1)
            animator.Play(takeMoving);

        moveCtrl.Move();

        while(moveCtrl.isMoving)
            yield return null;

        if(takeMoveFinish != -1)
            yield return animator.PlayWait(takeMoveFinish);

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
        if(!string.IsNullOrEmpty(sfxDemolish))
            M8.SoundPlaylist.instance.Play(sfxDemolish, false);

        if(takeDemolish != -1)
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
