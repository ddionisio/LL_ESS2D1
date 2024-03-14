using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleUnitSpawnerRigidbodyWaypoint : CycleUnitSpawnerBase {
	[System.Serializable]
	public struct Waypoint {
		public Vector2 point;
		public M8.RangeFloat dirAngleRange;

		public Vector2 dir { get { return M8.MathUtil.RotateAngle(Vector2.up, dirAngleRange.random); } }
	}

	[SerializeField]
	M8.RangeFloat _forceRange;
	[SerializeField]
	M8.RangeFloat _angleVelocityRange;

	[SerializeField]
	Waypoint[] _waypoints;

	private int mCurWpInd;

	protected override void ApplySpawnParams(M8.GenericParams parms) {
		var wp = _waypoints[mCurWpInd];
				
		var pt = ((Vector2)transform.position) + wp.point;
		var force = wp.dir * _forceRange.random;
		var avel = _angleVelocityRange.random;

		parms[UnitSpawnParams.spawnPoint] = pt;
		parms[UnitSpawnParams.force] = force;
		parms[UnitSpawnParams.angleVelocity] = avel;

		mCurWpInd++;
		if(mCurWpInd == _waypoints.Length) {
			M8.ArrayUtil.Shuffle(_waypoints);
			mCurWpInd = 0;
		}
	}

	protected override void Init() {
		base.Init();

		M8.ArrayUtil.Shuffle(_waypoints);

		mCurWpInd = 0;
	}

	void OnDrawGizmos() {
		Vector2 pt = transform.position;

		if(_waypoints != null) {
			for(int i = 0; i < _waypoints.Length; i++) {
				var wp = _waypoints[i];

				var wpt = pt + wp.point;

				Gizmos.color = Color.green;
				Gizmos.DrawSphere(wpt, 0.1f);

				Gizmos.color = Color.yellow;
				M8.Gizmo.ArrowLine2D(wpt, wpt + M8.MathUtil.RotateAngle(Vector2.up, wp.dirAngleRange.min));

				Gizmos.color = Color.yellow * 0.5f;
				M8.Gizmo.ArrowLine2D(wpt, wpt + M8.MathUtil.RotateAngle(Vector2.up, wp.dirAngleRange.max));
			}
		}
	}
}
