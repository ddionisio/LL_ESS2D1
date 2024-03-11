using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class StructureSpecialActionWindTurbineForce : StructureSpecialAction, IPointerClickHandler {
	[Header("Data")]
	public float actionDuration;

	[Header("Display")]
	public GameObject popupActiveGO;
	public M8.UI.Events.HoverGOSetActive hoverGOSetActive;

	public GameObject actionGO;

	[Header("Animation")]
	public M8.Animator.Animate animator;
	[M8.Animator.TakeSelector]
	public int takeActivate = -1;
	[M8.Animator.TakeSelector]
	public int takeDeactivate = -1;

	private Coroutine mActionRout;

	protected override void Activate(bool active) {
		if(hoverGOSetActive)
			hoverGOSetActive.enabled = active;

		if(popupActiveGO)
			popupActiveGO.SetActive(active);

		//reset state
		if(!active) {
			if(mActionRout != null) {
				StopCoroutine(mActionRout);
				mActionRout = null;
			}

			if(actionGO)
				actionGO.SetActive(false);

			if(animator && animator.currentPlayingTakeIndex == takeActivate)
				animator.ResetTake(takeActivate);
		}
	}

	protected override void OnDisable() {
		base.OnDisable();

		if(popupActiveGO)
			popupActiveGO.SetActive(false);

		if(actionGO)
			actionGO.SetActive(false);

		if(hoverGOSetActive)
			hoverGOSetActive.enabled = false;
				
		if(animator && takeActivate != -1)
			animator.ResetTake(takeActivate);

		if(mActionRout != null) {
			StopCoroutine(mActionRout);
			mActionRout = null;
		}
	}

	protected override void OnEnable() {
		base.OnEnable();

		if(popupActiveGO)
			popupActiveGO.SetActive(false);

		if(actionGO)
			actionGO.SetActive(false);

		if(animator && takeActivate != -1)
			animator.ResetTake(takeActivate);
	}

	protected override void Awake() {
		base.Awake();

		hoverGOSetActive = GetComponent<M8.UI.Events.HoverGOSetActive>();
	}

	void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
		if(!isActive)
			return;

		if(mActionRout == null)
			mActionRout = StartCoroutine(OnAction());
	}

	IEnumerator OnAction() {
		if(popupActiveGO)
			popupActiveGO.SetActive(false);

		if(actionGO)
			actionGO.SetActive(true);

		if(animator && takeActivate != -1)
			yield return animator.PlayWait(takeActivate);

		yield return new WaitForSeconds(actionDuration);

		if(animator && takeDeactivate != -1)
			yield return animator.PlayWait(takeDeactivate);

		if(popupActiveGO)
			popupActiveGO.SetActive(isActive);

		if(actionGO)
			actionGO.SetActive(false);

		mActionRout = null;
	}
}
