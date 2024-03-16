using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "weatherType", menuName = "Game/Weather Type")]
public class WeatherTypeData : ScriptableObject {
    [Header("Info")]
    [SerializeField]
    [M8.Localize]
    string nameRef; //short detail, ex: Mostly Sunny, T-Storms
    [M8.Localize]
    public string detailRef; //description of the weather

    public Sprite icon;
    public Sprite image; //higher quality for forecast and description

    public bool isSunVisible;

    [Header("Hazzard Info")]
    public bool isHazzard;
    public bool isHazzardRetreat; //if units need to retreat

    public float hazzardStartDelay; //when to start hazzard during cycle
    public float hazzardDuration; //how long the hazzard lasts after starting

    //this is a hack
    [System.Serializable]
    public struct nameRefOverrideInfo {
        public int hotspotIndex;
		[M8.Localize]
		public string nameRef;
    }

    [Header("Name Reference Overrides")]
    public nameRefOverrideInfo[] nameRefOverrides;

    public string GetNameType() {
        if(nameRefOverrides != null && nameRefOverrides.Length > 0) {
            var hotspotInd = GameData.instance.savedHotspotIndex;

            for(int i = 0; i < nameRefOverrides.Length; i++) {
                var inf = nameRefOverrides[i];
                if(inf.hotspotIndex == hotspotInd)
                    return M8.Localize.Get(inf.nameRef);
			}
        }

        return M8.Localize.Get(nameRef);
    }
}
