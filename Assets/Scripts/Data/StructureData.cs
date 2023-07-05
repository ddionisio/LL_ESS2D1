using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structure", menuName = "Game/Structure")]
public class StructureData : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;
    [M8.Localize]
    public string descRef;

    public Sprite icon;

    [Header("Spawn")]
    public GameObject ghostPrefab;
    public Structure spawnPrefab;
}
