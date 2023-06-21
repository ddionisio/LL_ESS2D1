using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class CriteriaItemWidget : MonoBehaviour {
    [Header("Display")]
    public Image iconImage;
    public bool iconUseNativeSize;

    public TMP_Text rangeLabel;

    public void Setup(AtmosphereAttributeBase atmosphereAttribute, int min, int max) {
        gameObject.name = atmosphereAttribute.name;

        if(iconImage) {
            iconImage.sprite = atmosphereAttribute.icon;

            if(iconUseNativeSize)
                iconImage.SetNativeSize();
        }

        if(rangeLabel)
            rangeLabel.text = atmosphereAttribute.GetValueRangeString(min, max);
    }
}
