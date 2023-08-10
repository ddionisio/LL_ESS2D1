using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class Hotspot : MonoBehaviour {
    [Header("Data")]
    public HotspotData data;

    [Header("Display")]
    public GameObject rootGO;

    [Header("Ping Info")]
    public float revealRadius;
    public float pingRadius;

    [Header("Signal Invoke")]
    public SignalHotspot signalInvokeClick;

    public Vector2 position { get { return transform.position; } }

    public bool isBusy { get { return false; } } //wait for animation

    /// <summary>
    /// Call when clicked to enter investigation mode
    /// </summary>
    public void Click() {
        signalInvokeClick?.Invoke(this);
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(position, revealRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, pingRadius);
    }
}
