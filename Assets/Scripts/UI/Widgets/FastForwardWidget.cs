using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FastForwardWidget : MonoBehaviour {
    [Header("Data")]
    public GameObject normalGO;
    public GameObject fastForwardGO;
    public GameObject pauseGO;

    [Header("Display")]
    public Selectable selectable;

    public void FastForwardToggle() {
        if(ColonyController.isInstantiated) {
            var colonyCtrl = ColonyController.instance;

            switch(colonyCtrl.timeState) {
                case ColonyController.TimeState.Normal:
                    colonyCtrl.FastForward();
                    break;
                case ColonyController.TimeState.FastForward:
                    colonyCtrl.Resume();
                    break;
            }
        }
    }

    void OnDisable() {
        if(ColonyController.isInstantiated) {
            var colonyCtrl = ColonyController.instance;
            colonyCtrl.fastforwardChangedCallback -= OnFastForwardChanged;
        }
    }

    void OnEnable() {        
        if(ColonyController.isInstantiated) {
            var colonyCtrl = ColonyController.instance;

            colonyCtrl.fastforwardChangedCallback += OnFastForwardChanged;
            OnFastForwardChanged(colonyCtrl.timeState);
        }
    }

    void OnFastForwardChanged(ColonyController.TimeState state) {
        switch(state) {
            case ColonyController.TimeState.None:
                if(selectable) selectable.interactable = false;

                if(normalGO) normalGO.SetActive(true);
                if(fastForwardGO) fastForwardGO.SetActive(false);
                if(pauseGO) pauseGO.SetActive(false);
                break;

            case ColonyController.TimeState.Normal:
                if(selectable) selectable.interactable = true;

                if(normalGO) normalGO.SetActive(true);
                if(fastForwardGO) fastForwardGO.SetActive(false);
                if(pauseGO) pauseGO.SetActive(false);
                break;

            case ColonyController.TimeState.FastForward:
                if(selectable) selectable.interactable = true;

                if(normalGO) normalGO.SetActive(false);
                if(fastForwardGO) fastForwardGO.SetActive(true);
                if(pauseGO) pauseGO.SetActive(false);
                break;

            case ColonyController.TimeState.Pause:
            case ColonyController.TimeState.CyclePause:
                if(selectable) selectable.interactable = false;

                if(normalGO) normalGO.SetActive(false);
                if(fastForwardGO) fastForwardGO.SetActive(false);
                if(pauseGO) pauseGO.SetActive(true);
                break;
        }
    }
}
