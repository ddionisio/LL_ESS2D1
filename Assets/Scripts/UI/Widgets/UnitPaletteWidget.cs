using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class UnitPaletteWidget : MonoBehaviour {
    [Header("Display Info")]
    public GameObject activeGO; //show only when capacity is > 0
    public GameObject isFullGO; //show when highlighting "increase" if capacity is full

    [Header("Unit Info")]
    public UnitItemWidget unitWidgetTemplate; //not prefab
    public Transform unitWidgetRoot;
        
    [Header("Counter Info")]
    public TMP_Text counterLabel;
    public string counterFormat = "{0}|{1}";

    public UnitPaletteCounterPipWidget counterPipTemplate; //not prefab
    public Transform counterPipRoot;

    private UnitItemWidget[] mUnitWidgets;
    private int mUnitWidgetCount;

    private UnitPaletteCounterPipWidget[] mCounterPips;

    private UnitItemWidget.Action mActionHighlight;

    private int mCounterPip;
    private int mCounterPipCount;

    private bool mIsQueueActive;

    public void Setup(UnitPaletteData unitPalette) {
        //setup units
        mUnitWidgetCount = unitPalette.units.Length;

        if(mUnitWidgets == null) {
            mUnitWidgets = new UnitItemWidget[mUnitWidgetCount];
            for(int i = 0; i < mUnitWidgetCount; i++) {
                var newUnitWidget = Instantiate(unitWidgetTemplate, unitWidgetRoot);

                newUnitWidget.clickCallback += OnClickUnit;
                newUnitWidget.actionChangeCallback += OnActionChanged;

                mUnitWidgets[i] = newUnitWidget;
            }
        }
        else if(mUnitWidgetCount > mUnitWidgets.Length) {
            var lastSize = mUnitWidgets.Length;
            System.Array.Resize(ref mUnitWidgets, mUnitWidgetCount);

            for(int i = lastSize; i < mUnitWidgetCount; i++) {
                var newUnitWidget = Instantiate(unitWidgetTemplate, unitWidgetRoot);

                newUnitWidget.clickCallback += OnClickUnit;
                newUnitWidget.actionChangeCallback += OnActionChanged;

                mUnitWidgets[i] = newUnitWidget;
            }
        }

        for(int i = 0; i < mUnitWidgetCount; i++) {
            var unitInfo = unitPalette.units[i];

            var unitWidget = mUnitWidgets[i];

            unitWidget.Setup(unitInfo.data);

            unitWidget.active = !unitInfo.isHidden;

            mUnitWidgets[i] = unitWidget;
        }

        for(int i = mUnitWidgetCount; i < mUnitWidgets.Length; i++)
            mUnitWidgets[i].active = false;
                
        //setup counter
        mCounterPip = 0;
        mCounterPipCount = unitPalette.capacityStart > 0 ? unitPalette.capacityStart : 0;

        if(unitPalette.capacity > 0) {
            mCounterPips = new UnitPaletteCounterPipWidget[unitPalette.capacity];

            for(int i = 0; i < mCounterPips.Length; i++) {
                var newCounterPip = Instantiate(counterPipTemplate, counterPipRoot);

                if(i < mCounterPipCount) {
                    newCounterPip.active = true;
                    newCounterPip.baseActive = false;
                    newCounterPip.lineActive = i < mCounterPipCount - 1;
                }
                else
                    newCounterPip.active = false;

                mCounterPips[i] = newCounterPip;
            }
        }
        else
            mCounterPips = new UnitPaletteCounterPipWidget[0];

        counterPipTemplate.active = false;

        mActionHighlight = UnitItemWidget.Action.None;

        mIsQueueActive = false;

        if(activeGO) activeGO.SetActive(unitPalette.capacityStart > 0);

        if(isFullGO) isFullGO.SetActive(false);
    }

    public void RefreshInfo() {
        var unitPaletteCtrl = ColonyController.instance.unitPaletteController;

        var capacity = unitPaletteCtrl.capacity;

        if(activeGO) activeGO.SetActive(capacity > 0);

        if(capacity == 0)
            return;

        //refresh unit items
        int queueTotalCount = 0;

        for(int i = 0; i < mUnitWidgetCount; i++) {
            var unitWidget = mUnitWidgets[i];

            var isHidden = unitPaletteCtrl.IsHidden(i);
            if(isHidden)
                unitWidget.active = false;
            else {
                unitWidget.active = true;
                unitWidget.interactable = true;

                unitWidget.cooldownScale = 0f;

                var activeCount = unitPaletteCtrl.GetActiveCountByType(unitWidget.unitData);
                var queueCount = unitPaletteCtrl.GetSpawnQueueCountByType(unitWidget.unitData);

                queueTotalCount += queueCount;

                unitWidget.SetCounter(activeCount, queueCount);
            }
        }

        mIsQueueActive = queueTotalCount > 0;

        //refresh counter
        var activeTotalCount = unitPaletteCtrl.activeCount;
        mCounterPip = activeTotalCount + queueTotalCount;

        var isCountChanged = mCounterPipCount != capacity;

        mCounterPipCount = capacity;
        for(int i = 0; i < mCounterPips.Length; i++) {
            var counterPip = mCounterPips[i];

            if(i < mCounterPipCount) {
                counterPip.active = true;

                if(i < mCounterPip) {
                    counterPip.baseActive = true;
                    counterPip.isQueue = i >= activeTotalCount;
                    counterPip.action = UnitItemWidget.Action.None;
                }
                else
                    counterPip.baseActive = false;

                counterPip.lineActive = i < mCounterPipCount - 1;
            }
            else
                counterPip.active = false;
        }

        //TODO: play flashy fx
        if(isCountChanged) {

        }

        //update pip highlight
        if(mActionHighlight != UnitItemWidget.Action.None)
            ApplyActionHighlight(mActionHighlight);
    }

    void Awake() {
        unitWidgetTemplate.active = false;
    }

    void Update() {
        if(mIsQueueActive) {
            var unitPaletteCtrl = ColonyController.instance.unitPaletteController;

            int queueActiveCount = 0;
            for(int i = 0; i < unitPaletteCtrl.unitPalette.units.Length; i++) {
                var unitWidget = mUnitWidgets[i];

                var queueCount = unitPaletteCtrl.GetSpawnQueueCount(i);
                if(queueCount > 0) {
                    unitWidget.cooldownScale = 1.0f - unitPaletteCtrl.GetSpawnQueueTimeScale(i);

                    queueActiveCount++;
                }
                else
                    unitWidget.cooldownScale = 0f;
            }

            mIsQueueActive = queueActiveCount > 0;
        }
    }

    void OnClickUnit(UnitItemWidget unitWidget) {
        var unitPaletteCtrl = ColonyController.instance.unitPaletteController;

        var unitData = unitWidget.unitData;

        switch(unitWidget.action) {
            case UnitItemWidget.Action.Increase:
                if(!unitPaletteCtrl.isFull) {
                    unitPaletteCtrl.SpawnQueue(unitData);
                }
                else {
                    //TODO: error display
                    //TODO: error sfx
                }
                break;

            case UnitItemWidget.Action.Decrease:
                if(unitWidget.counter + unitWidget.counterQueue > 0) {
                    unitPaletteCtrl.Despawn(unitData);
                }
                break;
        }
    }

    void OnActionChanged(UnitItemWidget unitWidget) {
        if(mActionHighlight != unitWidget.action) {
            ApplyActionHighlight(UnitItemWidget.Action.None); //clear previous
            ApplyActionHighlight(unitWidget.action);
        }
    }

    private void ApplyActionHighlight(UnitItemWidget.Action action) {
        switch(action) {
            case UnitItemWidget.Action.Decrease:
                if(mCounterPip > 0)
                    mCounterPips[mCounterPip - 1].action = action;

                if(isFullGO) isFullGO.SetActive(false);
                break;

            case UnitItemWidget.Action.Increase:
                var canIncrease = mCounterPip < mCounterPipCount;
                if(canIncrease) {
                    mCounterPips[mCounterPip].baseActive = true;
                    mCounterPips[mCounterPip].action = action;
                }

                if(isFullGO) isFullGO.SetActive(!canIncrease);
                break;

            case UnitItemWidget.Action.None:
                switch(mActionHighlight) {
                    case UnitItemWidget.Action.Decrease:
                        if(mCounterPip > 0)
                            mCounterPips[mCounterPip - 1].action = UnitItemWidget.Action.None;
                        break;
                    case UnitItemWidget.Action.Increase:
                        if(mCounterPip < mCounterPipCount)
                            mCounterPips[mCounterPip].baseActive = false;

                        if(isFullGO) isFullGO.SetActive(false);
                        break;
                }
                break;
        }

        mActionHighlight = action;
    }
}
