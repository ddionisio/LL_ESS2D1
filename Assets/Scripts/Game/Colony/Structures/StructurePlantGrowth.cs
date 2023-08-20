using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructurePlantGrowth : MonoBehaviour {
    [Header("Leaves")]
    public int leafCount = 4;

    [Header("Sway")]
    public M8.RangeFloat swayAngleRange;
    public M8.RangeFloat swayDelayRange;
    public M8.RangeFloat swayWaitDelayRange;

    [Header("Display")]
    public Transform stemRoot;
    public Transform topRoot; //root for bud and blossom
    public GameObject budGO;
    public GameObject blossomGO;

    [Header("Stem")]
    public StructurePlantStem stemTemplate;
    public float stemGrowth = 1f;
    public int stemMaxCount = 3;

    public bool isBlossomed { get { return blossomGO ? blossomGO.activeSelf : false; } }

    public Vector2 topPosition {
        get {
            var curStem = mStems[mCurStemIndex];
            return curStem.topWorldPosition;
        }
    }

    public Transform blossomTransform { get { return blossomGO ? blossomGO.transform : null; } }

    private StructurePlantStem[] mStems;
    private int mCurStemIndex;

    private Coroutine mSwayRout;
    private Quaternion[] mSwayStemRotStarts;

    private bool mIsInit;

    public void Init() {
        if(!mIsInit) {
            //initialize stems
            mStems = new StructurePlantStem[stemMaxCount];
            mSwayStemRotStarts = new Quaternion[stemMaxCount];

            var curStemRoot = stemRoot;
            var curStemPos = Vector3.zero;

            bool leafFlip = Random.Range(0, 2) == 0;

            for(int i = 0; i < stemMaxCount; i++) {
                var stem = Instantiate(stemTemplate, curStemRoot);

                stem.maxGrowth = stemGrowth;
                stem.Init(leafCount, leafFlip, i < stemMaxCount - 1);

                stem.transform.localPosition = curStemPos;
                stem.active = false;

                curStemRoot = stem.transform;
                curStemPos = stem.topLocalMaxPosition;

                if(leafCount % 2 != 0)
                    leafFlip = !leafFlip;

                mStems[i] = stem;
            }

            if(budGO) budGO.SetActive(false);
            if(blossomGO) blossomGO.SetActive(false);

            topRoot.SetParent(mStems[0].transform, false);
            topRoot.localPosition = Vector3.zero;

            mCurStemIndex = 0;

            mIsInit = true;
        }
    }

    public void HideBlossom() {
        if(blossomGO) blossomGO.SetActive(false);
    }

    public void RefreshTopPosition() {
        var curStem = mStems[mCurStemIndex];

        if(topRoot.parent != curStem.transform) {
            topRoot.SetParent(curStem.transform, false);
            topRoot.localRotation = Quaternion.identity;
        }

        topRoot.position = curStem.topWorldPosition;
    }

    public void ApplyGrowth(float t) {
        t = Mathf.Clamp01(t); //just in case

        float totalLen = t * mStems.Length;
        float totalLenFloor = Mathf.Floor(totalLen);

        mCurStemIndex = Mathf.Clamp((int)totalLenFloor, 0, mStems.Length - 1);

        float curStemT;
        if(totalLen == totalLenFloor) {
            if(mCurStemIndex == mStems.Length - 1)
                curStemT = 1f;
            else
                curStemT = 0f;
        }
        else
            curStemT = Mathf.Clamp01(totalLen - totalLenFloor);

        for(int i = 0; i < mStems.Length; i++) {
            var stem = mStems[i];

            if(i < mCurStemIndex) {
                stem.active = true;
                stem.growth = stem.maxGrowth;
            }
            else if(i == mCurStemIndex) {
                stem.active = true;
                stem.growth = stem.maxGrowth * curStemT;
            }
            else
                stem.active = false;
        }

        RefreshTopPosition();

        if(t < 1f) {
            if(budGO) budGO.SetActive(true);
            if(blossomGO) blossomGO.SetActive(false);
        }
        else {
            if(budGO) budGO.SetActive(false);
            if(blossomGO) blossomGO.SetActive(true);
        }
    }

    void OnDisable() {
        SetSwayActive(false);
    }

    void OnEnable() {
        SetSwayActive(true);
    }

    void Awake() {
        Init();
    }

    private void SetSwayActive(bool active) {
        if(active) {
            mSwayRout = StartCoroutine(DoSway());
        }
        else if(mSwayRout != null) {
            StopCoroutine(mSwayRout);
            mSwayRout = null;
        }
    }

    IEnumerator DoSway() {
        var easeFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(DG.Tweening.Ease.InOutSine);
        float angleSign = Random.Range(0, 2) == 0 ? 1f : -1f;

        while(true) {
            //initialize stem rots
            for(int i = 0; i < stemMaxCount; i++)
                mSwayStemRotStarts[i] = mStems[i].transform.localRotation;

            var targetRot = Quaternion.Euler(0f, 0f, angleSign * swayAngleRange.random);

            float curTime = 0f;
            float delay = swayDelayRange.random;
            while(curTime < delay) {
                yield return null;

                curTime += Time.deltaTime;

                float t = easeFunc(curTime, delay, 0f, 0f);

                for(int i = 0; i <= mCurStemIndex; i++) {
                    var stemT = mStems[i].transform;
                    stemT.localRotation = Quaternion.Lerp(mSwayStemRotStarts[i], targetRot, t);
                }
            }

            yield return new WaitForSeconds(swayWaitDelayRange.random);

            angleSign *= -1f;

            yield return null;
        }
    }
}
