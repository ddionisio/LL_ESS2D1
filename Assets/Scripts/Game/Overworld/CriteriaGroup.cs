using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CriteriaGroup : MonoBehaviour {
    [Header("Items")]
    public GameObject itemsRootGO; //place all CriteriaItem here

    //animation

    public bool active { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }

    public CriteriaData data { get; private set; }

    public CriteriaItem[] items { get { return mItems; } }
    public int criticCount { get { return mItemActiveCount; } }

    public int criticCountGood {
        get {
            int count = 0;

            if(mCriticResults != null) {
                for(int i = 0; i < mCriticResults.Length; i++) {
                    if(mCriticResults[i] == 1)
                        count++;
                }
            }

            return count;
        }
    }

    public int criticCountBad {
        get {
            int count = 0;

            if(mCriticResults != null) {
                for(int i = 0; i < mCriticResults.Length; i++) {
                    if(mCriticResults[i] == -1)
                        count++;
                }
            }

            return count;
        }
    }

    public int criticCountNeutral {
        get {
            int count = 0;

            if(mCriticResults != null) {
                for(int i = 0; i < mCriticResults.Length; i++) {
                    if(mCriticResults[i] == 0)
                        count++;
                }
            }

            return count;
        }
    }

    private bool mIsInit;

    private CriteriaItem[] mItems;
    private int[] mCriticResults;
    private int mItemActiveCount;

    public void Setup(CriteriaData criteriaData) {
        if(!mIsInit)
            Init();
        else
            DeactivateAllItems();

        data = criteriaData;

        mItemActiveCount = Mathf.Clamp(data.criticCount, 0, mItems.Length);

        //show critics
        for(int i = 0; i < mItemActiveCount; i++) {
            var itm = mItems[i];

            itm.state = CriteriaItem.State.None;

            itm.gameObject.SetActive(true);
        }

        if(mCriticResults == null || mCriticResults.Length != mItemActiveCount)
            mCriticResults = new int[mItemActiveCount];
    }

    public void Evaluate(AtmosphereStat[] stats) {
        data.Evaluate(mCriticResults, stats);

        for(int i = 0; i < mItemActiveCount; i++) {
            switch(mCriticResults[i]) {
                case 0: //neutral
                    mItems[i].state = CriteriaItem.State.Neutral;
                    break;

                case 1: //good
                    mItems[i].state = CriteriaItem.State.Good;
                    break;

                case -1: //bad
                    mItems[i].state = CriteriaItem.State.Bad;
                    break;
            }
        }
    }

    private void Init() {
        mItems = itemsRootGO.GetComponentsInChildren<CriteriaItem>(true);
                
        for(int i = 0; i < mItems.Length; i++) {
            //TODO: initialize palette here
            mItems[i].Init();

            mItems[i].gameObject.SetActive(false);
        }

        mItemActiveCount = 0;

        mIsInit = true;
    }

    private void DeactivateAllItems() {
        if(!mIsInit) return;

        for(int i = mItemActiveCount; i < mItems.Length; i++) {
            var itm = mItems[i];

            itm.gameObject.SetActive(false);
        }

        mItemActiveCount = 0;
    }
}
