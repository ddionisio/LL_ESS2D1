using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Actual geometric data used by LandscapeGridDisplay
/// </summary>
public class LandscapeGrid : MonoBehaviour {
	[Header("Data")]
	public float altitude; //base altitude

	[Header("Display")]
	public GameObject terrainRootGO;
	public GameObject invalidRootGO;

	public bool invalidActive { get { return invalidRootGO ? invalidRootGO.activeSelf : false; } set { if(invalidRootGO) invalidRootGO.SetActive(value); } }

	private Dictionary<Collider2D, LandscapeGridTerrain> mTerrainColliderLookup;

	public void GenerateTerrainColliderLookup() {
		if(mTerrainColliderLookup == null)
			mTerrainColliderLookup = new Dictionary<Collider2D, LandscapeGridTerrain>();
		else
			mTerrainColliderLookup.Clear();

		if(!terrainRootGO)
			return;

		var gridTerrains = terrainRootGO.GetComponentsInChildren<LandscapeGridTerrain>(true);
		for(int i = 0; i < gridTerrains.Length; i++) {
			var gridTerrain = gridTerrains[i];

			var coll = gridTerrain.coll;
			if(!coll)
				continue;

			mTerrainColliderLookup.Add(coll, gridTerrain);
		}
	}

	public LandscapeGridTerrain GetTerrain(Collider2D collider) {
		if(mTerrainColliderLookup == null)
			GenerateTerrainColliderLookup();

		LandscapeGridTerrain ret;
		mTerrainColliderLookup.TryGetValue(collider, out ret);

		return ret;
	}
		
	void OnDrawGizmos() {
		Gizmos.color = Color.yellow;

		var landscapeSize = GameData.instance.landscapePreviewSize;

		Gizmos.DrawWireCube(transform.position, new Vector3(landscapeSize.x, landscapeSize.y, 0f));
	}
}
