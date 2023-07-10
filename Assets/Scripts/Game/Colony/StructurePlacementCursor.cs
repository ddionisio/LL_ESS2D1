using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructurePlacementCursor : MonoBehaviour {

    public Transform highlightRoot;
    public Transform groundRoot; //set to a position on ground

    public bool active {
        get { return gameObject.activeSelf; }
        set { gameObject.SetActive(value); }
    }

    public Vector2 position { 
        get { return mPosition; }
        set {
            if(mPosition != value) {
                mPosition = value;

                //update highlight position
                if(highlightRoot) {
                    //simply update the x position
                    var highlightPos = highlightRoot.position;
                    highlightPos.x = mPosition.x;

                    highlightRoot.position = highlightPos;
                }

                //update ground position/normal
                if(groundRoot) {
                    GroundPoint grdPt;

                    if(GroundPoint.GetGroundPoint(mPosition, out grdPt)) {
                        groundRoot.gameObject.SetActive(true);

                        groundRoot.position = grdPt.position;
                    }
                    else {
                        groundRoot.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public Vector2 positionGround { get; private set; }
    public Vector2 normalGround { get; private set; }

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
}
