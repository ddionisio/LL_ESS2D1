using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StructureSpecialAction : MonoBehaviour {
	[Header("Unit Spawn Types")]
	public UnitData[] spawnTypes; //if they are spawned, keep this structure 'active'

    [Header("Signal Listen")]
    public M8.SignalBoolean signalListenActivate;
	public bool isActiveDefault;

    public Structure structure { get; private set; }

	public bool isActive { get { return mIsActive && (spawnTypes == null || spawnTypes.Length == 0 || mUnitMatchSpawnCount > 0); } }

	private bool mActivateOnStateActive = false;

	private bool mIsActive;
	private int mUnitMatchSpawnCount;

	public void Activate(bool active) {
		if(mIsActive != active) {
			mIsActive = active;

			RefreshActive();
		}
	}

	protected abstract void ApplyActivate(bool active);

	protected virtual void OnDisable() {
		if(signalListenActivate)
			signalListenActivate.callback -= Activate;

		if(spawnTypes != null && spawnTypes.Length > 0) {
			if(GameData.instance.signalUnitSpawned)
				GameData.instance.signalUnitSpawned.callback -= OnUnitSpawned;
			if(GameData.instance.signalUnitDespawned)
				GameData.instance.signalUnitDespawned.callback -= OnUnitDespawned;
		}

		mIsActive = false;
		mActivateOnStateActive = false;
	}

	protected virtual void OnEnable() {
		if(signalListenActivate)
			signalListenActivate.callback += Activate;

		if(spawnTypes != null && spawnTypes.Length > 0) {
			if(GameData.instance.signalUnitSpawned)
				GameData.instance.signalUnitSpawned.callback += OnUnitSpawned;
			if(GameData.instance.signalUnitDespawned)
				GameData.instance.signalUnitDespawned.callback += OnUnitDespawned;

			//determine if there are units spawned
			RefreshEntityCount();
		}

		mIsActive = isActiveDefault;

		RefreshActive();
	}

	protected virtual void OnDestroy() {
		if(structure)
			structure.stateChangedCallback -= OnStateChange;
	}

	protected virtual void Awake() {
		structure = GetComponent<Structure>();
		if(structure)
			structure.stateChangedCallback += OnStateChange;
	}

	void OnUnitSpawned(Unit unit) {
		RefreshEntityCount();
		if(mIsActive)
			RefreshActive();
	}

	void OnUnitDespawned(Unit unit) {
		RefreshEntityCount();
		if(mIsActive)
			RefreshActive();
	}

	void OnStateChange(StructureState state) {
		switch(state) {
			case StructureState.Active:
				if(isActive && mActivateOnStateActive) {
					mActivateOnStateActive = false;
					ApplyActivate(true);
				}
				break;

			case StructureState.Moving:
			case StructureState.Destroyed:
				//activate later when structure is fixed/move finish
				if(isActive) {
					mActivateOnStateActive = true;
					ApplyActivate(false);
				}
				break;
			
			case StructureState.Victory:
			case StructureState.Demolish:
				if(isActive) {
					mIsActive = false;
					ApplyActivate(false);
				}
				break;
		}
    }

	private void RefreshEntityCount() {
		var unitCtrl = ColonyController.instance.unitController;

		mUnitMatchSpawnCount = 0;

		for(int i = 0; i < spawnTypes.Length; i++) {
			var units = unitCtrl.GetUnitActivesByData(spawnTypes[i]);
			if(units != null)
				mUnitMatchSpawnCount += units.Count;
		}
	}

	private void RefreshActive() {
		//make sure building is in proper state
		if(isActive) {
			if(structure.state == StructureState.Active || (structure.state == StructureState.Damage && structure.hitpointsCurrent > 0)) {
				mActivateOnStateActive = false;
				ApplyActivate(true);
			}
			else
				mActivateOnStateActive = true;
		}
		else {
			ApplyActivate(false);
		}
	}
}
