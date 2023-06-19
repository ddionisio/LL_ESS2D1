using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class Hotspot : MonoBehaviour {
    [Header("Data")]
    public HotspotData data;

    [Header("Signal Invoke")]
    public SignalHotspot signalInvokeClick;

    public Vector2 position { get { return transform.position; } }

    /// <summary>
    /// Play hide animation
    /// </summary>
    public void Hide() {

    }

    /// <summary>
    /// Call when clicked to enter investigation mode
    /// </summary>
    public void Click() {
        signalInvokeClick?.Invoke(this);
    }
}
