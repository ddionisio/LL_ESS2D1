using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Activate "rootGO" if the current structure placement valid layer matches this gameObject's layer.
/// </summary>
public class StructurePlacementActivateGO : MonoBehaviour {
    public GameObject rootGO;

    void OnDisable() {
        if(rootGO && GameData.isInstantiated)
            GameData.instance.signalPlacementActive.callback -= OnPlacementActivate;
    }

    void OnEnable() {
        if(rootGO) {
            GameData.instance.signalPlacementActive.callback += OnPlacementActivate;

            //check if we are currently in placement mode
            if(ColonyController.isInstantiated && ColonyController.instance.structureController.placementCurrentStructureData)
                OnPlacementActivate(true);
            else
                rootGO.SetActive(false);
        }
    }

    void OnPlacementActivate(bool active) {
        var structureCtrl = ColonyController.instance.structureController;

        rootGO.SetActive(active && structureCtrl.placementCurrentStructureData.IsPlacementLayerValid(gameObject.layer));
    }
}
