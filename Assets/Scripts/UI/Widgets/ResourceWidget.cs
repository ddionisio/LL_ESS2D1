using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class ResourceWidget : MonoBehaviour {
    public StructureResourceData.ResourceType resourceType;

    [Header("Config")]
    public Color[] percentColors; //0 - 100
    public float percentLowThreshold = 0.1f;
    public float hideOnFullDelay = 5f; //hide at 100% delay

    [Header("Display")]
    public GameObject rootDisplayGO;
    
    public M8.UI.Graphics.ColorGroup percentColorGroup;
    public Slider percentSlider;
    public TMP_Text percentLabel;    
    public GameObject percentLowActiveGO;

    public TMP_Text valueLabel;

    private bool mIsFull;
    private float mLastTimeOnFull;

    void OnDisable() {
        if(ColonyController.isInstantiated) {
            var colonyCtrl = ColonyController.instance;

            if(colonyCtrl.signalInvokeResourceUpdate)
                colonyCtrl.signalInvokeResourceUpdate.callback -= OnResourceUpdate;
        }
    }

    void OnEnable() {
        var colonyCtrl = ColonyController.instance;

        if(colonyCtrl.signalInvokeResourceUpdate)
            colonyCtrl.signalInvokeResourceUpdate.callback += OnResourceUpdate;

        mIsFull = false;
        OnResourceUpdate();
    }

    void Update() {
        if(mIsFull && rootDisplayGO.activeSelf) {
            var colonyCtrl = ColonyController.instance;

            var amt = colonyCtrl.GetResourceAmount(resourceType);
            var capacity = colonyCtrl.GetResourceCapacity(resourceType);
            if(amt >= capacity) {
                if(Time.time - mLastTimeOnFull >= hideOnFullDelay) {
                    rootDisplayGO.SetActive(false);
                }
            }
        }
    }

    void OnResourceUpdate() {
        var colonyCtrl = ColonyController.instance;

        var capacity = colonyCtrl.GetResourceCapacity(resourceType);

        if(capacity > 0f) {
            var amt = colonyCtrl.GetResourceAmount(resourceType);

            var t = Mathf.Clamp01(amt / capacity);

            if(percentColorGroup && percentColors.Length > 0) {
                percentColorGroup.ApplyColor(M8.ColorUtil.Lerp(percentColors, t));
            }

            if(percentSlider)
                percentSlider.normalizedValue = t;

            if(percentLabel)
                percentLabel.text = Mathf.RoundToInt(t * 100f).ToString() + "%";

            if(percentLowActiveGO)
                percentLowActiveGO.SetActive(t <= 0.1f);

            if(valueLabel)
                valueLabel.text = Mathf.RoundToInt(amt).ToString();

            var rootShow = true;

            var isFull = amt >= capacity;
            if(mIsFull != isFull) {
                mIsFull = isFull;
                if(mIsFull)
                    mLastTimeOnFull = Time.time;
            }
            else if(mIsFull)
                rootShow = Time.time - mLastTimeOnFull < hideOnFullDelay;

            rootDisplayGO.SetActive(rootShow);
        }
        else {
            rootDisplayGO.SetActive(false);
        }
    }
}
