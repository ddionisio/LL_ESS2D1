using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class StructureItemWidget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Display")]
    public Image iconImage;
    public bool iconUseNativeSize;

    public TMP_Text nameLabel;

    public GameObject newHighlightGO;

    public bool newHighlightActive {
        get { return newHighlightGO ? newHighlightGO.activeSelf : false; }
        set { if(newHighlightGO) newHighlightGO.SetActive(value); }
    }

    public StructureData data { get; private set; }

    public event System.Action<StructureItemWidget> clickCallback;
    public event System.Action<StructureItemWidget, bool> hoverCallback;

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

        newHighlightActive = false;
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        hoverCallback?.Invoke(this, true);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        hoverCallback?.Invoke(this, false);
    }
}
