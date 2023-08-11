using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class HotspotAnalyzeItemWidget : MonoBehaviour {
    [Header("Config")]
    public float revealStartDelay = 0.5f;

    [Header("Analyze Display")]
    public Image analyzeAtmosphereIcon;
    public bool analyzeAtmosphereIconUseNativeSize;

    public TMP_Text analyzeAtmosphereLabel;

    public M8.TextMeshPro.TextMeshProScramble analyzeValueLabel;

    [Header("Criteria Display")]
    public TMP_Text criteriaValueLabel;

    [Header("Match Display")]
    public GameObject matchEqualGO;
    public GameObject matchLessGO;
    public GameObject matchGreaterGO;

    public bool active { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }
    public bool isBusy { get; private set; }
    public int compareResult { get; private set; }

    private M8.RangeFloat mAnalyzeValue;
    private string mAnalyzeValueString;

    private M8.RangeFloat mCriteriaValue;

    public void Setup(AtmosphereAttributeBase atmosphere, M8.RangeFloat analyzeValue, M8.RangeFloat criteriaValue) {
        mAnalyzeValue = analyzeValue;
        mCriteriaValue = criteriaValue;

        if(atmosphere) {
            if(analyzeAtmosphereIcon) {
                analyzeAtmosphereIcon.sprite = atmosphere.icon;
                if(analyzeAtmosphereIconUseNativeSize)
                    analyzeAtmosphereIcon.SetNativeSize();
            }

            if(analyzeAtmosphereLabel)
                analyzeAtmosphereLabel.text = M8.Localize.Get(atmosphere.nameRef);

            if(criteriaValueLabel)
                criteriaValueLabel.text = atmosphere.GetValueRangeString(mCriteriaValue);

            mAnalyzeValueString = atmosphere.GetValueRangeString(mAnalyzeValue);
        }
    }

    public void AnalyzeStart() {
        if(matchEqualGO) matchEqualGO.SetActive(false);
        if(matchLessGO) matchLessGO.SetActive(false);
        if(matchGreaterGO) matchGreaterGO.SetActive(false);

        analyzeValueLabel.ClearFixedString();

        StartCoroutine(DoReveal());
    }

    public void AnalyzeImmediate() {
        isBusy = false;

        analyzeValueLabel.ApplyFixedImmediate(mAnalyzeValueString);

        compareResult = AtmosphereStat.Compare(mAnalyzeValue, mCriteriaValue);
        if(matchEqualGO) matchEqualGO.SetActive(compareResult == 0);
        if(matchLessGO) matchLessGO.SetActive(compareResult < 0);
        if(matchGreaterGO) matchGreaterGO.SetActive(compareResult > 0);
    }

    IEnumerator DoReveal() {
        isBusy = true;

        yield return new WaitForSeconds(revealStartDelay);

        yield return analyzeValueLabel.ApplyFixedStringWait(mAnalyzeValueString);

        compareResult = AtmosphereStat.Compare(mAnalyzeValue, mCriteriaValue);
        switch(compareResult) {
            case 0:
                if(matchEqualGO) matchEqualGO.SetActive(true);
                break;
            case 1:
                if(matchGreaterGO) matchGreaterGO.SetActive(true);
                break;
            case -1:
                if(matchLessGO) matchLessGO.SetActive(true);
                break;
        }

        isBusy = false;
    }
}
