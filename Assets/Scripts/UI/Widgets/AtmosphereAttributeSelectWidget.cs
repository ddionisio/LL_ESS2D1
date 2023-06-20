using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AtmosphereAttributeSelectWidget : MonoBehaviour {
    //NOTE: ensure layout is set for container to allow dynamic enable/disable of attributes

    public struct ItemInfo {
        public Transform root;
        public AtmosphereAttributeWidget atmosphereWidget;
        public Button button;

        public ItemInfo(Transform t) {
            root = t;
            atmosphereWidget = t.GetComponent<AtmosphereAttributeWidget>();
            button = t.GetComponent<Button>();
        }

        public void ApplyActive(AtmosphereAttributeBase[] activeAtmosphereAttributes) {
            if(atmosphereWidget) {
                var attrDat = atmosphereWidget.data;

                bool isMatch = false;

                for(int i = 0; i < activeAtmosphereAttributes.Length; i++) {
                    if(activeAtmosphereAttributes[i] == attrDat) {
                        isMatch = true;
                        break;
                    }
                }

                root.gameObject.SetActive(isMatch);
            }
        }
    }

    public Transform itemsRoot; //put items here where it has the component: AtmosphereAttributeWidget, Button
    public Transform selectHighlightRoot;

    [Header("Signal Invoke")]
    public SignalAtmosphereAttribute signalInvokeAtmosphereClick;

    private ItemInfo[] mItems;
    private int mCurItemInd;

    private bool mIsInit;

    public void Setup(int selectIndex, AtmosphereAttributeBase[] activeAtmosphereAttributes) {
        if(!mIsInit) Init();

        //activate items that match, hide otherwise
        for(int i = 0; i < mItems.Length; i++)
            mItems[i].ApplyActive(activeAtmosphereAttributes);

        SetSelectItem(selectIndex);
    }

    void Awake() {
        if(!mIsInit) Init();
    }

    void OnItemClick(int index) {
        if(mCurItemInd == index)
            return;
                
        SetSelectItem(index);

        var itm = mItems[index];

        signalInvokeAtmosphereClick?.Invoke(itm.atmosphereWidget ? itm.atmosphereWidget.data : null);
    }

    private void SetSelectItem(int index) {
        mCurItemInd = index;

        if(mCurItemInd >= 0 && mCurItemInd < mItems.Length) {
            var itm = mItems[index];

            if(selectHighlightRoot) {
                selectHighlightRoot.gameObject.SetActive(true);

                selectHighlightRoot.position = itm.root.position;
            }
        }
        else {
            if(selectHighlightRoot)
                selectHighlightRoot.gameObject.SetActive(false);
        }
    }

    private void Init() {
        mItems = new ItemInfo[itemsRoot.childCount];

        for(int i = 0; i < itemsRoot.childCount; i++) {
            var t = itemsRoot.GetChild(i);

            var newItm = new ItemInfo(t);

            int clickIndex = i;
            newItm.button.onClick.AddListener(delegate () { OnItemClick(clickIndex); });

            mItems[i] = newItm;
        }

        mCurItemInd = -1;

        if(selectHighlightRoot) selectHighlightRoot.gameObject.SetActive(false);

        mIsInit = true;
    }
}
