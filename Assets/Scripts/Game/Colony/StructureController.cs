using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureController : MonoBehaviour {
    public const string poolGroup = "structure";

    public class GhostItem {
        public StructureData structure;
        public GameObject ghostGO;

        public GhostItem(StructureData data, GameObject go) {
            structure = data;

            ghostGO = go;
            ghostGO.SetActive(false);

            var trans = ghostGO.transform;
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = Vector3.one;
        }
    }

    [Header("Spawn Info")]
    public Transform spawnRoot;

    [Header("Placement Info")]
    public Transform placementRoot; //for placing structure on landscape

    [Header("Ghost Info")]
    public Transform ghostRoot; //structure's 'ghost' is placed here and activated during placement

    public StructurePaletteData paletteData { get; private set; }

    private int[] mPaletteItemSpawnCounts; //correlates to paletteData's items

    private GhostItem[] mGhostStructures;

    private M8.CacheList<Structure> mStructureActives;

    private M8.PoolController mPoolCtrl;

    public void Setup(StructurePaletteData aPaletteData) {
        paletteData = aPaletteData;

        mPoolCtrl = M8.PoolController.CreatePool(poolGroup);

        var ghostStructureList = new List<GhostItem>();

        int totalCapacity = 0;

        //setup pool and ghost content
        for(int i = 0; i < paletteData.items.Length; i++) {
            var item = paletteData.items[i];

            var capacity = item.capacity;

            for(int j = 0; j < item.structures.Length; j++) {
                var structure = item.structures[j];

                //add new item in pool
                mPoolCtrl.AddType(structure.spawnPrefab, capacity, capacity);

                //setup ghost
                var newGhostGO = Instantiate(structure.ghostPrefab, ghostRoot);

                ghostStructureList.Add(new GhostItem(structure, newGhostGO));
            }

            totalCapacity += capacity;
        }

        mStructureActives = new M8.CacheList<Structure>(totalCapacity);

        mGhostStructures = ghostStructureList.ToArray();

        /*public event System.Action<PoolDataController> spawnCallback;
        public event System.Action<PoolDataController> despawnCallback;*/
    }


}
