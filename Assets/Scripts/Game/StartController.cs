using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class StartController : GameModeController<ColonyController> {
    [Header("Display")]
    public GameObject loadingGO;
    public AnimatorEnterExit readyAnim;
    public GameObject continueGO;

    [Header("Music")]
    [M8.MusicPlaylist]
    public string music;

    public void Continue() {
        StartCoroutine(DoProceed(true));
    }

    public void NewGame() {
        StartCoroutine(DoProceed(false));
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        if(loadingGO) loadingGO.SetActive(true);
        if(readyAnim) readyAnim.Hide();
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        if(!string.IsNullOrEmpty(music))
            M8.MusicPlaylist.instance.Play(music, true, true);

        if(loadingGO) loadingGO.SetActive(false);
                
        var lolMgr = LoLManager.instance;

        if(continueGO) continueGO.SetActive(lolMgr.curProgress > 0);

        if(readyAnim) {
            readyAnim.Show();
            readyAnim.PlayEnter();
        }
    }

    IEnumerator DoProceed(bool isContinue) {
        if(readyAnim) {
            yield return readyAnim.PlayExitWait();
            readyAnim.Hide();
        }

        if(isContinue)
            GameData.instance.ProgressContinue();
        else {
            var lolMgr = LoLManager.instance;
            if(lolMgr.curProgress > 0) {
                GameData.instance.ProgressReset();
            }

            GameData.instance.introScene.Load();
        }
    }
}
