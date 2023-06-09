using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SeasonSelectWidget : MonoBehaviour {
    public struct ItemInfo {
        public Transform root;
        public SeasonWidget seasonWidget;
        public Button button;

        public SeasonData data { get { return seasonWidget ? seasonWidget.data : null; } }

        public bool selectActive { 
            get { return seasonWidget ? seasonWidget.selectActive : false; } 
            set { if(seasonWidget) seasonWidget.selectActive = value; }
        }

        public ItemInfo(Transform t) {
            root = t;
            seasonWidget = t.GetComponent<SeasonWidget>();
            button = t.GetComponent<Button>();
        }

        public ItemInfo(SeasonWidget aSeasonWidget) {
            root = aSeasonWidget.transform;
            seasonWidget = aSeasonWidget;
            button = aSeasonWidget.GetComponent<Button>();
        }
    }

    public Transform itemsRoot; //put items here where it has the component: SeasonWidget, Button

    [Header("Signal Invoke")]
    public SignalSeasonData signalInvokeSeasonClick;

    private ItemInfo[] mItems;
    private int mCurItemInd;

    private bool mIsInit;

    public void Setup(SeasonData curSeason) {
        if(!mIsInit) Init();

        mCurItemInd = -1;

        for(int i = 0; i < mItems.Length; i++) {
            var itm = mItems[i];

            if(itm.data == curSeason) {
                itm.selectActive = true;

                mCurItemInd = i;
            }
            else
                itm.selectActive = false;
        }
    }

    void Awake() {
        if(!mIsInit) Init();
    }

    void OnItemClick(int index) {
        if(mCurItemInd == index)
            return;

        SetSelectItem(index);

        var itm = mItems[index];

        signalInvokeSeasonClick?.Invoke(itm.seasonWidget ? itm.seasonWidget.data : null);
    }

    private void SetSelectItem(int index) {
        if(mCurItemInd >= 0 && mCurItemInd < mItems.Length)
            mItems[mCurItemInd].selectActive = false;

        mCurItemInd = index;

        if(mCurItemInd >= 0 && mCurItemInd < mItems.Length)
            mItems[mCurItemInd].selectActive = true;
    }

    private void Init() {
        var seasonWidgetList = new List<SeasonWidget>();
                        
        for(int i = 0; i < itemsRoot.childCount; i++) {
            var t = itemsRoot.GetChild(i);

            var seasonWidget = t.GetComponent<SeasonWidget>();
            if(seasonWidget)
                seasonWidgetList.Add(seasonWidget);
        }

        mItems = new ItemInfo[seasonWidgetList.Count];

        for(int i = 0; i < seasonWidgetList.Count; i++) {
            var newItm = new ItemInfo(seasonWidgetList[i]);

            int clickIndex = i;
            newItm.button.onClick.AddListener(delegate () { OnItemClick(clickIndex); });

            mItems[i] = newItm;
        }

        mIsInit = true;
    }
}
