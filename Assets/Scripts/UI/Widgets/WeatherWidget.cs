using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeatherWidget : MonoBehaviour {
    [Header("Simple Display")]
    public GameObject simpleRootGO;
    public float simpleWidth;

    [Header("Expand Display")]
    public GameObject expandRootGO;
    public float expandWidth;

    private RectTransform mRectTrans;
}
