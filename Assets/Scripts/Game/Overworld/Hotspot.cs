using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class Hotspot : MonoBehaviour {
    [Header("Data")]
    public HotspotData data;

    public event System.Action<Hotspot> investigateCallback;

    /// <summary>
    /// Call when clicked to enter investigation mode
    /// </summary>
    public void Investigate() {
        investigateCallback?.Invoke(this);
    }
}
