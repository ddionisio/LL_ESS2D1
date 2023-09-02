using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class IntroController : GameModeController<ColonyController> {

    [Header("Intro")]
    public ModalDialogFlowIncremental introDialog;
    
    public AnimatorEnterExit shipsAnim;

    public ModalDialogFlowIncremental introCommDialog;
    public ModalDialogFlowIncremental introComm2Dialog;

    public M8.Animator.Animate shipsAnimator;
    [M8.Animator.TakeSelector(animatorField = "shipsAnimator")]
    public string shipsCommunicate;

    public ModalDialogFlowIncremental introComm3Dialog;
    public ModalDialogFlowIncremental introComm4Dialog;

    [Header("Enter Earth")]
    public AnimatorEnterExit earthAnim;

    [Header("Audio")]
    [M8.MusicPlaylist]
    public string music;


    protected override IEnumerator Start() {
        yield return base.Start();

        if(!string.IsNullOrEmpty(music) && M8.MusicPlaylist.instance.lastPlayName != music)
            M8.MusicPlaylist.instance.Play(music, true, true);

        yield return new WaitForSeconds(1f);

        yield return introDialog.Play();

        //introduce frogs
        if(shipsAnim) {
            shipsAnim.Show();
            yield return shipsAnim.PlayEnterWait();
        }

        yield return introCommDialog.Play();

        yield return introComm2Dialog.Play();

        if(shipsAnimator && !string.IsNullOrEmpty(shipsCommunicate))
            yield return shipsAnimator.PlayWait(shipsCommunicate);

        yield return introComm3Dialog.Play();

        if(shipsAnimator && !string.IsNullOrEmpty(shipsCommunicate))
            yield return shipsAnimator.PlayWait(shipsCommunicate);

        yield return introComm4Dialog.Play();

        if(shipsAnim) {
            yield return shipsAnim.PlayExitWait();
            shipsAnim.Hide();
        }

        if(earthAnim) {
            earthAnim.Show();
            yield return earthAnim.PlayEnterWait();
        }

        GameData.instance.ProgressNextToOverworld();
    }
}
