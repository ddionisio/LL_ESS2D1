using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColonySpecialActionUnitDamage : ColonySpecialAction {
	public class Item {
		public GameObject gameObject { get; private set; }
		public Transform transform { get; private set; }

		private M8.Animator.Animate mAnimator;
		private float mLastPlayTime;
		
		public bool isPlaying {
			get {
				return mAnimator ? mAnimator.isPlaying : Time.time - mLastPlayTime > 1f;
			}
		}

		public Vector2 position { get { return transform ? transform.position : Vector2.zero; } set { if(transform) transform.position = value; } }

		public void Stop() {
			if(mAnimator)
				mAnimator.Stop();

			if(gameObject)
				gameObject.SetActive(false);
		}

		public void Play() {
			gameObject.SetActive(true);

			if(mAnimator && mAnimator.takeCount > 0)
				mAnimator.Play(0);

			mLastPlayTime = Time.time;
		}

		public Item(GameObject go) {
			gameObject = go;
			transform = go.transform;

			mAnimator = go.GetComponent<M8.Animator.Animate>();
			if(mAnimator)
				mAnimator.takeCompleteCallback += OnAnimatorTakeEnd;

			gameObject.SetActive(false);
		}

		void OnAnimatorTakeEnd(M8.Animator.Animate anim, M8.Animator.Take take) {
			gameObject.SetActive(false);
		}

	}

	[Header("Template")]
	public GameObject template;
	public Transform templateRoot;
	public int templateCapacity = 7;

	[Header("Data")]
	public UnitData[] unitTypes;
	public float unitCheckWidth;

	private M8.CacheList<Item> mActives;
	private M8.CacheList<Item> mCaches;

	private Coroutine mRout;

	protected override void Action(Vector2 point) {
		if(mCaches.Count > 0) {
			var itm = mCaches.RemoveLast();

			//check area within point for unit types and damage it
			var levelBounds = ColonyController.instance.bounds;
			var checkRect = new Rect(new Vector2(point.x - unitCheckWidth * 0.5f, levelBounds.min.y), new Vector2(unitCheckWidth, levelBounds.size.y));

			var unitCtrl = ColonyController.instance.unitController;
			for(int i = 0; i < unitTypes.Length; i++) {
				var units = unitCtrl.GetUnitActivesByData(unitTypes[i]);
				for(int j = 0; j < units.Count; j++) {
					var unit = units[j];

					if(unit && unit.isDamageable && unit.IsTouching(checkRect))
						unit.hitpointsCurrent--;
				}
			}

			//display drop-off
			itm.position = point;
			itm.Play();

			mActives.Add(itm);

			if(mRout == null)
				mRout = StartCoroutine(DoCleanup());
		}
	}

	protected override void OnDisable() {
		//
		if(mRout != null) {
			StopCoroutine(mRout);
			mRout = null;
		}

		for(int i = 0; i < mActives.Count; i++) {
			var itm = mActives[i];
			itm.Stop();
			mCaches.Add(itm);
		}

		mActives.Clear();
		//

		base.OnDisable();
	}

	protected override void Awake() {
		base.Awake();

		mActives = new M8.CacheList<Item>(templateCapacity);
		mCaches = new M8.CacheList<Item>(templateCapacity);

		if(template) {
			for(int i = 0; i < templateCapacity; i++) {
				var newGO = Instantiate(template, templateRoot);

				var newItm = new Item(newGO);

				mCaches.Add(newItm);
			}

			template.SetActive(false);
		}
	}

	IEnumerator DoCleanup() {
		while(mActives.Count > 0) {
			yield return null;

			var activeItm = mActives[0];
			if(!activeItm.isPlaying) {
				if(activeItm.gameObject)
					activeItm.gameObject.SetActive(false); //in case there's no animator

				mActives.Remove(activeItm);
				mCaches.Add(activeItm);
			}
		}

		mRout = null;
	}
}
