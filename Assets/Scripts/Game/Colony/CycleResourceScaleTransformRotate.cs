using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleResourceScaleTransformRotate : MonoBehaviour {
    public CycleResourceType resourceType;
    public Transform target;
    public Vector3 rotatePerSecondMax;

    void OnDisable() {
        if(target)
            target.localEulerAngles = Vector3.zero;
    }

    void Update() {
        if(target) {
            var cycleCtrl = ColonyController.instance.cycleController;

            var resScale = cycleCtrl.GetResourceScale(resourceType);
            
            var rotAmt = rotatePerSecondMax * Time.deltaTime * resScale;

            target.localEulerAngles += rotAmt;
        }
    }
}
