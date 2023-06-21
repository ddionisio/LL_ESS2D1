using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CriteriaWidget : MonoBehaviour {
    [Header("Item Info")]
    public CriteriaItemWidget itemTemplate; //assume template is not prefab (part of the root)
    public Transform itemRoot; //items are added here
    public int itemCapacity = 3;

    private bool mIsInit;
    private M8.CacheList<CriteriaItemWidget> mActives;
    private M8.CacheList<CriteriaItemWidget> mCache;

    public void Setup(CriteriaData criteriaData) {
        //initialize pool?
        if(!mIsInit)
            Init();
        else //clear out actives
            Clear();

        for(int i = 0; i < criteriaData.attributes.Length; i++) {
            var attr = criteriaData.attributes[i];

            if(mCache.Count == 0)
                break;

            var attrBounds = attr.rangeBounds;

            var itm = mCache.RemoveLast();

            itm.Setup(attr.atmosphere, Mathf.RoundToInt(attrBounds.min), Mathf.RoundToInt(attrBounds.max));

            itm.transform.SetAsLastSibling();

            itm.gameObject.SetActive(true);

            mActives.Add(itm);
        }
    }

    private void Clear() {
        for(int i = 0; i < mActives.Count; i++) {
            var itm = mActives[i];

            itm.gameObject.SetActive(false);

            mCache.Add(itm);
        }

        mActives.Clear();
    }

    private void Init() {
        mActives = new M8.CacheList<CriteriaItemWidget>(itemCapacity);
        mCache = new M8.CacheList<CriteriaItemWidget>(itemCapacity);

        for(int i = 0; i < itemCapacity; i++) {
            var newItem = Instantiate(itemTemplate);

            newItem.transform.SetParent(itemRoot, false);

            newItem.gameObject.SetActive(false);

            mCache.Add(newItem);
        }

        itemTemplate.gameObject.SetActive(false);

        mIsInit = true;
    }
}
