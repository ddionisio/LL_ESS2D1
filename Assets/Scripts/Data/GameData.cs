using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

[CreateAssetMenu(fileName = "gameData", menuName = "Game/GameData")]
public class GameData : M8.SingletonScriptableObject<GameData> {
    public const string saveKeyScene = "s";
    public const string saveKeyRegionIndex = "ri";
    public const string saveKeySeasonIndex = "si";

    public const int clickCategoryBackground = 1;
    public const int clickCategoryStructure = 2;
    public const int clickCategoryStructurePalette = 3;
    public const int clickCategoryUnitPalette = 4;

    public const string structureWaypointSpawn = "spawn";
    public const string structureWaypointWork = "work";
    public const string structureWaypointCollect = "collect";
    public const string structureWaypointIdle = "idle"; //use for any units that has no work to do and need to move somewhere
    public const string structureWaypointRoam = "roam"; //use for fly when roaming around target, or for hunter patrol

    [Header("Modals")]
    public string modalOverworld = "overworld";
    public string modalHotspotInvestigate = "hotspotInvestigate";
    public string modalHotspotAnalyze = "hotspotAnalyze";

    [Header("Overworld")]
    public int overworldLaunchCriticGoodCount = 3; //determines how many must be satisfied to launch colony

    [Header("Colony")]
    public LayerMask groundLayerMask;
    public LayerMask waterLayerMask;
    public LayerMask placementCheckLayerMask;

    public float cycleDuration = 120f; //entire duration of the colony game
    public float cycleDaylightScaleDefault = 0.5f;

    public float fastForwardScale = 2.0f;

    [Header("Colony | Structure")]
    public LayerMask structureLayerMask;
    public float structureBuildScalePerWork = 1f; //scale build time by this amount per work
    public float structureRepairScalePerWork = 1f; //scale build time by this amount per work
    
    public float structureRepairPerHitDelay = 1f; //for reparable structures, delay to restore one hp
    public float structureDamageDelay = 0.5f; //how long to stay in damaged state

    public float structureDemolishDelay = 2f; //how long before demolish is actually done.

    public StructureData[] structureFoodSources;
    public StructureData[] structureWaterSources;

    [Header("Colony | Structure | Plant")]
    public float growthScalePerWork = 1f; //scale growth time by this amount per work

    [Header("Colony | Structure | Unit Spawn")]
    public float structureUnitSpawnDelay = 0.3f; //delay to spawn a unit

    [Header("Colony | Unit")]
    [M8.TagSelector]
    public string unitAllyTag;
    [M8.TagSelector]
    public string unitEnemyTag;

    public LayerMask unitLayerMask;

    public float unitFallSpeed = 10f;
    public float unitUpdateAIDelay = 0.3f;
    public float unitHurtDelay = 0.5f; //how long to stay in hurt state
    public float unitDyingDelay = 5f; //how long to stay in dying state
    public float unitIdleWanderDelay = 2f; //how long to stay in idle before moving to a new spot
    public float unitGatherContainerDelay = 0.5f; //how long to 'act' before getting resource from a container

    public M8.RangeFloat unitRetreatDistanceRange;

    public float unitBounceToBaseDelay = 1.5f;
    public M8.RangeFloat unitBounceToBaseHeightRange;

    [Header("Scenes")]
    //intro sets progress to 1
    public M8.SceneAssetPath overworldScene;
    public M8.SceneAssetPath endScene;

    [Header("Signals")]
    public M8.SignalBoolean signalPause;

    [Header("Signals | Colony")]    
    public M8.SignalInteger signalClickCategory;

    public M8.Signal signalCycleBegin;
    public M8.Signal signalCycleNext;
    public M8.Signal signalCycleEnd;

    public M8.SignalBoolean signalPlacementActive;

    public SignalStructure signalStructureClick;

    public SignalUnit signalUnitSpawned;
    public SignalUnit signalUnitDespawned;
    public SignalUnit signalUnitDying;

    [Header("Editor Config | Landscape")]
    public Vector2 landscapePreviewSize;

    public Color landscapePreviewBoundsColor = Color.yellow;
    public float landscapePreviewBoundsEditSnap = 1f;

    public Color landscapePreviewRegionColor = Color.green;
    public float landscapePreviewRegionHandleScale = 0.05f;
    public float landscapePreviewRegionHandleSnap = 0.25f;

    [Header("Editor Config | Structure")]
    public Color structurePlacementBoundsColor = Color.cyan;
    public float structurePlacementBoundsEditSnap = 1f;

    public Color[] structureWaypointColors;
    public float structureWaypointHandleScale = 0.05f;
    public float structureWaypointHandleSnap = 0.25f;

    [Header("Editor Config | Unit Spawner Waypoint")]
    public Color unitSpawnerWaypointColor = Color.red;
    public float unitSpawnerWaypointHandleScale = 0.05f;
    public float unitSpawnerWaypointHandleSnap = 0.25f;

    public bool isProceed { get; private set; }

    public bool isPaused {
        get { return mIsPaused; }
        set {
            if(mIsPaused != value) {
                mIsPaused = value;

                if(M8.SceneManager.isInstantiated) {
                    if(mIsPaused)
                        M8.SceneManager.instance.Pause();
                    else
                        M8.SceneManager.instance.Resume();
                }

                signalPause?.Invoke(mIsPaused);
            }
        }
    }

    private bool mIsPaused;

    /// <summary>
    /// Uses current progress to determine index = (curProgress - 1) / 2
    /// </summary>
    public int hotspotGroupIndex {
        get {
            var curProgress = isProceed ? LoLManager.instance.curProgress : 1;

            return (curProgress - 1) / 2;
        }
    }

    public int savedRegionIndex {
        get {
            return LoLManager.instance.userData.GetInt(saveKeyRegionIndex);
        }
    }

    public int savedSeasonIndex {
        get {
            return LoLManager.instance.userData.GetInt(saveKeySeasonIndex);
        }
    }

    public void ProgressReset() {
        LoLManager.instance.userData.Delete();

        LoLManager.instance.ApplyProgress(0, 0);

        isProceed = false;
    }

    public void ProgressNextToOverworld() {
        int curProgress;

        if(!isProceed) {
            curProgress = 0;
            isProceed = true;
        }
        else
            curProgress = LoLManager.instance.curProgress;

        LoLManager.instance.ApplyProgress(curProgress + 1); //assume curProgress is even (this determines which hotspot group to use)

        //end?
        if(LoLManager.instance.curProgress >= LoLManager.instance.progressMax)
            endScene.Load();
        else
            overworldScene.Load();
    }

    public void ProgressNextToColony(M8.SceneAssetPath colonyScene, int regionIndex, int seasonIndex) {
        int curProgress;

        if(!isProceed) {
            curProgress = 1; //can't really find which progress we are in...
            isProceed = true;
        }
        else
            curProgress = LoLManager.instance.curProgress;

        var userDat = LoLManager.instance.userData;

        //save scene to load
        userDat.SetString(saveKeyScene, colonyScene.name);

        //save level info
        userDat.SetInt(saveKeyRegionIndex, regionIndex);
        userDat.SetInt(saveKeySeasonIndex, seasonIndex);

        LoLManager.instance.ApplyProgress(curProgress + 1); //assume curProgress is odd

        colonyScene.Load();
    }

    public void ProgressContinue() {
        isProceed = true;

        var curProgress = LoLManager.instance.curProgress;

        if(curProgress == 0) { //shouldn't be 0 at this point if this function is called...
            ProgressNextToOverworld();
        }
        else if(curProgress == LoLManager.instance.progressMax) { //ending
            endScene.Load();
        }
        else if(curProgress % 2 == 1) { //overworld
            overworldScene.Load();
        }
        else { //continue to colony scene
            var userDat = LoLManager.instance.userData;

            var sceneName = userDat.GetString(saveKeyScene);

            if(!string.IsNullOrEmpty(sceneName))
                M8.SceneManager.instance.LoadScene(sceneName);
            else { //fail-safe, go back to previous progress
                LoLManager.instance.ApplyProgress(curProgress - 1);
                overworldScene.Load();
            }
        }
    }

    protected override void OnInstanceInit() {
        isProceed = false;
        mIsPaused = false;
    }
}
