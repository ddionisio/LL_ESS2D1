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

            ghost = Instantiate(structure.ghostPrefab, root);

            mGO = ghost.gameObject;
            mGO.SetActive(false);

            var trans = ghost.transform;
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = Vector3.one;
        }
    }

    public struct GroupInfo {
        /// <summary>
        /// Current capacity for each group (use for house to limit capacity based on population)
        /// </summary>
        public int capacity;

        /// <summary>
        /// Current spawned structures under this group
        /// </summary>
        public int count;

        /// <summary>
        /// Tutorial purpose to slowly introduce structures
        /// </summary>
        public bool isHidden;
    }

    [Header("Spawn Info")]
    public Transform spawnRoot;

    [Header("Placement Info")]
    public GameBounds2D placementBounds;
    public StructurePlacementCursor placementCursor;    
    public Transform placementGhostRoot; //structure's 'ghost' is placed here and activated during placement

    [Header("Signal Invoke")]
    public M8.SignalBoolean signalInvokePlacementActive;
    public M8.SignalBoolean signalInvokePlacementClick; //false = placement is moving, true = clicked, placement move stopped

    public SignalStructure signalInvokeStructureSpawned;
    public SignalStructure signalInvokeStructureDespawned;

    public M8.Signal signalInvokeGroupInfoRefresh;

    public StructurePaletteData paletteData { get; private set; }

    public bool isPlacementValid { get { return placementCursor.isValid; } }

    public StructureGhost placementCurrentGhost { get { return mPlacementCurGhostItem != null ? mPlacementCurGhostItem.ghost : null; } }

    public bool isPlacementActive { get { return mPlacementCurStuctureData; } }

    private GroupInfo[] mGroupInfos; //correlates to paletteData's items

    private GhostItem[] mGhostStructures;

    private M8.CacheList<Structure> mStructureActives;

    private M8.PoolController mPoolCtrl;

    private Collider2D mColl;
        
    private StructureData mPlacementCurStuctureData;
    private int mPlacementCurGroupIndex;

    private GhostItem mPlacementCurGhostItem;

    private PointerEventData mPlacementPointerEvent;
    private bool mPlacementIsDragging;

    private bool mIsClicked;

    private M8.GenericParams mSpawnParms = new M8.GenericParams();

    public bool IsGroupFull(StructureData structureData) {
        return IsGroupFull(GetGroupIndex(structureData));
    }

    public bool IsGroupFull(int groupIndex) {
        if(groupIndex < 0 || groupIndex >= mGroupInfos.Length)
            return true;

        var grpInf = mGroupInfos[groupIndex];

        return grpInf.count >= grpInf.capacity;
    }

    public GroupInfo GetGroupInfo(int groupIndex) {
        if(groupIndex < 0 || groupIndex >= mGroupInfos.Length)
            return new GroupInfo();

        return mGroupInfos[groupIndex];
    }

    public void PlacementStart(StructureData structureData) {
        mPlacementCurStuctureData = structureData;
        mPlacementCurGroupIndex = GetGroupIndex(structureData);

        //grab ghost
        for(int i = 0; i < mGhostStructures.Length; i++) {
            var ghostItem = mGhostStructures[i];
            if(ghostItem.structure == structureData) {
                mPlacementCurGhostItem = ghostItem;
                break;
            }
        }

        if(mPlacementCurGhostItem != null) {
            mPlacementCurGhostItem.active = true;

            placementCursor.width = mPlacementCurGhostItem.ghost.placementBounds.size.x;
        }

        placementCursor.active = true;

        mColl.enabled = true;

        mIsClicked = false;

        signalInvokePlacementActive?.Invoke(true);
    }

    /// <summary>
    /// Should be called via confirm
    /// </summary>
    public void PlacementAccept() {
        //ensure it is valid (fail-safe)
        if(!(mStructureActives.IsFull || IsGroupFull(mPlacementCurGroupIndex))) {
            //spawn at ground
            mSpawnParms[StructureSpawnParams.spawnPoint] = placementCursor.positionGround;
            mSpawnParms[StructureSpawnParams.spawnNormal] = placementCursor.normalGround;

            mSpawnParms[StructureSpawnParams.data] = mPlacementCurStuctureData;

            var newStructure = mPoolCtrl.Spawn<Structure>(mPlacementCurStuctureData.spawnPrefab.name, spawnRoot, mSpawnParms);

            mStructureActives.Add(newStructure);

            mGroupInfos[mPlacementCurGroupIndex].count++;

            signalInvokeGroupInfoRefresh?.Invoke();

            signalInvokeStructureSpawned?.Invoke(newStructure);
        }

        PlacementClear();
    }

    /// <summary>
    /// Should be called via cancel (also if HUD has a cancel button)
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

        mGroupInfos = new GroupInfo[groupCount];
                
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

            mGroupInfos[i] = new GroupInfo { count = 0, capacity = capacity, isHidden = false };

            totalCapacity += capacity;
        }

        mStructureActives = new M8.CacheList<Structure>(totalCapacity);

        mGhostStructures = ghostStructureList.ToArray();

        //initialize placement
        mColl.enabled = false;

        placementCursor.active = false;
    }

    void OnDestroy() {
        if(mPoolCtrl) {
            mPoolCtrl.despawnCallback -= OnStructureDespawn;
        }
    }

    void Update() {
        if(!(mIsClicked || mPlacementIsDragging || mPlacementPointerEvent == null)) {
            UpdateCursor(mPlacementPointerEvent);
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        mPlacementPointerEvent = eventData;
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        mPlacementPointerEvent = null;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        UpdateCursor(eventData);

        //click
        mIsClicked = isPlacementValid;
        signalInvokePlacementClick?.Invoke(mIsClicked);
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
        mPlacementIsDragging = true;

        mIsClicked = false;
        signalInvokePlacementClick?.Invoke(false);

        UpdateCursor(eventData);
    }

    void IDragHandler.OnDrag(PointerEventData eventData) {
        if(mPlacementIsDragging)
            UpdateCursor(eventData);
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
        if(mPlacementIsDragging) {
            mPlacementIsDragging = false;

            UpdateCursor(eventData);

            //click
            mIsClicked = isPlacementValid;
            signalInvokePlacementClick?.Invoke(mIsClicked);
        }
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

            if(mGroupInfos[grpInd].count > 0) { //shouldn't be 0 at this point
                mGroupInfos[grpInd].count--;

                signalInvokeGroupInfoRefresh?.Invoke();
            }

            signalInvokeStructureDespawned?.Invoke(structure);
        }
    }

    private void UpdateCursor(PointerEventData pointerEventData) {
        var ptrRaycast = pointerEventData.pointerCurrentRaycast;
        if(ptrRaycast.isValid) {
            placementCursor.active = true;

            var worldPos = ptrRaycast.worldPosition;

            var itemBounds = mPlacementCurGhostItem.ghost.placementBounds;

            //clamp from bounds
            var pos = new Vector2(
                placementBounds.ClampX(worldPos.x, itemBounds.extents.x),
                placementBounds.ClampY(worldPos.y, 0f));

            placementCursor.position = pos;

            //check if valid placement
            var checkValid = false;

            var levelBounds = ColonyController.instance.bounds;

            var checkPoint = new Vector2(pos.x, levelBounds.max.y);
            var checkDir = Vector2.down;

            var hit = Physics2D.BoxCast(checkPoint, itemBounds.size, 0f, checkDir, levelBounds.size.y, GameData.instance.placementCheckLayerMask);
            var hitColl = hit.collider;
            if(hitColl) {
                checkValid = (mPlacementCurGhostItem.structure.placementValidLayerMask & (1 << hitColl.gameObject.layer)) != 0;
            }

            placementCursor.isValid = checkValid;
        }
        else {
            placementCursor.isValid = false;
            placementCursor.active = false;
        }
    }

    private void PlacementClear() {
        mColl.enabled = false;

        mPlacementCurStuctureData = null;

        if(mPlacementCurGhostItem != null) {
            mPlacementCurGhostItem.active = false;
            mPlacementCurGhostItem = null;
        }

        mPlacementPointerEvent = null;

        mPlacementIsDragging = false;

        placementCursor.active = false;

        signalInvokePlacementActive?.Invoke(false);
    }
}
