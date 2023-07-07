using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructurePlacementCursor : MonoBehaviour {

    public bool active {
        get { return gameObject.activeSelf; }
        set { gameObject.SetActive(value); }
    }

    public Vector2 position { 
        get { return mPosition; }
        set {
            if(mPosition != value) {
                mPosition = value;

                //update ground position

                //set position of self
            }
        }
    }

    public Vector2 positionGround { get { return mPositionGround; } }

    public float width { 
        get { return mWidth; } 
        set {
            if(mWidth != value) {
                mWidth = value;

                //setup dimensions
            }
        }
    }

    private float mWidth;
    private Vector2 mPosition;
    private Vector2 mPositionGround;

    public void Show() {

    }

    public void Hide() {

    }
}
