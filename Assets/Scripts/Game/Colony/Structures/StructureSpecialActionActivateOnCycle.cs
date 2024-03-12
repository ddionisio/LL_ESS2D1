using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureSpecialActionActivateOnCycle : CycleControlBase {
	public M8.RangeInt cycleIndexRange;
	public M8.SignalBoolean signalInvokeActivate;

	protected override void Begin() {
		int curCycleInd = ColonyController.instance.cycleController.cycleCurIndex;
		if(signalInvokeActivate)
			signalInvokeActivate.Invoke(curCycleInd >= cycleIndexRange.min && curCycleInd <= cycleIndexRange.max);
	}

	protected override void Next() {
		int curCycleInd = ColonyController.instance.cycleController.cycleCurIndex;
		if(signalInvokeActivate)
			signalInvokeActivate.Invoke(curCycleInd >= cycleIndexRange.min && curCycleInd <= cycleIndexRange.max);
	}
}
