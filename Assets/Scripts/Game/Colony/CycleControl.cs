using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CycleControl : MonoBehaviour {
    private bool mIsInit;

    public virtual void Init() {
        mIsInit = true;
    }

    public virtual void Deinit() { }

    void OnDestroy() {
        if(mIsInit)
            Deinit();
    }
}
