using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StructureSpecialAction : MonoBehaviour {
    [Header("Signal Listen")]
    public M8.SignalBoolean signalListenActivate;

    public Structure structure { get; private set; }

	public bool isActive { get; private set; } = false;

	private bool mActivateOnStateActive = false;

	public void Activate(bool active) {
		if(isActive != active) {
			isActive = active;

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

	protected abstract void ApplyActivate(bool active);

	protected virtual void OnDisable() {
		if(signalListenActivate)
			signalListenActivate.callback -= Activate;

		isActive = false;
		mActivateOnStateActive = false;
	}

	protected virtual void OnEnable() {
		if(signalListenActivate)
			signalListenActivate.callback += Activate;
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
					ApplyActivate(false);
					isActive = false;
				}
				break;
		}
    }
}
