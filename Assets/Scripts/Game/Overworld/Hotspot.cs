using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class Hotspot : MonoBehaviour {
    [Header("Data")]
    public HotspotData data;

    public event System.Action<Hotspot> clickCallback;

    /// <summary>
    /// Call when clicked to enter investigation mode
    /// </summary>
    public void Click() {
        clickCallback?.Invoke(this);
    }
}
