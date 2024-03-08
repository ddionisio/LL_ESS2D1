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
	public float shipMoveDelay = 0.3f;

	[Header("SFX")]
	[M8.SoundPlaylist]
	public string sfxClickValid;
	[M8.SoundPlaylist]
	public string sfxClickInvalid;

	[Header("Signal Listen")]
	public SignalSeasonData signalListenSeasonChange;

	public bool active {
		get { return gameObject.activeSelf; }
		set { gameObject.SetActive(value); }
	}

	public HotspotData hotspotData { get; private set; }

	public LandscapeGrid grid { get; private set; }

	public bool shipActive { 
		get { return shipRoot ? shipRoot.gameObject.activeSelf : false; } 
		set { if(shipRoot) shipRoot.gameObject.SetActive(value); } 
	}

	public Vector2 shipPosition {
		get { return mShipPosition; }
		set {
			if(mShipPosition != value) {
				mShipPosition = value;

				if(shipRoot) {
					if(mShipMoveRout == null)
						mShipMoveRout = StartCoroutine(DoShipMove());
				}
			}
		}
	}

	public GridData.AtmosphereMod atmosphereMod { get { return mAtmosphereMod; } }
	public float altitude { get { return mAltitude; } }

	public event System.Action<LandscapeGridController> clickCallback;

	private const int gridCollCapacity = 10;

	private Collider2D[] mGridColls = new Collider2D[gridCollCapacity];

	private Dictionary<HotspotData, LandscapeGrid> mHotspotGrids = new Dictionary<HotspotData, LandscapeGrid>();
	private Dictionary<GridData.TopographyType, int> mShipTopographyCounts = new Dictionary<GridData.TopographyType, int>();

	private PointerEventData mPointerEvent;
	private GridData.AtmosphereMod mAtmosphereMod;
	private float mAltitude;

	private Coroutine mShipMoveRout;
	private Vector2 mShipPosition;

	/// <summary>
	/// Call after ship has been placed to determine which topographies are nearby
	/// </summary>
	public int GetShipTopographies(GridData.TopographyType[] output) {
		int curInd = 0;

		foreach(var pair in mShipTopographyCounts) {
			output[curInd] = pair.Key;

			curInd++;
			if(curInd == output.Length)
				break;
		}

		return curInd;
	}

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

	void OnDisable() {
		if(signalListenSeasonChange) signalListenSeasonChange.callback -= SetSeason;

		if(mShipMoveRout != null) {
			StopCoroutine(mShipMoveRout);
			mShipMoveRout = null;
		}
	}

	void OnEnable() {
		if(signalListenSeasonChange) signalListenSeasonChange.callback += SetSeason;
	}

	void Update() {
		if(mPointerEvent != null) {
			UpdateCursor(mPointerEvent);
		}
	}

	IEnumerator DoShipMove() {
		Vector2 shipVel = Vector2.zero;
		Vector2 shipPos = (Vector2)shipRoot.position;

		while(shipPos != mShipPosition) {
			shipPos = Vector2.SmoothDamp(shipPos, mShipPosition, ref shipVel, shipMoveDelay);
			shipRoot.position = shipPos;

			yield return null;
		}

		mShipMoveRout = null;
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
		if(hit.isValid) {
			if(cursor.isValid) {
				Vector2 pos = hit.worldPosition;

				//update ship position
				if(shipActive)
					shipPosition = pos;
				else {
					shipActive = true;

					//initial position
					shipRoot.position = pos;
					mShipPosition = pos;					
				}				

				//update atmosphere mod and altitude
				mAtmosphereMod = new GridData.AtmosphereMod();
				mAltitude = grid.altitude;

				mShipTopographyCounts.Clear();

				//grab surrounding terrain
				var count = Physics2D.OverlapCircleNonAlloc(pos, cursor.radiusCheck, mGridColls, GridData.instance.gridLayerMask);
				for(int i = 0; i < count; i++) {
					var terrain = grid.GetTerrain(mGridColls[i]);
					if(terrain != null) {
						mAtmosphereMod += terrain.mod;

						//count topography feature
						var topography = terrain.topography;
						if(topography != GridData.TopographyType.None) {
							if(mShipTopographyCounts.ContainsKey(topography))
								mShipTopographyCounts[topography]++;
							else
								mShipTopographyCounts.Add(topography, 1);
						}
					}
				}

				//get altitude, and also apply its modifier
				var zMin = 100000f;
				var altitudeAtmosphereMod = new GridData.AtmosphereMod();
				var altitudeMod = 0f;

				count = Physics2D.OverlapPointNonAlloc(pos, mGridColls, GridData.instance.gridLayerMask);
				for(int i = 0; i < count; i++) {
					var terrain = grid.GetTerrain(mGridColls[i]);
					if(terrain != null && terrain.isTerrain) {
						var z = terrain.transform.position.z;
						if(z < zMin) {
							zMin = z;
							altitudeAtmosphereMod = terrain.altitudeMod;
							altitudeMod = terrain.altitude;
						}
					}
				}

				mAtmosphereMod += altitudeAtmosphereMod;
				mAltitude += altitudeMod;
				//

				cursor.PlayPlacement();

				if(!string.IsNullOrEmpty(sfxClickValid))
					M8.SoundPlaylist.instance.Play(sfxClickValid, false);

				clickCallback?.Invoke(this);
			}
			else {
				cursor.PlayInvalid();

				if(!string.IsNullOrEmpty(sfxClickInvalid))
					M8.SoundPlaylist.instance.Play(sfxClickInvalid, false);
			}
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
}
