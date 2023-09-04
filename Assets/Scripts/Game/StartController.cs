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

    public CanvasGroup canvasGrp;

    private bool mIsProceed;
    
    public void Continue() { 
        if(mIsProceed) return;


        StartCoroutine(DoProceed(true));
    }

    public void NewGame() {
        if(mIsProceed) return;

        StartCoroutine(DoProceed(false));
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        if(loadingGO) loadingGO.SetActive(true);
        if(readyAnim) readyAnim.Hide();

        if(canvasGrp) canvasGrp.interactable = false;
    }

    protected override IEnumerator Start() {
        yield return base.Start();
                
        while(!LoLManager.instance.isReady)
            yield return null;

        if(!string.IsNullOrEmpty(music))
            M8.MusicPlaylist.instance.Play(music, true, true);

        yield return new WaitForSeconds(0.3f);

        if(loadingGO) loadingGO.SetActive(false);
                
        var lolMgr = LoLManager.instance;

        if(continueGO) continueGO.SetActive(lolMgr.curProgress > 0);

        if(readyAnim) {
            readyAnim.Show();
            readyAnim.PlayEnter();
        }

        if(canvasGrp) canvasGrp.interactable = true;
    }

    IEnumerator DoProceed(bool isContinue) {
        mIsProceed = true;

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
