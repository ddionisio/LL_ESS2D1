using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class AtmosphereAttributeWidget : MonoBehaviour {
    [Header("Data")]
    [SerializeField]
    AtmosphereAttributeBase _data;

    [Header("Display")]
    public Image iconImage;
    public bool iconUseNativeSize;

    public Image legendRangeImage;
    public Image legendRangeMinImage;
    public Image legendRangeMaxImage;

    public TMP_Text nameLabel;
    public TMP_Text symbolLabel;
    public TMP_Text legendRangeLabel;

    public GameObject selectGO;

    public AtmosphereAttributeBase data {
        get { return _data; }
        set {
            if(_data != value) {
                _data = value;
                RefreshDisplay();
            }
        }
    }

    public bool selectActive { 
        get { return selectGO ? selectGO.activeSelf : false; } 
        set {
            if(selectGO)
                selectGO.SetActive(value);
        }
    }

    void OnEnable() {
        RefreshDisplay();
    }

    private void RefreshDisplay() {
        if(_data) {
            if(_data.icon && iconImage) {
                iconImage.sprite = _data.icon;
                if(iconUseNativeSize)
                    iconImage.SetNativeSize();
            }

            if(_data.legendRange && legendRangeImage)
                legendRangeImage.sprite = _data.legendRange;

            if(nameLabel)
                nameLabel.text = M8.Localize.Get(_data.nameRef);

            if(symbolLabel)
                symbolLabel.text = _data.symbolString;

            if(legendRangeLabel)
                legendRangeLabel.text = _data.legendRangeString;

            if(legendRangeMinImage)
                legendRangeMinImage.color = _data.legendRangeMinColor;

            if(legendRangeMaxImage)
                legendRangeMaxImage.color = _data.legendRangeMaxColor;
        }
    }
}
