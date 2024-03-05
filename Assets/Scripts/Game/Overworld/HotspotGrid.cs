using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HotspotGrid : MonoBehaviour {
	[Header("Data")]
	public HotspotData data;

	[Header("Display")]
	public GameObject rootGO;
	public GameObject selectGO;

	[Header("Animation")]
	public M8.Animator.Animate animator;
	[M8.Animator.TakeSelector]
	public int takeActive = -1;

	[Header("Signal Invoke")]
	public SignalHotspotGrid signalInvokeClick;

	public Vector2 position { get { return transform.position; } }

	public bool isBusy { get { return mRout != null; } } //wait for animation

	public bool isSelected {
		get { return mIsSelected; }
		set {
			if(mIsSelected != value) {
				mIsSelected = value;

				ApplySelectDisplay();
			}
		}
	}

	private Coroutine mRout;

	private bool mIsSelected;
	private bool mIsHover;

	public bool GetStat(SeasonData season, AtmosphereAttributeBase atmosphere, out M8.RangeFloat outputStat) {
		var stats = data.GetAtmosphereStats(season);

		for(int i = 0; i < stats.Length; i++) {
			if(stats[i].atmosphere == atmosphere) {
				outputStat = stats[i].range;
				return true;
			}
		}

		outputStat = new M8.RangeFloat();
		return false;
	}

	/// <summary>
	/// Call when clicked to enter investigation mode
	/// </summary>
	public void Click() {
		signalInvokeClick?.Invoke(this);
	}

	public void OnPointerEnter(PointerEventData eventData) {
		mIsHover = true;

		ApplySelectDisplay();
	}

	public void OnPointerExit(PointerEventData eventData) {
		mIsHover = false;

		ApplySelectDisplay();
	}

	void OnDisable() {
		mIsHover = false;

		if(mRout != null) {
			StopCoroutine(mRout);
			mRout = null;
		}
	}

	void OnEnable() {
		if(selectGO) selectGO.SetActive(false);

		if(rootGO) rootGO.SetActive(true);

		mRout = StartCoroutine(DoShow());
	}

	IEnumerator DoShow() {
		if(takeActive != -1)
			yield return animator.PlayWait(takeActive);

		ApplySelectDisplay();

		mRout = null;
	}

	private void ApplySelectDisplay() {
		if(selectGO) selectGO.SetActive(mIsSelected || mIsHover);
	}
}
