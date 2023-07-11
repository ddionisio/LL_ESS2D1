using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class StructureItemWidget : MonoBehaviour {
    [Header("Display")]
    public Image iconImage;
    public bool iconUseNativeSize;

    public TMP_Text nameLabel;

    public StructureData data { get; private set; }

    public event System.Action<StructureItemWidget> clickCallback;

    public void Click() {
        clickCallback?.Invoke(this);
    }

    public void Setup(StructureData aData) {
        data = aData;

        if(iconImage) {
            iconImage.sprite = data.icon;

            if(iconUseNativeSize)
                iconImage.SetNativeSize();
        }

        if(nameLabel) nameLabel.text = M8.Localize.Get(data.nameRef);
    }
}
