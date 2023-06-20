using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class SeasonWidget : MonoBehaviour {
    [Header("Data")]
    [SerializeField]
    SeasonData _data;

    [Header("Display")]
    public Image iconImage;
    public bool iconUseNativeSize;

    public TMP_Text nameLabel;

    public SeasonData data {
        get { return _data; }
        set {
            if(_data != value) {
                _data = value;
                RefreshDisplay();
            }
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

            if(nameLabel)
                nameLabel.text = M8.Localize.Get(_data.nameRef);
        }
    }
}
