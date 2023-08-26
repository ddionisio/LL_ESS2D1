using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureColonyShip : Structure {
    [SerializeField]
    StructureColonyShipData _data;

    [Header("Landing Info")]
    public GameObject landingActiveGO; //disable upon touchdown
    public float landingDelay = 1.0f;
    public DG.Tweening.Ease landingEase = DG.Tweening.Ease.OutSine;

    [Header("Colony Ship Animation")]
    [M8.Animator.TakeSelector]
    public int takeLanding = -1;
    [M8.Animator.TakeSelector]
    public int takeBump = -1;

    public StructureColonyShipData colonyShipData { get { return _data; } }

    public override StructureAction actionFlags {
        get {
            return StructureAction.None;
        }
    }

    private M8.CacheList<Unit> mUnitDyingList;
    private M8.CacheList<UnitMedic> mMedicActives;

    private M8.CacheList<UnitData> mUnitHazzardRetreats = new M8.CacheList<UnitData>(16);

    private bool mIsInit;

    public void Bump() {
        if(takeBump != -1)
            animator.Play(takeBump);
    }

    public void Init(ColonyController colonyController) {
        if(mIsInit) return;

        _data.Setup(colonyController, 1);

        //special case since we are not spawned from pool
        var init = this as M8.IPoolInit;
        if(init != null)
            init.OnInit();

        mIsInit = true;
    }

    public void Spawn() {
        var spawn = this as M8.IPoolSpawn;
        if(spawn != null) {
            var parms = new M8.GenericParams();
            parms[StructureSpawnParams.spawnPoint] = position;
            parms[StructureSpawnParams.spawnNormal] = up;
            parms[StructureSpawnParams.data] = _data;

            spawn.OnSpawned(parms);
        }

        var spawnComplete = this as M8.IPoolSpawnComplete;
        if(spawnComplete != null)
            spawnComplete.OnSpawnComplete();
    }

    public void AddUnitHazzardRetreat(UnitData unitData) {
        if(mUnitHazzardRetreats.IsFull)
            return;

        var unitIndex = ColonyController.instance.unitPaletteController.GetUnitIndex(unitData);
        if(unitIndex != -1)
            mUnitHazzardRetreats.Add(unitData);
    }

    protected override void ApplyCurrentState() {
        switch(state) {
            case StructureState.Spawning:
                if(activeGO) activeGO.SetActive(true);

                mRout = StartCoroutine(DoSpawn());
                break;

            default:
                base.ApplyCurrentState();
                break;
        }
    }

    protected override void Init() {
        mUnitDyingList = new M8.CacheList<Unit>(64);
        mMedicActives = new M8.CacheList<UnitMedic>(_data.medicCapacity);

        //setup signals
        var gameDat = GameData.instance;

        if(gameDat.signalUnitDying) GameData.instance.signalUnitDying.callback += OnUnitDying;
        if(gameDat.signalUnitDespawned) gameDat.signalUnitDespawned.callback += OnUnitDespawned;

        if(landingActiveGO) landingActiveGO.SetActive(false);
    }

    protected override void Spawned() {
        
    }
        
    void OnDestroy() {
        if(GameData.isInstantiated) {
            var gameDat = GameData.instance;

            if(gameDat.signalUnitDying) GameData.instance.signalUnitDying.callback -= OnUnitDying;
            if(gameDat.signalUnitDespawned) gameDat.signalUnitDespawned.callback -= OnUnitDespawned;
        }
    }

    protected override void Update() {
        base.Update();

        //check for dying units
        if(mUnitDyingList.Count > 0) {
            for(int i = 0; i < mUnitDyingList.Count; i++) {
                var unitDying = mUnitDyingList[i];
                if(unitDying.state != UnitState.Dying) { //this unit is no longer dying
                    mUnitDyingList.RemoveAt(i);
                    continue;
                }

                UnitMedic medicAssigned = null;

                for(int j = 0; j < mMedicActives.Count; j++) {
                    var medic = mMedicActives[j];

                    if(medic.targetUnit == unitDying) { //already targeted by a medic?
                        medicAssigned = medic;
                        break;
                    }
                    else if(!medic.targetUnit && (medic.state == UnitState.Idle || medic.state == UnitState.Move)) { //reassign medic to this unit
                        medic.targetUnit = unitDying;
                        medicAssigned = medic;
                        break;
                    }
                }

                if(!medicAssigned) {
                    //can spawn medic?
                    if(!mMedicActives.IsFull && !ColonyController.instance.cycleController.isHazzard) {
                        var wp = GetWaypointRandom(GameData.structureWaypointSpawn, false);
                        var medic = (UnitMedic)ColonyController.instance.unitController.Spawn(_data.medicData, this, wp != null ? wp.groundPoint.position : position);

                        medic.targetUnit = unitDying;

                        mMedicActives.Add(medic);
                    }
                    else //wait for available medic
                        break;
                }
            }
        }

        if(mUnitHazzardRetreats.Count > 0 && !ColonyController.instance.cycleController.isHazzard) {
            var unitDat = mUnitHazzardRetreats.RemoveLast();
            ColonyController.instance.unitPaletteController.SpawnQueue(unitDat);
        }
    }

    void OnUnitDying(Unit unit) {
        if(unit.CompareTag(GameData.instance.unitAllyTag))
            mUnitDyingList.Add(unit);
    }

    void OnUnitDespawned(Unit unit) {
        //check if it's one of those units we have in dying queue
        mUnitDyingList.Remove(unit);

        //remove from active medics if it's ours
        if(unit.data == _data.medicData)
            mMedicActives.Remove((UnitMedic)unit);
    }

    IEnumerator DoSpawn() {
        var easeFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(landingEase);

        var screenExt = ColonyController.instance.mainCamera2D.screenExtent;

        var startPos = new Vector2(position.x, screenExt.yMax);
        var endPos = position;

        position = startPos;

        if(takeLanding != -1)
            animator.ResetTake(takeLanding);

        if(landingActiveGO) landingActiveGO.SetActive(true);

        var curTime = 0f;
        while(curTime < landingDelay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = easeFunc(curTime, landingDelay, 0f, 0f);

            position = Vector2.Lerp(startPos, endPos, t);
        }

        if(landingActiveGO) landingActiveGO.SetActive(false);

        if(takeLanding != -1)
            yield return animator.PlayWait(takeLanding);

        mRout = null;

        state = StructureState.Active;
    }
}
