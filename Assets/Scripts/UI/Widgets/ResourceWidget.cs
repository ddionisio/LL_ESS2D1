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

    [Header("Display")]
    public GameObject rootDisplayGO;
    
    public M8.UI.Graphics.ColorGroup percentColorGroup;
    public Slider percentSlider;
    public TMP_Text percentLabel;    
    public GameObject percentLowActiveGO;

    public TMP_Text valueLabel;

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

        OnResourceUpdate();
    }

    void OnResourceUpdate() {
        var colonyCtrl = ColonyController.instance;
                
        var capacity = colonyCtrl.GetResourceCapacity(resourceType);

        if(capacity > 0f) {
            rootDisplayGO.SetActive(true);

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
        }
        else {
            rootDisplayGO.SetActive(false);
        }
    }
}
