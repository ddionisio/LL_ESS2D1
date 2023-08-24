using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleControlColorCamera : CycleControlColorBase {
    private Camera mCamera;

    protected override void ApplyColor(Color color) {
        if(mCamera) 
            mCamera.backgroundColor = color;
    }

    protected override void Awake() {
        mCamera = Camera.main;

        base.Awake();
    }
}
