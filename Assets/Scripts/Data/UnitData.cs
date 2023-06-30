using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unit", menuName = "Game/Unit")]
public class UnitData : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;
    [M8.Localize]
    public string descRef;

    public Sprite icon;

    [Header("Spawn")]
    public GameObject spawnPrefab;
}
