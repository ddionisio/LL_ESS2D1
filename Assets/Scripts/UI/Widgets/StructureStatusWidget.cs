using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Make sure the name correlates to any in StructureStatus
/// </summary>
public class StructureStatusWidget : MonoBehaviour {
    [Header("Display")]
    public Image progressImage;
    public GameObject requireActiveGO;

    public bool active { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }

    public bool requireActive {
        get { return requireActiveGO ? requireActiveGO.activeSelf : false; }
        set {
            if(requireActiveGO)
                requireActiveGO.SetActive(value);
        }
    }

    public float progressFill {
        get { return mProgressFillAmount; }
        set {
            if(mProgressFillAmount != value) {
                mProgressFillAmount = value;

                if(progressImage)
                    progressImage.fillAmount = Mathf.Clamp01(value);
            }
        }
    }

    private StructureStatusState mState;
    private float mProgressFillAmount;

    public void Apply(StructureStatusInfo inf) {
        var state = inf.state;

        if(mState != state) {
            mState = state;
            ApplyCurrentState();
        }

        if(mState != StructureStatusState.None)
            progressFill = inf.progress;
    }

    public void ResetState() {
        mState = StructureStatusState.None;
        ApplyCurrentState();        
    }

    private void ApplyCurrentState() {
        switch(mState) {
            case StructureStatusState.Require:
                active = true;
                requireActive = true;
                break;

            case StructureStatusState.Progress:
                active = true;
                requireActive = false;
                break;

            default:
                active = false;
                requireActive = false;
                progressFill = 0f;
                break;
        }
    }
}
