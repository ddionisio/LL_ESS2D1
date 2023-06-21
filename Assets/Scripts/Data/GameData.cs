using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

[CreateAssetMenu(fileName = "gameData", menuName = "Game/GameData")]
public class GameData : M8.SingletonScriptableObject<GameData> {
    [System.Serializable]
    public struct LevelData {
        public M8.SceneAssetPath overworldScene;
        public M8.SceneAssetPath colonyScene;
    }

    [Header("Modals")]
    public string modalOverworld = "overworld";
    public string modalHotspotInvestigate = "hotspotInvestigate";

    [Header("Scenes")]
    //intro sets progress to 1
    public LevelData[] levels;
    public M8.SceneAssetPath endScene;

    public bool isProceed { get; private set; }

    public void ResetProgress() {
        LoLManager.instance.userData.Delete();

        LoLManager.instance.ApplyProgress(0, 0);
    }

    public void NextProgress() {
        int curProgress;

        if(isProceed) {
            curProgress = LoLManager.instance.curProgress;
        }
        else { //progressing through current scene in editor
            LoLManager.instance.progressMax = levels.Length + 1;

            //setup progress
            var curScene = M8.SceneManager.instance.curScene;
            if(curScene != endScene) { //we shouldn't be calling this function at end scene
                curProgress = 0;

                for(int i = 0; i < levels.Length; i++) {
                    var lvl = levels[i];

                    if(lvl.overworldScene == curScene) {
                        curProgress = i * 2 + 1;
                        break;
                    }
                    else if(lvl.colonyScene == curScene) {
                        curProgress = i * 2 + 2;
                        break;
                    }
                }

                isProceed = true;
            }
            else
                curProgress = -1;
        }

        if(curProgress >= 0) {
            LoLManager.instance.ApplyProgress(curProgress + 1);
            LoadLevelFromProgress();
        }
    }

    public void ContinueProgress() {
        isProceed = true;

        LoLManager.instance.progressMax = levels.Length + 1;

        //intro should have incremented the progress to 1
        if(LoLManager.instance.curProgress <= 0)
            LoLManager.instance.ApplyProgress(1);

        LoadLevelFromProgress();
    }

    private void LoadLevelFromProgress() {
        var curProgress = LoLManager.instance.curProgress;

        var isOverworld = curProgress % 2 != 0;
        var levelInd = (curProgress - 1) / 2;

        if(levelInd < levels.Length) {
            var curLevel = levels[levelInd];

            if(isOverworld)
                curLevel.overworldScene.Load();
            else {
                //load colony settings from overworld

                curLevel.colonyScene.Load();
            }
        }
        else
            endScene.Load();
    }
}
