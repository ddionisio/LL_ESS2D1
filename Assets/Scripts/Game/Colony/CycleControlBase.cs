using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CycleControlBase : MonoBehaviour {
    protected virtual void Init() { }

    protected virtual void Deinit() { }

    protected virtual void Begin() { }
    protected virtual void Next() { }
    protected virtual void End() { }

    private bool mIsInit;

    protected virtual void Awake() {
        var gameDat = GameData.instance;

        if(gameDat.signalColonyStart) gameDat.signalColonyStart.callback += OnColonyStart;

        if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback += Begin;
        if(gameDat.signalCycleNext) gameDat.signalCycleNext.callback += Next;
        if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback += End;
    }

    protected virtual void OnDestroy() {
        if(GameData.isInstantiated) {
            var gameDat = GameData.instance;

            if(gameDat.signalColonyStart) gameDat.signalColonyStart.callback -= OnColonyStart;

            if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback -= Begin;
            if(gameDat.signalCycleNext) gameDat.signalCycleNext.callback -= Next;
            if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback -= End;
        }

        if(mIsInit)
            Deinit();
    }

    void OnColonyStart() {
        Init();
        mIsInit = true;
    }
}
