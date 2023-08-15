using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FastForwardWidget : MonoBehaviour {
    [Header("Data")]
    public GameObject normalGO;
    public GameObject fastForwardGO;

    [Header("Display")]
    public Selectable selectable;

    public void FastForwardToggle() {
        if(ColonyController.isInstantiated) {
            var colonyCtrl = ColonyController.instance;

            switch(colonyCtrl.fastforwardState) {
                case ColonyController.FastForwardState.Normal:
                    colonyCtrl.fastforwardState = ColonyController.FastForwardState.FastForward;
                    break;
                case ColonyController.FastForwardState.FastForward:
                    colonyCtrl.fastforwardState = ColonyController.FastForwardState.Normal;
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
            OnFastForwardChanged(colonyCtrl.fastforwardState);
        }
    }

    void OnFastForwardChanged(ColonyController.FastForwardState state) {
        switch(state) {
            case ColonyController.FastForwardState.None:
                if(selectable) selectable.interactable = false;

                if(normalGO) normalGO.SetActive(true);
                if(fastForwardGO) fastForwardGO.SetActive(false);
                break;

            case ColonyController.FastForwardState.Normal:
                if(selectable) selectable.interactable = true;

                if(normalGO) normalGO.SetActive(true);
                if(fastForwardGO) fastForwardGO.SetActive(false);
                break;

            case ColonyController.FastForwardState.FastForward:
                if(selectable) selectable.interactable = true;

                if(normalGO) normalGO.SetActive(false);
                if(fastForwardGO) fastForwardGO.SetActive(true);
                break;
        }
    }
}
