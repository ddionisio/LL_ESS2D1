using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitDebris : Unit {
	[Header("Debris Data")]
	public float explodeRadius;

	[Header("Debris Display")]
	public GameObject damagedGO;

	public override Vector2 position {
		get { 
			return body2D.simulated ? body2D.position : base.position;
		}
		set {
			if(body2D.simulated)
				body2D.position = value;
			else
				base.position = value;
		}
	}

	public Rigidbody2D body2D { get; private set; }

	private const int structureCheckCapacity = 4;
	private Collider2D[] mStructureColls = new Collider2D[structureCheckCapacity];

	private bool mInitialForceIsApplied;
	private Vector2 mInitialForce;
	private float mInitialAngleVel;

	protected override void HitpointsChanged(int previousHitpoints) {
		base.HitpointsChanged(previousHitpoints);

		//check if we want damaged activated
		if(damagedGO)
			damagedGO.SetActive(hitpointsCurrent > 0 && hitpointsCurrent < hitpointsMax);
	}

	protected override void ApplyTelemetryState(bool canMove, bool physicsActive) {
		base.ApplyTelemetryState(canMove, physicsActive);

		if(state != UnitState.Hurt) //not exactly elegant, but there it is...
			body2D.simulated = physicsActive;
	}

	protected override void Spawned(M8.GenericParams parms) {
		if(damagedGO)
			damagedGO.SetActive(false);

		body2D.simulated = false;
		body2D.angularVelocity = 0f;
		body2D.velocity = Vector2.zero;

		mInitialForceIsApplied = false;

		if(parms.ContainsKey(UnitSpawnParams.force))
			mInitialForce = parms.GetValue<Vector2>(UnitSpawnParams.force);
		else
			mInitialForce = Vector2.zero;

		if(parms.ContainsKey(UnitSpawnParams.angleVelocity))
			mInitialAngleVel = parms.GetValue<float>(UnitSpawnParams.angleVelocity);
		else
			mInitialAngleVel = 0f;
	}

	protected override void ApplyCurrentState() {
		base.ApplyCurrentState();

		//apply initial force
		if(!mInitialForceIsApplied && body2D.simulated) {
			body2D.AddForce(mInitialForce, ForceMode2D.Impulse);
			body2D.angularVelocity = mInitialAngleVel;

			mInitialForceIsApplied = true;
		}
	}

	protected override void Init() {
		body2D = GetComponent<Rigidbody2D>();
		body2D.simulated = false;
	}

	protected override void Update() {
		base.Update();
	}

	void OnCollisionEnter2D(Collision2D collision) {

		//check if we hit ground, damage a bit
		if(state == UnitState.Idle || state == UnitState.Move) {
			//damage buildings nearby
			var checkCount = Physics2D.OverlapCircleNonAlloc(position, explodeRadius, mStructureColls, GameData.instance.structureLayerMask);
			for(int i = 0; i < checkCount; i++) {
				var coll = mStructureColls[i];
				var structure = coll.GetComponent<Structure>();
				if(structure && structure.isDamageable)
					structure.hitpointsCurrent--;

			}

			if(ColonyController.instance.cameraShake)
				ColonyController.instance.cameraShake.Shake();

			hitpointsCurrent--;
		}
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.red;

		if(explodeRadius > 0f)
			Gizmos.DrawWireSphere(transform.position, explodeRadius);
	}
}
