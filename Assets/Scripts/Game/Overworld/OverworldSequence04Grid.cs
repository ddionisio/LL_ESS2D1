using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class OverworldSequence04Grid : OverworldSequenceBase {

	[Header("Dialogs")]
	public ModalDialogFlowIncremental dlgIntro;

	public override IEnumerator StartBegin() {

		yield return dlgIntro.Play();
	}
}
