using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "resource", menuName = "Game/Resource")]
public class ResourceData : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public Sprite icon;
}
