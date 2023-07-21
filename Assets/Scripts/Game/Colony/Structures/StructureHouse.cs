using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureHouse : Structure {

    [Header("House Signal Invoke")]
    public M8.Signal signalInvokePopulationChanged;

    [Header("House Signal Listen")]
    public SignalUnit signalListenDeath;
    public SignalUnit signalListenUnitDespawned;

    private UnitData mCitizenUnitData;
    private M8.CacheList<Unit> mCitizensActive;

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case StructureState.None:
                break;
        }
    }

    protected override void Init() {
        
    }

    protected override void Despawned() {
        
    }

    protected override void Spawned() {
        var houseData = data as StructureHouseData;
        if(houseData) {
            //initialize citizens
            mCitizenUnitData = houseData.citizenData;


        }
    }

    IEnumerator DoActive() {
        //spawn citizens, check requirements, increase population

        //if damaged, need to repair first before anything

        //turn on the appropriate statuses (if damaged, just repair status)

        yield return null;

        mRout = null;
    }

    void OnSignalUnitDeath(Unit unit) {

    }

    void OnSignalUnitDespawned(Unit unit) {

    }
}
