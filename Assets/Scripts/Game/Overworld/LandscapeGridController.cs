using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LandscapeGridController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
	[Header("View")]
	public Transform root;
	
	[Header("Cursor")]
	public LandscapeGridCursor cursor;

	[Header("Ship")]
	public Transform shipRoot;

	[Header("Signal Listen")]
	public SignalSeasonData signalListenSeasonChange;

	public bool active {
		get { return gameObject.activeSelf; }
		set { gameObject.SetActive(value); }
	}

	public HotspotData hotspotData { get; private set; }

	public LandscapeGrid grid { get; private set; }

	public bool shipActive { 
		get { return shipRoot ? shipRoot.gameObject : null; } 
		set { if(shipRoot) shipRoot.gameObject.SetActive(value); } 
	}

	public Vector2 shipPosition {
		get { return shipRoot ? shipRoot.position : Vector2.zero; }
		set { if(shipRoot) shipRoot.position = value; }
	}

	public GridData.AtmosphereMod atmosphereMod { get { return mAtmosphereMod; } }
	public float altitude { get { return mAltitude; } }

	public event System.Action<LandscapeGridController> clickCallback;

	private const int gridCollCapacity = 10;

	private Collider2D[] mGridColls = new Collider2D[gridCollCapacity];

	private Dictionary<HotspotData, LandscapeGrid> mHotspotGrids = new Dictionary<HotspotData, LandscapeGrid>();

	private PointerEventData mPointerEvent;
	private GridData.AtmosphereMod mAtmosphereMod;
	private float mAltitude;

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

		newGrid.GenerateTerrainColliderLookup();

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

			grid.invalidActive = false;

			var landscapeTrans = grid.transform;

			landscapeTrans.localPosition = Vector3.zero;
			landscapeTrans.localRotation = Quaternion.identity;
			landscapeTrans.localScale = Vector3.one;

			grid.gameObject.SetActive(true);

			//reset land placement			
			shipActive = false;

			//reset atmosphere mod and altitude
			mAtmosphereMod = new GridData.AtmosphereMod();
			mAltitude = grid.altitude;
		}
	}

	public void SetSeason(SeasonData seasonData) {

	}

	void Update() {
		if(mPointerEvent != null) {
			UpdateCursor(mPointerEvent);
		}
	}

	void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
		mPointerEvent = eventData;

		grid.invalidActive = true;

		cursor.active = true;
	}

	void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
		mPointerEvent = null;

		grid.invalidActive = false;

		cursor.active = false;
	}

	void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
		UpdateCursor(eventData);

		var hit = eventData.pointerCurrentRaycast;
		if(cursor.isValid && hit.isValid) {
			Vector2 pos = hit.worldPosition;

			//update ship position
			shipActive = true;
			shipPosition = pos;

			//update atmosphere mod and altitude
			mAtmosphereMod = new GridData.AtmosphereMod();
			mAltitude = grid.altitude;

			//grab surrounding terrain
			var count = Physics2D.OverlapCircleNonAlloc(pos, cursor.radiusCheck, mGridColls, GridData.instance.gridLayerMask);
			for(int i = 0; i < count; i++) {
				var terrain = grid.GetTerrain(mGridColls[i]);
				if(terrain != null) {
					mAtmosphereMod += terrain.mod;
				}
			}

			//get altitude, and also apply its modifier
			count = Physics2D.OverlapPointNonAlloc(pos, mGridColls, GridData.instance.gridLayerMask);
			for(int i = 0; i < count; i++) {
				var terrain = grid.GetTerrain(mGridColls[i]);
				if(terrain != null && terrain.isTerrain) {
					mAtmosphereMod += terrain.altitudeMod;
					mAltitude += terrain.altitude;
					break;
				}
			}

			clickCallback?.Invoke(this);
		}
	}

	private void UpdateCursor(PointerEventData eventData) {
		var hit = eventData.pointerCurrentRaycast;
		if(hit.isValid) {
			cursor.active = true;

			Vector2 pos = hit.worldPosition;

			cursor.position = pos;

			//check if cursor is on invalid collision
			var count = Physics2D.OverlapCircleNonAlloc(pos, cursor.radiusLanding, mGridColls, GridData.instance.gridInvalidLayerMask);

			cursor.isValid = count == 0;
		}
		else {
			cursor.active = false;
		}
	}

	void OnDisable() {
		if(signalListenSeasonChange) signalListenSeasonChange.callback -= SetSeason;
	}

	void OnEnable() {
		if(signalListenSeasonChange) signalListenSeasonChange.callback += SetSeason;
	}
}
