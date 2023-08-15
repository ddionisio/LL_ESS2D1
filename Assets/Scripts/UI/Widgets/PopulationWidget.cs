using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class PopulationWidget : MonoBehaviour {
    [Header("Display")]
    public GameObject populationRootGO;
    public TMP_Text populationCountLabel;
    public TMP_Text populationCapacityLabel;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public int takePopulationEnter = -1;
    [M8.Animator.TakeSelector]
    public int takePopulationIncrease = -1;
    [M8.Animator.TakeSelector]
    public int takePopulationDecrease = -1;

    private int mPopulationCount;
    private int mPopulationCapacity;

    void OnDisable() {
        if(ColonyController.isInstantiated) {
            var colonyCtrl = ColonyController.instance;

            if(colonyCtrl.signalInvokePopulationUpdate) colonyCtrl.signalInvokePopulationUpdate.callback -= OnPopulationUpdate;
        }
    }

    void OnEnable() {
        var colonyCtrl = ColonyController.instance;

        if(colonyCtrl.signalInvokePopulationUpdate) colonyCtrl.signalInvokePopulationUpdate.callback += OnPopulationUpdate;

        mPopulationCount = colonyCtrl.population;
        mPopulationCapacity = colonyCtrl.populationCapacity;

        if(mPopulationCapacity > 0) {
            if(populationRootGO) populationRootGO.SetActive(true);

            ApplyPopulationInfo();
        }
        else {
            if(populationRootGO) populationRootGO.SetActive(false);
        }
    }

    void OnPopulationUpdate() {
        var colonyCtrl = ColonyController.instance;

        int prevCount = mPopulationCount, prevCapacity = mPopulationCapacity;

        mPopulationCount = colonyCtrl.population;
        mPopulationCapacity = colonyCtrl.populationCapacity;

        ApplyPopulationInfo();

        if(mPopulationCapacity <= 0) {
            if(populationRootGO) populationRootGO.SetActive(false);
        }
        else {
            if(populationRootGO) populationRootGO.SetActive(true);

            if(prevCapacity == 0) {
                if(takePopulationEnter != -1)
                    animator.Play(takePopulationEnter);
            }
            else if(mPopulationCount > prevCount || mPopulationCapacity > prevCapacity) {
                if(takePopulationIncrease != -1)
                    animator.Play(takePopulationIncrease);
            }
            else if(mPopulationCount < prevCount) {
                if(takePopulationDecrease != -1)
                    animator.Play(takePopulationDecrease);
            }
        }
    }

    void ApplyPopulationInfo() {
        if(populationCountLabel) populationCountLabel.text = mPopulationCount.ToString("D2");
        if(populationCapacityLabel) populationCapacityLabel.text = mPopulationCapacity.ToString("D2");
    }
}
