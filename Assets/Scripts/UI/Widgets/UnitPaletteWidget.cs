using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class UnitPaletteWidget : MonoBehaviour {
    [Header("Display Info")]
    public GameObject activeGO; //show only when capacity is > 0

    [Header("Unit Info")]
    public UnitItemWidget unitWidgetTemplate; //not prefab
    public Transform unitWidgetRoot;
        
    [Header("Counter Info")]
    public TMP_Text counterLabel;
    public string counterFormat = "{0}|{1}";

    public Slider counterSlider;
    public GameObject counterPipTemplate; //not prefab
    public Transform counterPipRoot;

    private UnitItemWidget[] mUnitWidgets;

    private GameObject[] mCounterPipGOs;
    private int mCounterPipCount;

    public void Setup(UnitPaletteData unitPalette) {
        //setup units
        mUnitWidgets = new UnitItemWidget[unitPalette.units.Length];

        for(int i = 0; i < unitPalette.units.Length; i++) {
            var unitInfo = unitPalette.units[i];

            var newUnitWidget = Instantiate(unitWidgetTemplate, unitWidgetRoot);

            newUnitWidget.Setup(unitInfo.data);

            newUnitWidget.clickCallback += OnClickUnit;

            newUnitWidget.active = !unitInfo.isHidden;

            mUnitWidgets[i] = newUnitWidget;
        }

        unitWidgetTemplate.gameObject.SetActive(false);

        //setup counter
        mCounterPipCount = unitPalette.capacityStart > 0 ? unitPalette.capacityStart - 1 : 0;

        if(unitPalette.capacity > 0) {
            mCounterPipGOs = new GameObject[unitPalette.capacity - 1];

            for(int i = 0; i < mCounterPipGOs.Length; i++) {
                var newCounterPipGO = Instantiate(counterPipTemplate, counterPipRoot);

                newCounterPipGO.SetActive(i < mCounterPipCount);

                mCounterPipGOs[i] = newCounterPipGO;
            }
        }
        else
            mCounterPipGOs = new GameObject[0];

        if(counterLabel)
            counterSlider.onValueChanged.AddListener(OnCounterValueChanged);

        counterSlider.maxValue = unitPalette.capacityStart;
        counterSlider.value = 0;

        counterPipTemplate.SetActive(false);

        activeGO.SetActive(unitPalette.capacityStart > 0);
    }

    public void RefreshInfo() {
        var unitPaletteCtrl = ColonyController.instance.unitPaletteController;

        var capacity = unitPaletteCtrl.capacity;

        activeGO.SetActive(capacity > 0);

        if(capacity == 0)
            return;

        //refresh unit items
        for(int i = 0; i < mUnitWidgets.Length; i++) {
            var unitWidget = mUnitWidgets[i];

            var isHidden = unitPaletteCtrl.IsHidden(i);
            if(isHidden)
                unitWidget.active = false;
            else {
                unitWidget.active = true;

                unitWidget.interactable = unitPaletteCtrl.IsBusy(i);

                unitWidget.counter = unitPaletteCtrl.GetActiveCountByType(unitWidget.unitData);
            }
        }

        //refresh counter
        var curCapacity = Mathf.FloorToInt(counterSlider.maxValue);
        if(curCapacity != capacity) {
            mCounterPipCount = curCapacity > 0 ? curCapacity - 1 : 0;
            for(int i = 0; i < mCounterPipGOs.Length; i++)
                mCounterPipGOs[i].SetActive(i < mCounterPipCount);

            counterSlider.maxValue = capacity;

            //TODO: play flashy fx
        }

        counterSlider.value = unitPaletteCtrl.activeCount;
    }

    void OnClickUnit(UnitItemWidget unitWidget) {
        GameData.instance.signalClickCategory?.Invoke(GameData.clickCategoryUnitPalette);

        var unitPaletteCtrl = ColonyController.instance.unitPaletteController;

        var unitData = unitWidget.unitData;

        if(unitPaletteCtrl.IsBusy(unitData)) //just in case
            return;

        switch(unitWidget.action) {
            case UnitItemWidget.Action.Increase:
                if(!unitPaletteCtrl.isFull) {
                    unitPaletteCtrl.Spawn(unitData);
                }
                else {
                    //TODO: error display
                    //TODO: error sfx
                }
                break;

            case UnitItemWidget.Action.Decrease:
                if(unitWidget.counter > 0) {
                    unitPaletteCtrl.Despawn(unitData);
                }
                break;
        }
    }

    void OnCounterValueChanged(float val) {
        int count = Mathf.FloorToInt(val);
        int maxCount = Mathf.FloorToInt(counterSlider.maxValue);

        counterLabel.text = string.Format(counterFormat, count, maxCount);
    }
}
