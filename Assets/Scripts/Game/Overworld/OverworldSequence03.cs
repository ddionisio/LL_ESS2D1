using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class OverworldSequence03 : OverworldSequenceBase {
    [Header("Dialogs")]
    public ModalDialogFlowIncremental dlgIntro;
    public ModalDialogFlowIncremental dlgInvestigate;

    private bool mIsInvestigateShown;

    public override IEnumerator StartBegin() {

        yield return dlgIntro.Play();
    }

    public override IEnumerator InvestigationEnterEnd() {
        if(!mIsInvestigateShown) {
            mIsInvestigateShown = true;
            yield return dlgInvestigate.Play();
        }
    }
}
