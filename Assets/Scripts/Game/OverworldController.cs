using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class OverworldController : GameModeController<OverworldController> {

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
