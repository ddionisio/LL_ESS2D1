using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "season", menuName = "Game/Season")]
public class SeasonData : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public Sprite icon;
}
