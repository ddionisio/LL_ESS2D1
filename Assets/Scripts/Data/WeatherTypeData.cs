using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "weatherType", menuName = "Game/Weather Type")]
public class WeatherTypeData : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef; //short detail, ex: Mostly Sunny, T-Storms
    [M8.Localize]
    public string detailRef; //description of the weather

    public Sprite icon;
    public Sprite image; //higher quality for forecast and description

    public bool isSunVisible;
    public bool isHazzard;
}
