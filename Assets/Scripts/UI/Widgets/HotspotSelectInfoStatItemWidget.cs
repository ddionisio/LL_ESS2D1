using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class HotspotSelectInfoStatItemWidget : MonoBehaviour {
    [Header("Display")]
	public TMP_Text nameLabel;
	public Image icon;
	public bool iconUseNative;

	[Header("Match Color")]
	public Color matchColorEmpty = Color.white;
	public Color matchColorEqual = Color.white;
	public Color matchColorNotEqual = Color.white;

	public GameObject matchEqualGO;
	public GameObject matchLessGO;
	public GameObject matchGreaterGO;

	public bool active { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }

	public void SetupEmpty(AtmosphereAttributeBase atmosphere) {
		if(icon) {
			icon.sprite = atmosphere.icon;
			if(iconUseNative)
				icon.SetNativeSize();
		}

		if(nameLabel) {
			nameLabel.text = string.Format("--{0}", atmosphere.symbolString);
			nameLabel.color = matchColorEmpty;
		}

		if(matchEqualGO) matchEqualGO.SetActive(false);
		if(matchLessGO) matchLessGO.SetActive(false);
		if(matchGreaterGO) matchGreaterGO.SetActive(false);
	}

	public void Setup(AtmosphereAttributeBase atmosphere, float val, Hotspot.AnalyzeResult analyzeResult) {
		if(icon) {
			icon.sprite = atmosphere.icon;
			if(iconUseNative)
				icon.SetNativeSize();
		}

		if(nameLabel) {
			nameLabel.text = atmosphere.GetValueString(Mathf.RoundToInt(val));
			nameLabel.color = analyzeResult == Hotspot.AnalyzeResult.Equal ? matchColorEqual : matchColorNotEqual;
		}

		if(matchEqualGO) matchEqualGO.SetActive(analyzeResult == Hotspot.AnalyzeResult.Equal);
		if(matchLessGO) matchLessGO.SetActive(analyzeResult == Hotspot.AnalyzeResult.Less);
		if(matchGreaterGO) matchGreaterGO.SetActive(analyzeResult == Hotspot.AnalyzeResult.Greater);
	}
}
