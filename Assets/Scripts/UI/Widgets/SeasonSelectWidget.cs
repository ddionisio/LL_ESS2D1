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

        public ItemInfo(Transform t) {
            root = t;
            seasonWidget = t.GetComponent<SeasonWidget>();
            button = t.GetComponent<Button>();
        }
    }

    public Transform itemsRoot; //put items here where it has the component: SeasonWidget, Button
    public Transform selectHighlightRoot;

    [Header("Signal Invoke")]
    public SignalSeasonData signalInvokeSeasonClick;

    private ItemInfo[] mItems;
    private int mCurItemInd;

    private bool mIsInit;

    public void Setup(SeasonData curSeason) {
        if(!mIsInit) Init();

        for(int i = 0; i < mItems.Length; i++) {
            var itm = mItems[i];
            if(itm.data == curSeason) {
                SetSelectItem(i);
                break;
            }
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
