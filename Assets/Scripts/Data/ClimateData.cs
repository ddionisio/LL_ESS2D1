using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "climate", menuName = "Game/Climate")]
public class ClimateData : ScriptableObject {
    [System.Serializable]
    public class SeasonInfo {
        public SeasonData season;
        public AtmosphereStat[] atmosphereModifiers;
    }

    [Header("Info")]
    [M8.Localize]
    public string nameRef;
    [M8.Localize]
    public string descRef;

    public Sprite icon;

    [Header("Seasons")]
    public SeasonInfo[] seasons;
}
