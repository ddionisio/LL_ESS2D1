using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleUnitSpawnerRigidbody : CycleUnitSpawnerBase {
	[SerializeField]
	M8.RangeFloat _forceRange;
	[SerializeField]
	M8.RangeFloat _angleVelocityRange;
	[SerializeField]
	M8.RangeFloat _dirAngleRange; //relative to up vector

	[SerializeField]
	float _spawnRadius;

	protected override void ApplySpawnParams(M8.GenericParams parms) {
		var pt = ((Vector2)transform.position) + (Random.insideUnitCircle * _spawnRadius);
		var force = M8.MathUtil.RotateAngle(Vector2.up, _dirAngleRange.random) * _forceRange.random;
		var avel = _angleVelocityRange.random;

		parms[UnitSpawnParams.spawnPoint] = pt;
		parms[UnitSpawnParams.force] = force;
		parms[UnitSpawnParams.angleVelocity] = avel;
	}

	protected override void Init() {
		base.Init();
	}

	void OnDrawGizmos() {
		Vector2 pt = transform.position;

		if(_spawnRadius > 0f) {
			Gizmos.color = Color.white;
			Gizmos.DrawWireSphere(pt, _spawnRadius);
		}

		Gizmos.color = Color.yellow;
		M8.Gizmo.ArrowLine2D(pt, pt + M8.MathUtil.RotateAngle(Vector2.up, _dirAngleRange.min));

		Gizmos.color = Color.yellow * 0.5f;
		M8.Gizmo.ArrowLine2D(pt, pt + M8.MathUtil.RotateAngle(Vector2.up, _dirAngleRange.max));
	}
}
