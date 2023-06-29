using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "climate", menuName = "Game/Climate")]
public class ClimateData : ScriptableObject {
    [System.Serializable]
    public class SeasonInfo {
        public SeasonData season;
        public AtmosphereModifier[] atmosphereMods;
    }

    [Header("Info")]
    [M8.Localize]
    public string nameRef;
    [M8.Localize]
    public string descRef;

    public Sprite icon;

    [Header("Seasons")]
    public SeasonInfo[] seasons;

    public AtmosphereModifier[] GetModifiers(SeasonData curSeason) {
        for(int i = 0; i < seasons.Length; i++) {
            var season = seasons[i];
            if(season.season == curSeason)
                return season.atmosphereMods;
        }

        return null;
    }

    public int GetSeasonIndex(SeasonData season) {
        for(int i = 0; i < seasons.Length; i++) {
            if(seasons[i].season == season)
                return i;
        }

        return -1;
    }
}
