using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStructureDOT : UnitTargetStructure {
	[Header("FX")]
	public ParticleSystem fx;
	public M8.RangeFloat fxSizeRange; //change based on current hp
	public M8.RangeInt fxEmissionRange; //change based on current hp

	private Coroutine mDamageRout;

	protected override void TargetChanged() {
		ApplyTargetDimension();
	}

	protected override void ApplyCurrentState() {
		base.ApplyCurrentState();

		switch(state) {
			case UnitState.Spawning:
				if(fx)
					fx.Play();
				break;

			case UnitState.Idle:
				mRout = StartCoroutine(DoIdle());

				if(mDamageRout == null)
					mDamageRout = StartCoroutine(DoDamage());
				break;

			case UnitState.Death:
			case UnitState.Dying:
			case UnitState.Despawning:
			case UnitState.None:
				if(fx)
					fx.Stop();

				if(mDamageRout != null) {
					StopCoroutine(mDamageRout);
					mDamageRout = null;
				}
				break;
		}
	}

	protected override void UpdateAI() {
		switch(state) {
			case UnitState.Idle:
			case UnitState.Act:
				//check if target is still viable
				if(!targetStructure || targetStructure.state == StructureState.None || targetStructure.state == StructureState.Moving || (targetStructure.state == StructureState.Destroyed && !targetStructure.isReparable)) {
					targetStructure = null;

					hitpointsCurrent = 0; //destroy self
				}
				break;
		}
	}

	protected override void Spawned(M8.GenericParams parms) {
		base.Spawned(parms);

		ApplyTargetDimension();
		RefreshFX();
	}

	protected override void HitpointsChanged(int previousHitpoints) {
		RefreshFX();

		base.HitpointsChanged(previousHitpoints);
	}

	IEnumerator DoIdle() {
		var dotData = data as UnitStructureDOTData;
		if(!dotData) {
			mRout = null;
			yield break;
		}

		while(hitpointsCurrent > 0) {
			if(hitpointsCurrent < hitpointsMax) {
				RestartStateTime();
				while(stateTimeElapsed < dotData.growthDelay)
					yield return null;

				hitpointsCurrent++;
			}

			yield return null;
		}

		mRout = null;
	}

	IEnumerator DoDamage() {
		var dotData = data as UnitStructureDOTData;
		if(!dotData) {
			mDamageRout = null;
			yield break;
		}

		var wait = new WaitForSeconds(dotData.attackDelay);

		while(targetStructure && targetStructure.hitpointsCurrent > 0) {
			yield return wait;

			//damage target
			if(targetStructure.isDamageable)
				targetStructure.hitpointsCurrent--;
		}

		//hitpointsCurrent = 0; //destroy self

		mDamageRout = null;
	}

	private void RefreshFX() {
		if(!fx)
			return;

		var t = Mathf.Clamp01(((float)hitpointsCurrent) / hitpointsMax);

		var main = fx.main;

		main.startSize = fxSizeRange.Lerp(t);

		var emission = fx.emission;

		emission.rateOverTime = Mathf.RoundToInt(fxEmissionRange.Lerp(t));
	}

	private void ApplyTargetDimension() {
		if(!targetStructure)
			return;

		//set collision size
		var width = targetStructure.boxCollider.size.x;

		if(boxCollider) {
			var size = boxCollider.size;
			size.x = width;
			boxCollider.size = size;
		}

		//set particle emission area
		if(fx) {
			var shape = fx.shape;

			var shapeScale = shape.scale;
			shapeScale.x = width;
			shape.scale = shapeScale;
		}
	}
}