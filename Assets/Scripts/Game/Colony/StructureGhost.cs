using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureGhost : MonoBehaviour {
    [SerializeField]
    Bounds _placementBounds = new Bounds(Vector3.zero, Vector3.one);
    [SerializeField]
    M8.SpriteColorGroup _spriteColorGroup;
    [SerializeField]
    Color _invalidColor;

    public Bounds placementBounds { get { return _placementBounds; } }

    public bool isInvalid {
        get { return mIsInvalid; }
        set {
            if(mIsInvalid != value) {
                mIsInvalid = value;
                if(mIsInvalid) {
                    if(_spriteColorGroup) _spriteColorGroup.ApplyColor(_invalidColor);
                }
                else {
                    if(_spriteColorGroup) _spriteColorGroup.Revert();
                }
            }
        }
    }

    private bool mIsInvalid;

    void OnDisable() {
        isInvalid = false;
    }

    void OnDrawGizmos() {
        Gizmos.color = GameData.instance.structurePlacementBoundsColor;
        Gizmos.DrawWireCube(transform.position + _placementBounds.center, _placementBounds.size);
    }
}
