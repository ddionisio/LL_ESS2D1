using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class OverworldSequence02 : OverworldSequenceBase {
    [Header("Atmosphere Toggles")]
    public AtmosphereAttributeBase none;
    public AtmosphereAttributeBase wind;
    public AtmosphereAttributeBase temperature;

    [Header("Dialogs")]
    public ModalDialogFlowIncremental dlgIntro;
    public ModalDialogFlowIncremental dlgWind;
    public ModalDialogFlowIncremental dlgWindTemp;
    public ModalDialogFlowIncremental dlgIntroPost;

    [Header("Signal Invoke")]
    public SignalAtmosphereAttribute signalInvokeAtmosphereToggle;

    public override IEnumerator StartBegin() {

        yield return dlgIntro.Play();
    }

    public override IEnumerator StartFinish() {
        if(OverworldController.instance.overworldWindFX)
            OverworldController.instance.overworldWindFX.Play();

        signalInvokeAtmosphereToggle.Invoke(wind);

        yield return dlgWind.Play();

        signalInvokeAtmosphereToggle.Invoke(temperature);

        yield return dlgWindTemp.Play();

        signalInvokeAtmosphereToggle.Invoke(none);

        yield return dlgIntroPost.Play();
    }
}