using M8.Animator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class ColonySpecialAction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
    [Header("Display")]
    public Transform cursorRoot;
	public float cursorFollowSpeed = 0.2f;
	
    [Header("Signal Listen")]
    public M8.Signal signalListenActivate;

	[Header("SFX")]
	[M8.SoundPlaylist]
	public string sfxAction;

	private Collider2D mColl;

	private PointerEventData mPointer;

	private Vector2 mCursorPos;
	private Vector2 mCursorVel;

	protected abstract void Action(Vector2 point);

	protected virtual void OnDisable() {
		mPointer = null;
	}

	protected virtual void OnEnable() {
		mColl.enabled = false;
	}

	protected virtual void OnDestroy() {
		if(signalListenActivate) signalListenActivate.callback -= OnActivate;
	}

	protected virtual void Awake() {
		mColl = GetComponent<Collider2D>();

		if(signalListenActivate) signalListenActivate.callback += OnActivate;
	}

	protected virtual void Update() {
		if(mPointer != null && cursorRoot) {
			var hit = mPointer.pointerCurrentRaycast;
			if(hit.isValid) {
				mCursorPos = hit.worldPosition;
			}

			Vector2 pos = cursorRoot.position;
			cursorRoot.position = Vector2.SmoothDamp(pos, mCursorPos, ref mCursorVel, cursorFollowSpeed);
		}
	}

	void OnActivate() {
		mColl.enabled = true;
	}

	void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
		mColl.enabled = false;

		if(cursorRoot)
			cursorRoot.gameObject.SetActive(false);

		mPointer = null;

		var hit = eventData.pointerCurrentRaycast;
		if(hit.isValid) {
			if(!string.IsNullOrEmpty(sfxAction))
				M8.SoundPlaylist.instance.Play(sfxAction, false);

			Vector2 pt = hit.worldPosition;

			Action(pt);
		}
	}

	void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
		mPointer = eventData;

		mCursorVel = Vector2.zero;

		if(eventData.pointerCurrentRaycast.isValid)
			mCursorPos = eventData.pointerCurrentRaycast.worldPosition;
		else
			mCursorPos = Vector2.zero;

		if(cursorRoot) {
			cursorRoot.gameObject.SetActive(true);
			cursorRoot.position = mCursorPos;
		}
	}

	void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
		mPointer = null;

		if(cursorRoot)
			cursorRoot.gameObject.SetActive(false);
	}
}
