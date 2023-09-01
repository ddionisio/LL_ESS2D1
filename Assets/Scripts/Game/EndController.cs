using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

using LoLExt;

public class EndController : ColonyController {
    [Header("End Data")]
    public SeasonData endSeason;
    public Transform spawnPointRoot;
    public Transform structurePointRoot;

    [Header("End Pop Display")]
    public AnimatorEnterExit congratsAnim;

    public GameObject endPopulationActiveGO;
    public TMP_Text endPopulationLabel;
    public MedalWidget endPopulationMedalWidget;

    public GameObject thankYouActiveGO;

    protected override void OnInstanceInit() {
        //ensure scene manager exists
        if(!M8.SceneManager.isInstantiated)
            M8.SceneManager.Reinstantiate();

        //ensure UIRoot exists
        if(!UIRoot.isInstantiated)
            UIRoot.Reinstantiate();
        //

        mainCamera = Camera.main;
        if(mainCamera) {
            mainCamera2D = mainCamera.GetComponent<M8.Camera2D>();
            mainCameraTransform = mainCamera.transform.parent;
        }

        //initialize resource array
        var structureResVals = System.Enum.GetValues(typeof(StructureResourceData.ResourceType));
        mResources = new ResourceInfo[structureResVals.Length];
        for(int i = 0; i < mResources.Length; i++)
            mResources[i] = new ResourceInfo();

        //determine landscape
        CycleController firstCycleController = null;

        for(int i = 0; i < regionRoot.childCount; i++) {
            var cycleCtrl = regionRoot.GetChild(i).GetComponent<CycleController>();
            if(!cycleCtrl)
                continue;

            if(!firstCycleController) {
                firstCycleController = cycleCtrl;
                break;
            }
        }

        //setup cycle control
        if(!cycleController && firstCycleController) //no region index match, use first ctrl (fail-safe)
            cycleController = firstCycleController;

        cycleController.gameObject.SetActive(true);

        cycleController.Setup(hotspotData, endSeason);
        //

        //setup structure control
        structurePaletteController.Setup(structurePalette);

        //setup unit control
        unitPaletteController.Setup(this);

        //setup structures (unit spawning, etc)
        for(int i = 0; i < structurePalette.groups.Length; i++) {
            var grp = structurePalette.groups[i];
            var capacity = grp.capacity;

            for(int j = 0; j < grp.structures.Length; j++) {
                var structureData = grp.structures[j].data;
                structureData.Setup(this, capacity);
            }
        }

        //setup colony ship
        colonyShip.Init(this);

        //setup total population
        var totalPopCount = GameData.instance.totalPopulation;
        var totalPopCap = GameData.instance.totalPopulationCapacity;

        if(endPopulationLabel) endPopulationLabel.text = totalPopCount.ToString();
        if(endPopulationMedalWidget) {
            float t = totalPopCap > 0 ? Mathf.Clamp01(((float)totalPopCount) / totalPopCap) : 0f;
            endPopulationMedalWidget.ApplyRank(t);
        }
    }

    protected override IEnumerator Start() {
        do {
            yield return null;
        } while(M8.SceneManager.instance.isLoading);

        if(signalModeChanged)
            signalModeChanged.Invoke(mode);
        //

        if(!string.IsNullOrEmpty(musicPlay))
            M8.MusicPlaylist.instance.Play(musicPlay, false, true);

        var lastTime = Time.time;

        var gameDat = GameData.instance;

        gameDat.signalColonyStart?.Invoke();

        //colony ship enter
        colonyShip.Spawn();
                
        //spawn houses
        var houseCount = Mathf.Min(structurePalette.groups[0].capacity, structurePointRoot.childCount);
        for(int i = 0; i < houseCount; i++) {
            yield return new WaitForSeconds(1f);

            var spawnTrans = structurePointRoot.GetChild(i);

            Vector2 spawnPt;
            GroundPoint groundPt;

            if(GroundPoint.GetGroundPoint(spawnTrans.position.x, out groundPt))
                spawnPt = groundPt.position;
            else
                spawnPt = spawnTrans.position;

            structurePaletteController.Spawn(structurePalette.groups[0].structures[0].data, spawnPt, Vector2.up);
        }

        //wait for colony to be active
        while(colonyShip.state != StructureState.Active)
            yield return null;

        //wait for houses
        var structActives = structurePaletteController.structureActives;
        var readyCount = 0;
        while(readyCount < structActives.Count) {
            readyCount = 0;
            for(int i = 0; i < structActives.Count; i++) {
                var structure = structActives[i];
                if(structure.state == StructureState.Active)
                    readyCount++;
            }

            yield return null;
        }

        cycleController.Begin();

        //spawn frogs
        bool isFaceLeft = false;

        var count = Mathf.Min(unitPalette.units.Length, spawnPointRoot.childCount);
        for(int i = 0; i < count; i++) {
            var spawnTrans = spawnPointRoot.GetChild(i);

            Vector2 spawnPt;
            GroundPoint groundPt;

            if(GroundPoint.GetGroundPoint(spawnTrans.position.x, out groundPt))
                spawnPt = groundPt.position;
            else
                spawnPt = spawnTrans.position;

            var unit = unitController.Spawn(unitPalette.units[i].data, null, spawnPt);
            while(unit.state == UnitState.Spawning)
                yield return null;

            unit.state = UnitState.End;
            unit.facing = isFaceLeft ? MovableBase.Facing.Left : MovableBase.Facing.Right;
            isFaceLeft = !isFaceLeft;
        }

        cycleController.End();

        //victory
        if(gameDat.signalVictory) gameDat.signalVictory.Invoke();

        //congrats
        if(congratsAnim) {
            congratsAnim.Show();
            yield return congratsAnim.PlayEnterWait();
        }

        yield return new WaitForSeconds(6.0f);

        //total population widget
        if(endPopulationActiveGO)
            endPopulationActiveGO.SetActive(true);

        //thank you for playing
        yield return new WaitForSeconds(1.0f);

        if(thankYouActiveGO) {
            thankYouActiveGO.SetActive(true);

            yield return new WaitForSeconds(5.0f);
        }

        var time = Time.time - lastTime;
        //Debug.Log("Time: " + time);

        //end
        LoLManager.instance.Complete();
    }
}