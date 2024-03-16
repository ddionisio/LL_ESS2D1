using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class ColonySequence02Revised : ColonySequenceBase {
	[Header("Enemy Data")]
	public UnitData debrisData;

	[Header("Illustrations")]
	public AnimatorEnterExit windDefenseIllustrate;

	[Header("Intro Dialog")]
	public ModalDialogFlowIncremental dlgIntro;

	[Header("Wind Defense Dialog")]
	public ModalDialogFlowIncremental dlgWindDefenseIntro;
	public ModalDialogFlowIncremental dlgWindDefense;

	[Header("Storm Dialog")]
	public ModalDialogFlowIncremental dlgStormIntro;
	public ModalDialogFlowIncremental dlgStormExplain;
	public ModalDialogFlowIncremental dlgStormPower;
	public ModalDialogFlowIncremental dlgStormEnd;

	[Header("Storm Move")]
    public float stormCameraMoveDelay = 0.5f;
    public DG.Tweening.Ease stormCameraEase = DG.Tweening.Ease.OutSine;

    [Header("Storm Stuff")]
    public Transform stormRoot;
    public ParticleSystem stormLowPressureFX;
    public ParticleSystem[] stormWarmAirFXs;
	public ParticleSystem[] stormMoistureFXs;

	private bool mIsDebrisSpawned;
	private bool mIsHazzardHappened;

	public override void Deinit() {
		GameData.instance.signalUnitSpawned.callback -= OnUnitSpawned;
	}

	public override void Init() {
		GameData.instance.signalUnitSpawned.callback += OnUnitSpawned;

		stormRoot.gameObject.SetActive(false);
	}

	public override IEnumerator Intro() {
		//Debug.Log("Dialog about tropical climate.");
		yield return dlgIntro.Play();
	}

	public override void CycleNext() {
		if(ColonyController.instance.cycleController.cycleCurWeather.isHazzard) {
			if(!mIsHazzardHappened) {
				mIsHazzardHappened = true;

				//Debug.Log("Dialog about hazzard.");
				StartCoroutine(DoStormStuff());
			}
		}
	}

	void OnUnitSpawned(Unit unit) {
		if(unit.data == debrisData) {
			if(!mIsDebrisSpawned) {
				mIsDebrisSpawned = true;
				StartCoroutine(DoDebrisStuff(unit));
			}
		}
	}

	IEnumerator DoDebrisStuff(Unit unit) {
		yield return new WaitForSeconds(0.5f);

		M8.SceneManager.instance.Pause();

		yield return dlgWindDefenseIntro.Play();

		windDefenseIllustrate.Show();
		yield return windDefenseIllustrate.PlayEnterWait();

		yield return dlgWindDefense.Play();

		yield return windDefenseIllustrate.PlayExitWait();
		windDefenseIllustrate.Hide();

		M8.SceneManager.instance.Resume();
	}

	IEnumerator DoStormStuff() {
		ColonyHUD.instance.mainRootGO.SetActive(false);

		isPauseCycle = true;

		stormRoot.gameObject.SetActive(true);

		var camTrans = ColonyController.instance.mainCameraTransform;

		Vector2 camOrigin = camTrans.position;
		Vector2 stormOrigin = stormRoot.position;

		var easeFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(stormCameraEase);

		//move to storm area
		var moveTime = 0f;
		while(moveTime < stormCameraMoveDelay) {
			yield return null;

			moveTime += Time.deltaTime;

			var t = easeFunc(moveTime, stormCameraMoveDelay, 0f, 0f);

			camTrans.position = Vector2.Lerp(camOrigin, stormOrigin, t);
		}
		//

		stormLowPressureFX.Play();

		yield return dlgStormIntro.Play();

		for(int i = 0; i < stormWarmAirFXs.Length; i++)
			stormWarmAirFXs[i].Play();

		yield return dlgStormExplain.Play();

		for(int i = 0; i < stormMoistureFXs.Length; i++)
			stormMoistureFXs[i].Play();

		yield return dlgStormPower.Play();

		yield return new WaitForSeconds(1f);

		//move camera back
		moveTime = 0f;
		while(moveTime < stormCameraMoveDelay) {
			yield return null;

			moveTime += Time.deltaTime;

			var t = easeFunc(moveTime, stormCameraMoveDelay, 0f, 0f);

			camTrans.position = Vector2.Lerp(stormOrigin, camOrigin, t);
		}
		//

		stormRoot.gameObject.SetActive(false);

		yield return dlgStormEnd.Play();

		ColonyHUD.instance.mainRootGO.SetActive(true);

		isPauseCycle = false;
	}
}
