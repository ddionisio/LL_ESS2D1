using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorPulseWait : MonoBehaviour {
	public float startDelay;
	public float fadeDelay;
	public float midDelay;
	public float endDelay;
		
	public Color startColor;
	public Color endColor = Color.white;

	public bool realtime = false;

	private bool mStarted = false;

	private Color mDefaultColor;

	private Graphic mTarget;

	void OnEnable() {
		if(mStarted) {
			mTarget.color = startColor;
			StartCoroutine(DoPulse());
		}
	}

	void OnDisable() {
		if(mStarted) {
			mTarget.color = mDefaultColor;
		}
	}

	void Awake() {
		mTarget = GetComponent<Graphic>();
		mDefaultColor = mTarget.color;
	}

	// Use this for initialization
	void Start() {
		mStarted = true;
		mTarget.color = startColor;
		StartCoroutine(DoPulse());
	}

	IEnumerator DoPulse() {
		float time = realtime ? Time.realtimeSinceStartup : Time.time;
		float lastTime = time;

		while(time - lastTime < startDelay) {
			yield return null;
			time = realtime ? Time.realtimeSinceStartup : Time.time;
		}

		while(true) {
			lastTime = time;
			while(time - lastTime < fadeDelay) {
				yield return null;
				time = realtime ? Time.realtimeSinceStartup : Time.time;

				float t = Mathf.Sin(Mathf.Clamp01((time - lastTime)/fadeDelay) * Mathf.PI * 0.5f);

				mTarget.color = Color.Lerp(startColor, endColor, t);
			}

			lastTime = time;
			while(time - lastTime < midDelay) {
				yield return null;
				time = realtime ? Time.realtimeSinceStartup : Time.time;
			}

			lastTime = time;
			while(time - lastTime < fadeDelay) {
				yield return null;
				time = realtime ? Time.realtimeSinceStartup : Time.time;

				float t = Mathf.Sin(Mathf.Clamp01((time - lastTime) / fadeDelay) * Mathf.PI * 0.5f);

				mTarget.color = Color.Lerp(endColor, startColor, t);
			}

			lastTime = time;
			while(time - lastTime < endDelay) {
				yield return null;
				time = realtime ? Time.realtimeSinceStartup : Time.time;
			}
		}
	}
}
