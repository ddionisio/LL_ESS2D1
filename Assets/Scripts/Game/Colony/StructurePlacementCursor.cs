using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructurePlacementCursor : MonoBehaviour {

    [Header("Highlight Display")]
    public Transform highlightRoot;
    public SpriteRenderer highlightRender;
    public Transform highlightSideLeftRoot;
    public Transform highlightSideRightRoot;
    public M8.SpriteColorGroup highlightColorGroup;
    public Color highlightColorValid;
    public Color highlightColorInvalid;

    [Header("Ground Display")]
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

                        positionGround = grdPt.position;
                        normalGround = grdPt.up;
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
                if(highlightRender) {
                    var s = highlightRender.size;

                    s.x = mWidth;

                    highlightRender.size = s;
                }

                var extX = mWidth * 0.5f;

                if(highlightSideLeftRoot) {
                    var p = highlightSideLeftRoot.localPosition;
                    p.x = -extX;
                    highlightSideLeftRoot.localPosition = p;
                }

                if(highlightSideRightRoot) {
                    var p = highlightSideRightRoot.localPosition;
                    p.x = extX;
                    highlightSideRightRoot.localPosition = p;
                }
            }
        }
    }

    public bool isValid { 
        get { return mIsValid; }
        set {
            if(mIsValid != value) {
                mIsValid = value;
                ApplyValidDisplay();
            }
        }
    }

    private float mWidth;
    private Vector2 mPosition;

    private bool mIsValid;

    void Awake() {
        ApplyValidDisplay();
    }

    private void ApplyValidDisplay() {
        if(highlightColorGroup)
            highlightColorGroup.ApplyColor(mIsValid ? highlightColorValid : highlightColorInvalid);
    }
}
