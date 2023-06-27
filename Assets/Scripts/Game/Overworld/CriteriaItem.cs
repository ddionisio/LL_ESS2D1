using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CriteriaItem : MonoBehaviour {
    public enum State {
        None,
        Neutral,
        Good,
        Bad
    }

    [Header("State Display")]
    public GameObject stateNeutralGO;
    public GameObject stateGoodGO;
    public GameObject stateBadGO;

    //animations

    public State state {
        get { return mState; }
        set {
            if(mState != value) {
                ClearState();

                mState = value;

                ApplyState();
            }
        }
    }

    private State mState;

    //TODO: palette index
    public void Init() {
        if(stateNeutralGO) stateNeutralGO.SetActive(false);
        if(stateGoodGO) stateGoodGO.SetActive(false);
        if(stateBadGO) stateBadGO.SetActive(false);

        mState = State.None;
    }

    private void ApplyState() {
        switch(mState) {
            case State.Neutral:
                if(stateNeutralGO) stateNeutralGO.SetActive(true);
                break;
            case State.Good:
                if(stateGoodGO) stateGoodGO.SetActive(true);
                break;
            case State.Bad:
                if(stateBadGO) stateBadGO.SetActive(true);
                break;
        }
    }

    private void ClearState() {
        switch(mState) {
            case State.Neutral:
                if(stateNeutralGO) stateNeutralGO.SetActive(false);
                break;
            case State.Good:
                if(stateGoodGO) stateGoodGO.SetActive(false);
                break;
            case State.Bad:
                if(stateBadGO) stateBadGO.SetActive(false);
                break;
        }
    }
}
