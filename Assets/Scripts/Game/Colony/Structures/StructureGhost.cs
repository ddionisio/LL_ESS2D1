using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureGhost : MonoBehaviour {
    [SerializeField]
    Bounds _placementBounds = new Bounds(Vector3.zero, Vector3.one);

    public Bounds placementBounds { get { return _placementBounds; } }

    void OnDrawGizmos() {
        Gizmos.color = GameData.instance.structurePlacementBoundsColor;
        Gizmos.DrawWireCube(transform.position + _placementBounds.center, _placementBounds.size);
    }
}
