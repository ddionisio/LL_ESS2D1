using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CycleUnitSpawnerBase : CycleControlBase {
    [System.Serializable]
    public struct SpawnInfo {
        public float delay;
        public int count;
    }

    [Header("Spawn Info")]
    public UnitData unitData;
    
    [Header("Cycle Info")]
    [Tooltip("Cycle range for when this spawner is active, will despawn everything after max range (set to -1 to despawn at CycleEnd)")]
    public M8.RangeInt cycleIndexRange;
    public SpawnInfo[] cycleSpawnSequence; //determines maximum spawn count in delayed sequence during cycle

    public int cycleIndex { get; private set; }

    public int spawnSequenceIndex { get; private set; }

    public int spawnCounter { get; private set; }
    public int spawnCounterMax { get; private set; }

    public bool isSpawning { get { return mRout != null; } }

    public M8.CacheList<Unit> spawnedUnits { get; private set; }

    private Coroutine mRout;
        
    private M8.GenericParams mSpawnParms = new M8.GenericParams();

    protected virtual bool CanSpawn() { return true; }

    /// <summary>
    /// Apply any parameters needed for this spawn (mostly just position and target structure)
    /// </summary>
    protected abstract void ApplySpawnParams(M8.GenericParams parms);

    /// <summary>
    /// Force a spawn regardless of counter
    /// </summary>
    public void Spawn() {
        if(!spawnedUnits.IsFull && CanSpawn()) {
            var colonyCtrl = ColonyController.instance;
            var unitCtrl = colonyCtrl.unitController;

            ApplySpawnParams(mSpawnParms);

            var newUnit = unitCtrl.Spawn(unitData, mSpawnParms);

            spawnedUnits.Add(newUnit);

            spawnCounter++;
        }
    }

    protected override void Init() {
        base.Init();

        var gameDat = GameData.instance;

        if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback += OnCycleBegin;
        if(gameDat.signalCycleNext) gameDat.signalCycleNext.callback += OnCycleNext;
        if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback += OnCycleEnd;

        if(gameDat.signalUnitDespawned) gameDat.signalUnitDespawned.callback += OnUnitDespawn;

        var colonyCtrl = ColonyController.instance;

        if(cycleIndexRange.max == -1)
            cycleIndexRange.max = colonyCtrl.cycleController.cycleCount - 1;

        //setup spawn
        var maxCount = 0;
        for(int i = 0; i < cycleSpawnSequence.Length; i++)
            maxCount += cycleSpawnSequence[i].count;

        spawnedUnits = new M8.CacheList<Unit>(maxCount);

        colonyCtrl.unitController.AddUnitData(unitData, maxCount, true);
        unitData.Setup(colonyCtrl);
    }

    protected override void Deinit() {
        if(GameData.isInstantiated) {
            var gameDat = GameData.instance;

            if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback -= OnCycleBegin;
            if(gameDat.signalCycleNext) gameDat.signalCycleNext.callback -= OnCycleNext;
            if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback -= OnCycleEnd;

            if(gameDat.signalUnitDespawned) gameDat.signalUnitDespawned.callback -= OnUnitDespawn;
        }

        base.Deinit();
    }

    protected virtual void OnCycleBegin() {
        cycleIndex = 0;

        if(cycleIndexRange.min == 0)
            StartSpawn();
    }

    protected virtual void OnCycleNext() {
        cycleIndex = ColonyController.instance.cycleController.cycleCurIndex;

        if(!isSpawning) {
            if(cycleIndexRange.min == cycleIndex)
                StartSpawn();
        }
    }

    protected virtual void OnCycleEnd() {
        if(isSpawning)
            EndSpawn();
    }

    IEnumerator DoSpawns() {
        var colonyCtrl = ColonyController.instance;
        var cycleCtrl = ColonyController.instance.cycleController;
        var unitCtrl = colonyCtrl.unitController;

        spawnSequenceIndex = 0;

        spawnCounter = 0;
        spawnCounterMax = 0;
        
        var lastElapsedTime = cycleCtrl.cycleCurElapsed;

        while(cycleIndex <= cycleIndexRange.max) {
            yield return null;

            if(spawnSequenceIndex < cycleSpawnSequence.Length) {
                var spawnSequence = cycleSpawnSequence[spawnSequenceIndex];

                var curTime = cycleCtrl.cycleCurElapsed - lastElapsedTime;
                if(curTime >= spawnSequence.delay) {
                    spawnCounterMax += spawnSequence.count;

                    spawnSequenceIndex++;
                    lastElapsedTime = cycleCtrl.cycleCurElapsed;
                }
            }

            if(spawnCounter < spawnCounterMax && !spawnedUnits.IsFull) {
                if(CanSpawn()) {
                    ApplySpawnParams(mSpawnParms);

                    var newUnit = unitCtrl.Spawn(unitData, mSpawnParms);

                    spawnedUnits.Add(newUnit);

                    spawnCounter++;
                }
            }
        }

        mRout = null;
        EndSpawn();
    }

    void OnUnitDespawn(Unit unit) {
        if(unit.data == unitData)
            spawnedUnits.Remove(unit);
    }

    private void StartSpawn() {
        mRout = StartCoroutine(DoSpawns());
    }

    private void EndSpawn() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }

        //despawn all actives
        for(int i = 0; i < spawnedUnits.Count; i++) {
            var unit = spawnedUnits[i];
            if(unit)
                unit.Despawn();
        }

        spawnedUnits.Clear();
    }
}
