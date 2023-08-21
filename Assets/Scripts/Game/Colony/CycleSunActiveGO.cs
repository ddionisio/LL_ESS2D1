using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleSunActiveGO : MonoBehaviour {
    public GameObject sunActiveGO;
    public GameObject sunInvisibleActiveGO;

    private bool mIsSunVisible;

    void OnEnable() {
        if(ColonyController.isInstantiated) {
            mIsSunVisible = ColonyController.instance.cycleController.cycleIsSunVisible;
            ApplyActiveGO();
        }
        else {
            mIsSunVisible = false;
            if(sunActiveGO) sunActiveGO.SetActive(false);
            if(sunInvisibleActiveGO) sunInvisibleActiveGO.SetActive(false);
        }
    }

    void Update() {
        if(ColonyController.isInstantiated) {
            var isDay = ColonyController.instance.cycleController.cycleIsSunVisible;
            if(mIsSunVisible != isDay) {
                mIsSunVisible = isDay;
                ApplyActiveGO();
            }
        }
    }

    void ApplyActiveGO() {
        if(sunActiveGO) sunActiveGO.SetActive(mIsSunVisible);
        if(sunInvisibleActiveGO) sunInvisibleActiveGO.SetActive(!mIsSunVisible);
    }
}
