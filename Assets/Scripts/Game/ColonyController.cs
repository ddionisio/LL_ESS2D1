using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class ColonyController : GameModeController<ColonyController> {

    [Header("Setup")]
    public HotspotData hotspotData; //correlates to the hotspot we launched from the overworld
    public CriteriaData criteriaData; //used to determine house req. params

    protected override void OnInstanceDeinit() {
        base.OnInstanceDeinit();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();
    }

    protected override IEnumerator Start() {
        yield return base.Start();


    }
}