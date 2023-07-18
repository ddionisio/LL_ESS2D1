using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class StructureGroupWidget : MonoBehaviour {
    [Header("Display")]
    public Image iconImage;
    public bool iconUseNativeSize;

    public TMP_Text nameLabel;
    public TMP_Text counterLabel;

    public Selectable interactWidget; //set interactable to true/false based on counter

    [Header("Items Info")]
    public GameObject itemsRootGO;
    public Transform itemsContainerRoot;

    public int index { get; private set; }

    public bool active {
        get { return gameObject.activeSelf; }
        set { gameObject.SetActive(value); }
    }

    public bool itemsActive { 
        get { return itemsRootGO ? itemsRootGO.activeSelf : false; } 
        set { if(itemsRootGO) itemsRootGO.SetActive(value); }
    }

    public int count {
        get { return mCount; }
        set {
            if(mCount != value) {
                mCount = value;
                RefreshCount();
            }
        }
    }

    public event System.Action<StructureGroupWidget> clickCallback;

    private int mCount;

    public void Setup(int aIndex, StructurePaletteData.GroupInfo info) {
        index = aIndex;

        if(iconImage) {
            iconImage.sprite = info.icon;

            if(iconUseNativeSize)
                iconImage.SetNativeSize();
        }

        gameObject.name = info.nameRef;

        if(nameLabel) nameLabel.text = M8.Localize.Get(info.nameRef);

        itemsActive = false;

        mCount = 0;
        RefreshCount();
    }
        
    public void Click() {
        clickCallback?.Invoke(this);
    }

    private void RefreshCount() {
        if(counterLabel) counterLabel.text = mCount.ToString();

        if(interactWidget) interactWidget.interactable = mCount > 0;
    }
}
