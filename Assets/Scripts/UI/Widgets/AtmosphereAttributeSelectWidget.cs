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

        public AtmosphereAttributeBase data { get { return atmosphereWidget ? atmosphereWidget.data : null; } }

        public bool selectActive { 
            get { return atmosphereWidget ? atmosphereWidget.selectActive : false; } 
            set {
                if(atmosphereWidget)
                    atmosphereWidget.selectActive = value;
            }
        }

        public ItemInfo(Transform t) {
            root = t;
            atmosphereWidget = t.GetComponent<AtmosphereAttributeWidget>();
            button = t.GetComponent<Button>();
        }

        public ItemInfo(AtmosphereAttributeWidget atmosphereAttrWidget) {
            root = atmosphereAttrWidget.transform;
            atmosphereWidget = atmosphereAttrWidget;
            button = atmosphereAttrWidget.GetComponent<Button>();
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

    [Header("Signal Invoke")]
    public SignalAtmosphereAttribute signalInvokeAtmosphereClick;

    private ItemInfo[] mItems;
    private int mCurItemInd;

    private bool mIsInit;

    public void Setup(AtmosphereAttributeBase selectAtmosphere, AtmosphereAttributeBase[] activeAtmosphereAttributes) {
        if(!mIsInit) Init();

        int selectIndex = -1;

        //activate items that match, hide otherwise
        for(int i = 0; i < mItems.Length; i++) {
            var itm = mItems[i];

            itm.ApplyActive(activeAtmosphereAttributes);

            itm.selectActive = false;

            if(itm.data == selectAtmosphere)
                selectIndex = i;
        }

        mCurItemInd = -1;

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

        signalInvokeAtmosphereClick?.Invoke(itm.data);
    }

    private void SetSelectItem(int index) {
        if(mCurItemInd >= 0 && mCurItemInd < mItems.Length)
            mItems[mCurItemInd].selectActive = false;

        mCurItemInd = index;

        if(mCurItemInd >= 0 && mCurItemInd < mItems.Length)
            mItems[mCurItemInd].selectActive = true;
    }

    private void Init() {
        var atmosphereAttrWidgetList = new List<AtmosphereAttributeWidget>();
                        
        for(int i = 0; i < itemsRoot.childCount; i++) {
            var t = itemsRoot.GetChild(i);

            var atmosAttrWidget = t.GetComponent<AtmosphereAttributeWidget>();
            if(atmosAttrWidget)
                atmosphereAttrWidgetList.Add(atmosAttrWidget);
        }

        mItems = new ItemInfo[atmosphereAttrWidgetList.Count];

        for(int i = 0; i < atmosphereAttrWidgetList.Count; i++) {
            var newItm = new ItemInfo(atmosphereAttrWidgetList[i]);

            int clickIndex = i;
            newItm.button.onClick.AddListener(delegate () { OnItemClick(clickIndex); });

            mItems[i] = newItm;
        }

        mIsInit = true;
    }
}
