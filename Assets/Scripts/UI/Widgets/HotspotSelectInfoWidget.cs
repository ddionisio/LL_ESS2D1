using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class HotspotSelectInfoWidget : MonoBehaviour {
    [Header("Region Info Display")]
    public Text regionNameLabel;
    public Text climateNameLabel;
    public Image climateIcon;
    public bool climateIconUseNative;

    [Header("Atmosphere Stats Display")]
    public RectTransform atmosphereStatsRoot;
    public HotspotSelectInfoStatItemWidget atmosphereStatItemTemplate; //not prefab
    public int atmosphereStatItemCapacity = 3;

    [Header("Analysis State Display")]
    public GameObject analysisWaitGO;
	public GameObject analysisIncompatibleGO;
	public GameObject analysisInvestigateGO;
}
