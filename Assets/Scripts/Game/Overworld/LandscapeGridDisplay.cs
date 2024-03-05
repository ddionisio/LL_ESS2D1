using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandscapeGridDisplay : MonoBehaviour {
	[Header("View")]
	public Transform root;

	[Header("Signal Listen")]
	public SignalSeasonData signalListenSeasonChange;

	public bool active {
		get { return gameObject.activeSelf; }
		set { gameObject.SetActive(value); }
	}

	public HotspotData hotspotData { get; private set; }

	public LandscapeGrid grid { get; private set; }

	private Dictionary<HotspotData, LandscapeGrid> mHotspotGrids = new Dictionary<HotspotData, LandscapeGrid>();

	public void DestroyHotspotGrids() {
		foreach(var pair in mHotspotGrids) {
			if(pair.Value)
				Destroy(pair.Value);
		}

		mHotspotGrids.Clear();
	}

	public void AddHotspotPreview(HotspotData aHotspotData) {
		if(mHotspotGrids.ContainsKey(aHotspotData)) //already added
			return;

		if(!aHotspotData.landscapeGridPrefab) {
			//Debug.LogWarning("No landscape prefab found for: " + aHotspotData.name);
			return;
		}

		var newGrid = Instantiate(aHotspotData.landscapeGridPrefab);

		var landscapeTrans = newGrid.transform;
		landscapeTrans.SetParent(root, false);

		newGrid.gameObject.SetActive(false);

		mHotspotGrids.Add(aHotspotData, newGrid);
	}

	public void SetCurrentPreview(HotspotData aHotspotData) {
		if(grid) {
			grid.gameObject.SetActive(false);
		}

		LandscapeGrid newLandscapeGrid;
		mHotspotGrids.TryGetValue(aHotspotData, out newLandscapeGrid);

		if(newLandscapeGrid) {
			hotspotData = aHotspotData;

			grid = newLandscapeGrid;

			var landscapeTrans = grid.transform;

			landscapeTrans.localPosition = Vector3.zero;
			landscapeTrans.localRotation = Quaternion.identity;
			landscapeTrans.localScale = Vector3.one;

			grid.gameObject.SetActive(true);

			//reset land placement			
			
		}
	}

	public void SetSeason(SeasonData seasonData) {

	}

	void OnDisable() {
		if(signalListenSeasonChange) signalListenSeasonChange.callback -= SetSeason;
	}

	void OnEnable() {
		if(signalListenSeasonChange) signalListenSeasonChange.callback += SetSeason;
	}
}
