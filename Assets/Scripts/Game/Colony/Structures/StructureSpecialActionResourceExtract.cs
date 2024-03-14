using M8.Animator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class StructureSpecialActionResourceExtract : StructureSpecialAction, IPointerClickHandler {
	[Header("Display")]
	public GameObject popupActiveGO;

	[Header("Signal Invoke")]
	public M8.Signal signalInvokeResourceExtract;

	[Header("SFX")]
	[M8.SoundPlaylist]
	public string sfxClick;
	
	private StructureResourceGenerateContainer mStructure;
	private M8.UI.Events.HoverGOSetActive mHoverGOSetActive;

	private bool mIsReady;

	protected override void ApplyActivate(bool active) {
		RefreshReadyState();
		RefreshReadyDisplay();
	}

	protected override void OnDisable() {
		base.OnDisable();

		if(popupActiveGO)
			popupActiveGO.SetActive(false);

		if(mHoverGOSetActive)
			mHoverGOSetActive.enabled = false;
	}

	protected override void Awake() {
		base.Awake();

		mStructure = GetComponent<StructureResourceGenerateContainer>();
		mHoverGOSetActive = GetComponent<M8.UI.Events.HoverGOSetActive>();
	}

	void Update() {
		if(isActive) {
			if(RefreshReadyState())
				RefreshReadyDisplay();
		}
	}

	void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
		if(!mIsReady || !isActive)
			return;

		if(mStructure)
			mStructure.resourceWhole--;

		if(!string.IsNullOrEmpty(sfxClick))
			M8.SoundPlaylist.instance.Play(sfxClick, false);

		if(RefreshReadyState())
			RefreshReadyDisplay();

		if(signalInvokeResourceExtract)
			signalInvokeResourceExtract.Invoke();
	}

	//return true if changed
	private bool RefreshReadyState() {
		if(!mStructure) {
			mIsReady = false;
			return false;
		}

		var prevReady = mIsReady;

		mIsReady = mStructure.resourceWhole > 0 && mStructure.isDamageable;

		return mIsReady != prevReady;
	}

	private void RefreshReadyDisplay() {
		var _active = isActive && mIsReady;

		if(mHoverGOSetActive)
			mHoverGOSetActive.enabled = _active;

		if(popupActiveGO)
			popupActiveGO.SetActive(_active);
	}
}
