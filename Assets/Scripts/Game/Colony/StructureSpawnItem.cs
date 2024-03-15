using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureSpawnItem : MonoBehaviour {
    public StructureData structureData;
    public float spawnDelay;

	public bool isSpawning { get { return mSpawnRout != null; } }

    private Coroutine mSpawnRout;

	public void Spawn() {
		if(mSpawnRout == null)
			mSpawnRout = StartCoroutine(DoSpawn());
	}

	public void CancelSpawn() {
		if(mSpawnRout != null) {
			StopCoroutine(mSpawnRout);
			mSpawnRout = null;
		}
	}

	void OnDisable() {
		CancelSpawn();
	}

	IEnumerator DoSpawn() {
		while(ColonyController.instance.timeState == ColonyController.TimeState.CyclePause)
			yield return null;

		yield return new WaitForSeconds(spawnDelay);

		GroundPoint grdPt;
		if(GroundPoint.GetGroundPoint(transform.position.x, out grdPt)) {
			var structure = ColonyController.instance.structurePaletteController.Spawn(structureData, grdPt.position, grdPt.up);

			yield return null;

			while(structure.state == StructureState.Spawning)
				yield return null;
		}

		mSpawnRout = null;
    }
}
