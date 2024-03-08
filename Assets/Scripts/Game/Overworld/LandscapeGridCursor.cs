using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandscapeGridCursor : MonoBehaviour {
    [Header("Data")]
    public float radiusLanding;
	public float radiusCheck;

    [Header("Display")]
    public M8.SpriteColorGroup colorGroup;
    public Color colorValid = Color.green;
    public Color colorInvalid = Color.red;

    [Header("Animation")]
    public M8.Animator.Animate animator;
	[M8.Animator.TakeSelector]
	public int takeIdle = -1;
	[M8.Animator.TakeSelector]
	public int takePlacement = -1;
	[M8.Animator.TakeSelector]
    public int takeInvalid = -1;

    public bool active { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }

    public Vector2 position { get { return transform.position; } set {  transform.position = value; } }

    public bool isValid {
        get { return mIsValid; }
        set {
            if(mIsValid != value) {
                mIsValid = value;
                UpdateDisplay();
            }
        }
    }

    private bool mIsValid;

	public void PlayPlacement() {
		if(animator && takePlacement != -1)
			animator.Play(takePlacement);
	}

	public void PlayInvalid() {
        if(animator && takeInvalid != -1)
            animator.Play(takeInvalid);
    }

	void OnTakeFinish(M8.Animator.Animate anim, M8.Animator.Take take) {
        if(takeIdle != -1)
            anim.Play(takeIdle);
    }

	void OnDisable() {
        if(animator) animator.takeCompleteCallback -= OnTakeFinish;
	}

	void OnEnable() {
		if(animator) animator.takeCompleteCallback += OnTakeFinish;

		UpdateDisplay();
	}

	void OnDrawGizmos() {
        var pos = transform.position;

        if(radiusLanding > 0f) {
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(pos, radiusLanding);
        }

        if(radiusCheck > 0f) {
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(pos, radiusCheck);
		}
	}

	private void UpdateDisplay() {
        if(colorGroup) {
            if(mIsValid)
                colorGroup.color = colorValid;
            else
				colorGroup.color = colorInvalid;
		}
    }
}
