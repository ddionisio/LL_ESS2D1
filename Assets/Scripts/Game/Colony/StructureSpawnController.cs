using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureSpawnController : MonoBehaviour {
    [System.Serializable]
    public class TierData {
        public GameObject rootGO;
        public int populationQuota;

        private StructureSpawnItem[] mItems;

		public void Init() {
            if(rootGO && rootGO.activeSelf)
                mItems = rootGO.GetComponentsInChildren<StructureSpawnItem>(true);
            else
                mItems = new StructureSpawnItem[0];
		}

		public IEnumerator SpawnSequence() {
            for(int i = 0; i < mItems.Length; i++) {
                var itm = mItems[i];
                if(!itm)
                    continue;

                itm.Spawn();

                while(itm.isSpawning)
                    yield return null;
            }
        }

        public void CancelSpawns() {
            for(int i = 0; i < mItems.Length; i++) {
				var itm = mItems[i];
				if(!itm)
					continue;

                itm.CancelSpawn();
			}
        }
    }

    public TierData[] tierSpawns;

    private Coroutine mSpawnRout;

	void OnDisable() {
        CancelSpawns();
	}

    void OnDestroy() {
        if(GameData.isInstantiated) {
            var gameDat = GameData.instance;

			if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback -= OnCycleBegin;
			if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback -= OnCycleEnd;
		}
    }

	void Awake() {
		var gameDat = GameData.instance;

		if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback += OnCycleBegin;
		if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback += OnCycleEnd;

        //initialize spawners
        for(int i = 0; i < tierSpawns.Length; i++)
            tierSpawns[i].Init();
	}

    void OnCycleBegin() {
        CancelSpawns();

        mSpawnRout = StartCoroutine(DoSpawns());
    }

    void OnCycleEnd() {
        CancelSpawns();
	}

    IEnumerator DoSpawns() {
        var colonyCtrl = ColonyController.instance;

        for(int i = 0; i < tierSpawns.Length; i++) {
            var tierSpawn = tierSpawns[i];

            //wait until population capacity is met
            while(colonyCtrl.population < tierSpawn.populationQuota)
				yield return null;

            yield return tierSpawn.SpawnSequence();
		}

        mSpawnRout = null;
	}

    private void CancelSpawns() {
        if(mSpawnRout != null) {
            StopCoroutine(mSpawnRout);
            mSpawnRout = null;
		}

        for(int i = 0; i < tierSpawns.Length; i++)
            tierSpawns[i].CancelSpawns();
    }
}
