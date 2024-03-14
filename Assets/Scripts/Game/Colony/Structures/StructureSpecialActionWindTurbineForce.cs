using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class StructureSpecialActionWindTurbineForce : StructureSpecialAction, IPointerClickHandler {
	[Header("Data")]
	public float actionDuration;
	public float cooldownDuration;

	[Header("Display")]
	public GameObject popupActiveGO;	
	public GameObject actionGO;

	[Header("Animation")]
	public M8.Animator.Animate animator;
	[M8.Animator.TakeSelector]
	public int takeActivate = -1;
	[M8.Animator.TakeSelector]
	public int takeDeactivate = -1;

	[Header("SFX")]
	[M8.SoundPlaylist]
	public string sfxActive;

	private M8.UI.Events.HoverGOSetActive mHoverGOSetActive;

	private Coroutine mActionRout;

	protected override void ApplyActivate(bool active) {
		if(mHoverGOSetActive)
			mHoverGOSetActive.enabled = active;

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

			if(animator) {
				if(animator.currentPlayingTakeIndex == takeActivate || animator.currentPlayingTakeIndex == takeDeactivate)
					animator.Stop();

				if(takeActivate != -1)
					animator.ResetTake(takeActivate);
			}
		}
	}

	protected override void OnDisable() {
		base.OnDisable();

		if(popupActiveGO)
			popupActiveGO.SetActive(false);

		if(actionGO)
			actionGO.SetActive(false);

		if(mHoverGOSetActive)
			mHoverGOSetActive.enabled = false;
				
		if(animator && takeActivate != -1)
			animator.ResetTake(takeActivate);

		if(mActionRout != null) {
			StopCoroutine(mActionRout);
			mActionRout = null;
		}
	}

	protected override void Awake() {
		base.Awake();

		mHoverGOSetActive = GetComponent<M8.UI.Events.HoverGOSetActive>();
	}

	void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
		if(!isActive)
			return;

		if(mActionRout == null)
			mActionRout = StartCoroutine(OnAction());
	}

	IEnumerator OnAction() {
		if(mHoverGOSetActive)
			mHoverGOSetActive.enabled = false;

		if(popupActiveGO)
			popupActiveGO.SetActive(false);

		if(actionGO)
			actionGO.SetActive(true);

		if(!string.IsNullOrEmpty(sfxActive))
			M8.SoundPlaylist.instance.Play(sfxActive, false);
				
		if(animator && takeActivate != -1)
			yield return animator.PlayWait(takeActivate);

		yield return new WaitForSeconds(actionDuration);

		if(animator && takeDeactivate != -1)
			yield return animator.PlayWait(takeDeactivate);

		if(actionGO)
			actionGO.SetActive(false);

		yield return new WaitForSeconds(cooldownDuration);

		if(mHoverGOSetActive)
			mHoverGOSetActive.enabled = isActive;

		if(popupActiveGO)
			popupActiveGO.SetActive(isActive);
				
		mActionRout = null;
	}
}
