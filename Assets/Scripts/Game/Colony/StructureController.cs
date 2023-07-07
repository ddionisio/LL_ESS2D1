using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

using UnityEngine.Events;
using UnityEngine.EventSystems;

public class StructureController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler  {
    public const string poolGroup = "structure";

    public class GhostItem {
        public StructureData structure { get; private set; }
        public StructureGhost ghost { get; private set; }
        
        public bool active { get { return mGO.activeSelf; } set { mGO.SetActive(value); } }

        private GameObject mGO;

        public GhostItem(StructureData data, Transform root) {
            structure = data;

            mGO = Instantiate(structure.ghostPrefab, root);
            mGO.SetActive(false);

            ghost = mGO.GetComponent<StructureGhost>();

            var trans = mGO.transform;
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = Vector3.one;
        }
    }

    [Header("Spawn Info")]
    public Transform spawnRoot;

    [Header("Placement Info")]
    public GameBounds2D placementBounds;
    public StructurePlacementCursor placementCursor;
    public Transform placementConfirmRoot;
    public Transform placementGhostRoot; //structure's 'ghost' is placed here and activated during placement

    public StructurePaletteData paletteData { get; private set; }

    public bool isPlacementActive { get { return mPlacementCurStuctureData; } }

    private int[] mGroupSpawnCapacities; //current capacity for each group (use for house to limit capacity based on population)
    private int[] mGroupSpawnCounts; //correlates to paletteData's items

    private GhostItem[] mGhostStructures;

    private M8.CacheList<Structure> mStructureActives;

    private M8.PoolController mPoolCtrl;

    private Collider2D mColl;
        
    private StructureData mPlacementCurStuctureData;
    private int mPlacementCurGroupIndex;

    private GhostItem mPlacementCurGhostItem;

    private PointerEventData mPlacementPointerEvent;
    private bool mPlacementIsDragging;
        
    public void PlacementStart(StructureData structureData) {
        mPlacementCurStuctureData = structureData;
        mPlacementCurGroupIndex = GetGroupIndex(structureData);


        mColl.enabled = true;
    }

    /// <summary>
    /// Should be called via confirm
    /// </summary>
    public void PlacementAccept() {

        PlacementClear();
    }

    /// <summary>
    /// Should be called via confirm (also if HUD has a cancel button)
    /// </summary>
    public void PlacementCancel() {
        PlacementClear();
    }
    
    public int GetGroupIndex(StructureData structureData) {
        return paletteData.GetGroupIndex(structureData);
    }

    public void Setup(StructurePaletteData aPaletteData) {
        if(!mColl) mColl = GetComponent<Collider2D>();

        //initialize pool
        if(!mPoolCtrl) {
            mPoolCtrl = M8.PoolController.CreatePool(poolGroup);

            mPoolCtrl.despawnCallback += OnStructureDespawn;
        }

        paletteData = aPaletteData;

        var groupCount = paletteData.groups.Length;

        mGroupSpawnCapacities = new int[groupCount];
        mGroupSpawnCounts = new int[groupCount];
                
        var ghostStructureList = new List<GhostItem>();

        int totalCapacity = 0;

        //setup pool and ghost content
        for(int i = 0; i < groupCount; i++) {
            var item = paletteData.groups[i];

            var capacity = item.capacity;

            for(int j = 0; j < item.structures.Length; j++) {
                var structure = item.structures[j];

                //add new item in pool
                mPoolCtrl.AddType(structure.spawnPrefab.gameObject, capacity, capacity);

                //setup ghost
                ghostStructureList.Add(new GhostItem(structure, placementGhostRoot));
            }

            mGroupSpawnCapacities[i] = capacity;

            totalCapacity += capacity;
        }

        mStructureActives = new M8.CacheList<Structure>(totalCapacity);

        mGhostStructures = ghostStructureList.ToArray();

        //initialize placement
        mColl.enabled = false;

        placementCursor.active = false;
        placementConfirmRoot.gameObject.SetActive(false);
    }

    void OnDestroy() {
        if(mPoolCtrl) {
            mPoolCtrl.despawnCallback -= OnStructureDespawn;
        }
    }

    void Update() {

    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        if(!isPlacementActive) return;

        mPlacementPointerEvent = eventData;
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        if(!isPlacementActive) return;

        mPlacementPointerEvent = null;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if(!isPlacementActive) return;
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
        if(!isPlacementActive) return;
    }

    void IDragHandler.OnDrag(PointerEventData eventData) {

    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData) {

    }

    void OnStructureDespawn(M8.PoolDataController pdc) {
        Structure structure = null;
        for(int i = 0; i < mStructureActives.Count; i++) {
            var structureActive = mStructureActives[i];
            if(structureActive.poolCtrl == pdc) {
                structure = structureActive;
                mStructureActives.RemoveAt(i);
                break;
            }
        }

        if(structure) {
            int grpInd = GetGroupIndex(structure.data);

            if(mGroupSpawnCounts[grpInd] > 0) //shouldn't be 0 at this point
                mGroupSpawnCounts[grpInd]--;
        }
    }

    private void PlacementClear() {
        mColl.enabled = false;

        mPlacementCurStuctureData = null;

        mPlacementCurGhostItem.active = false;
        mPlacementCurGhostItem = null;

        mPlacementPointerEvent = null;

        mPlacementIsDragging = false;

        placementCursor.active = false;
        placementConfirmRoot.gameObject.SetActive(false);
    }
}
