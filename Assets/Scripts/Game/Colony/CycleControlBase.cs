using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CycleControlBase : MonoBehaviour {
    public virtual void Init() { }

    public virtual void Deinit() { }

    private bool mIsInit;

    void Awake() {
        if(GameData.instance.signalColonyStart) GameData.instance.signalColonyStart.callback += OnColonyStart;
    }

    void OnDestroy() {
        if(GameData.instance.signalColonyStart) GameData.instance.signalColonyStart.callback -= OnColonyStart;

        if(mIsInit)
            Deinit();
    }

    void OnColonyStart() {
        Init();
        mIsInit = true;
    }
}
