using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "hotspot", menuName = "Game/Hotspot")]
public class HotspotData : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public ClimateData climate;

    public AtmosphereStat[] atmosphereStats;

    [Header("Inspection")]
    public GameObject landscapePrefab;
        
    public M8.SceneAssetPath colonyScene; //if viable, this is the scene to load when launching
}
