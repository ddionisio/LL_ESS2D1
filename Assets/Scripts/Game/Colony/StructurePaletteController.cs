using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructurePaletteController : MonoBehaviour  {
    public const string poolGroup = "structure";

    public struct StructureInfo {
        public StructureData data;
        public bool isHidden;
    }

    public class GroupInfo {
        public StructureInfo[] structures;

        /// <summary>
        /// Current capacity for each group (use for house to limit capacity based on population)
        /// </summary>
        public int capacity;

        /// <summary>
        /// Current spawned structures under this group
        /// </summary>
        public int count;

        public int visibleStructuresCount {
            get {
                var visibleCount = 0;

                for(int i = 0; i < structures.Length; i++) {
                    if(!structures[i].isHidden)
                        visibleCount++;
                }

                return visibleCount;
            }
        }

        public bool IsHidden(StructureData structureData) {
            for(int i = 0; i < structures.Length; i++) {
                var inf = structures[i];
                if(inf.data == structureData)
                    return inf.isHidden;
            }

            return true;
        }

        public void SetHidden(StructureData structureData, bool hidden) {
            for(int i = 0; i < structures.Length; i++) {
                var inf = structures[i];
                if(inf.data == structureData) {
                    inf.isHidden = hidden;
                    structures[i] = inf;
                    return;
                }
            }
        }
                
        public GroupInfo(StructurePaletteData.GroupInfo dataGrpInf) {
            capacity = dataGrpInf.capacityStart;
            count = 0;

            structures = new StructureInfo[dataGrpInf.structures.Length];
            for(int i = 0; i < structures.Length; i++) {
                var structureInfo = dataGrpInf.structures[i];

                structures[i] = new StructureInfo { data = structureInfo.data, isHidden = structureInfo.isHidden };
            }
        }
    }

    [Header("Spawn Info")]
    public Transform spawnRoot;

    [Header("Placement Info")]
    public StructurePlacementInput placementInput;
    public Transform placementBlockerRoot; //ensure this root's layer is "PlacementBlocker"

    [Header("Signal Invoke")]
    public SignalStructure signalInvokeStructureSpawned;
    public SignalStructure signalInvokeStructureDespawned;

    public M8.SignalInteger signalInvokeGroupInfoRefresh;

    public StructurePaletteData paletteData { get; private set; }

    public StructureData placementCurrentStructureData { get { return mPlacementCurStuctureData; } }

    private GroupInfo[] mGroupInfos; //correlates to paletteData's items
        
    private M8.CacheList<Structure> mStructureActives;

    private M8.PoolController mPoolCtrl;

    private Dictionary<Structure, BoxCollider2D> mPlacementBlockerActives;
    private M8.CacheList<BoxCollider2D> mPlacementBlockerCache;

    private Structure mPlacementCurStructure; //when moving structures
        
    private StructureData mPlacementCurStuctureData;
    private int mPlacementCurGroupIndex;
                
    private M8.GenericParams mSpawnParms = new M8.GenericParams();

    public int GroupGetIndex(StructureData structureData) {
        return paletteData.GetGroupIndex(structureData);
    }

    public bool GroupIsFull(StructureData structureData) {
        return GroupIsFull(GroupGetIndex(structureData));
    }

    public bool GroupIsFull(int groupIndex) {
        if(groupIndex < 0 || groupIndex >= mGroupInfos.Length)
            return true;

        var grpInf = mGroupInfos[groupIndex];

        return grpInf.count >= grpInf.capacity;
    }

    public void GroupSetStructureHidden(StructureData structureData, bool isHidden) {
        var groupIndex = GroupGetIndex(structureData);
        if(groupIndex == -1)
            return;

        var grpInf = mGroupInfos[groupIndex];
        grpInf.SetHidden(structureData, isHidden);

        signalInvokeGroupInfoRefresh?.Invoke(groupIndex);
    }

    public void GroupAddCapacity(StructureData structureData, int amount) {
        GroupAddCapacity(GroupGetIndex(structureData), amount);
    }

    public void GroupAddCapacity(int groupIndex, int amount) {
        if(groupIndex < 0 || groupIndex >= mGroupInfos.Length)
            return;

        var grpInf = mGroupInfos[groupIndex];

        grpInf.capacity = Mathf.Clamp(grpInf.capacity + amount, 0, paletteData.groups[groupIndex].capacity);

        signalInvokeGroupInfoRefresh?.Invoke(groupIndex);
    }

    public GroupInfo GroupGetInfo(int groupIndex) {
        if(groupIndex < 0 || groupIndex >= mGroupInfos.Length)
            return null;

        return mGroupInfos[groupIndex];
    }

    /// <summary>
    /// For moving
    /// </summary>
    public void PlacementStart(Structure structure) {
        //ensure we can move structure
        if(!structure.isMovable) {
            //display hint - can't move due to <something>
            return;
        }

        mPlacementCurStructure = structure;
        mPlacementCurStructure.state = StructureState.MoveReady;

        mPlacementCurStuctureData = structure.data;

        PlacementActive();
    }

    public void PlacementStart(StructureData structureData) {
        mPlacementCurStuctureData = structureData;
        mPlacementCurGroupIndex = GroupGetIndex(structureData);

        PlacementActive();
    }

    /// <summary>
    /// Should be called via confirm
    /// </summary>
    public void PlacementAccept() {
        var placementCursor = placementInput.cursor;

        if(mPlacementCurStructure) { //move structure
            mPlacementCurStructure.MoveTo(placementCursor.positionGround);
        }
        else if(mPlacementCurStuctureData) { //spawn structure
            if(!(mStructureActives.IsFull || GroupIsFull(mPlacementCurGroupIndex))) { //ensure it is valid (fail-safe)
                                                                                      //spawn at ground
                mSpawnParms[StructureSpawnParams.spawnPoint] = placementCursor.positionGround;
                mSpawnParms[StructureSpawnParams.spawnNormal] = placementCursor.normalGround;

                mSpawnParms[StructureSpawnParams.data] = mPlacementCurStuctureData;

                var spawnName = mPlacementCurStuctureData.spawnPrefab.name;
                var newStructure = mPoolCtrl.Spawn<Structure>(spawnName, spawnName, spawnRoot, mSpawnParms);

                mStructureActives.Add(newStructure);

                mGroupInfos[mPlacementCurGroupIndex].count++;

                signalInvokeStructureSpawned?.Invoke(newStructure);
            }
        }

        PlacementClear();
    }

    /// <summary>
    /// Should be called via cancel (also if HUD has a cancel button)
    /// </summary>
    public void PlacementCancel() {
        if(mPlacementCurStructure)
            mPlacementCurStructure.state = StructureState.Active;

        PlacementClear();
    }

    public void PlacementAddBlocker(Structure structure) {
        //add new blocker if it hasn't already
        BoxCollider2D blocker;
        if(!mPlacementBlockerActives.TryGetValue(structure, out blocker)) {
            blocker = mPlacementBlockerCache.RemoveLast();

            blocker.gameObject.SetActive(true);

            mPlacementBlockerActives.Add(structure, blocker);
        }

        blocker.transform.position = structure.position;

        //adjust dimensions
        Vector2 ofs, size;

        if(structure.boxCollider) {
            ofs = structure.boxCollider.offset;
            size = structure.boxCollider.size;
        }
        else {
            ofs = structure.data.ghostPrefab.placementBounds.center;
            size = structure.data.ghostPrefab.placementBounds.size;
        }

        blocker.offset = ofs;
        blocker.size = size;
    }

    public void PlacementAddBlocker(Structure structure, Vector2 worldPos) {
        PlacementAddBlocker(structure);

        var blocker = mPlacementBlockerActives[structure];

        blocker.transform.position = worldPos;
    }

    public void PlacementRemoveBlocker(Structure structure) {
        BoxCollider2D blocker;
        if(mPlacementBlockerActives.TryGetValue(structure, out blocker)) {
            blocker.gameObject.SetActive(false);

            mPlacementBlockerCache.Add(blocker);

            mPlacementBlockerActives.Remove(structure);
        }
    }
        
    public void Setup(StructurePaletteData aPaletteData) {
        //initialize pool
        if(!mPoolCtrl) {
            mPoolCtrl = M8.PoolController.CreatePool(poolGroup);

            mPoolCtrl.despawnCallback += OnStructureDespawn;
        }

        paletteData = aPaletteData;

        var groupCount = paletteData.groups.Length;

        mGroupInfos = new GroupInfo[groupCount];
        
        int totalCapacity = 0;

        //setup pool
        for(int i = 0; i < groupCount; i++) {
            var grpItm = paletteData.groups[i];

            var capacity = grpItm.capacity;

            for(int j = 0; j < grpItm.structures.Length; j++) {
                var structureInfo = grpItm.structures[j];
                var structure = structureInfo.data;

                //add new item in pool
                mPoolCtrl.AddType(structure.spawnPrefab.gameObject, capacity, capacity);
            }

            mGroupInfos[i] = new GroupInfo(grpItm);

            totalCapacity += capacity;
        }

        mStructureActives = new M8.CacheList<Structure>(totalCapacity);

        //setup placement blocker
        mPlacementBlockerActives = new Dictionary<Structure, BoxCollider2D>(totalCapacity);
        mPlacementBlockerCache = new M8.CacheList<BoxCollider2D>(totalCapacity);

        int blockerLayer = placementBlockerRoot.gameObject.layer;

        for(int i = 0; i < totalCapacity; i++) {
            var newBlockerGO = new GameObject("blocker");
            newBlockerGO.layer = blockerLayer;

            newBlockerGO.transform.SetParent(placementBlockerRoot, false);

            var newBlockerColl = newBlockerGO.AddComponent<BoxCollider2D>();

            newBlockerGO.SetActive(false);

            mPlacementBlockerCache.Add(newBlockerColl);
        }

        //initialize placement
        placementInput.Init(paletteData);
    }

    void OnDestroy() {
        if(mPoolCtrl) {
            mPoolCtrl.despawnCallback -= OnStructureDespawn;
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
            //ensure associated blocker is detached
            PlacementRemoveBlocker(structure);

            //fail-safe - if for some reason we despawn during placement of this structure
            if(mPlacementCurStructure == structure)
                PlacementClear();

            int grpInd = GroupGetIndex(structure.data);

            if(mGroupInfos[grpInd].count > 0) //shouldn't be 0 at this point
                mGroupInfos[grpInd].count--;

            signalInvokeStructureDespawned?.Invoke(structure);
        }
    }

    private void PlacementActive() {

        if(!placementInput.Activate(mPlacementCurStuctureData))
            return; //shouldn't happen

        GameData.instance.signalPlacementActive?.Invoke(true);
    }

    private void PlacementClear() {
        mPlacementCurStructure = null;

        mPlacementCurStuctureData = null;

        placementInput.Deactivate();

        GameData.instance.signalPlacementActive?.Invoke(false);
    }
}
