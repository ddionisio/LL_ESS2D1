using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldBounds : MonoBehaviour {

    public BoxCollider2D boxCollider {
        get {
            if(!mBoxColl)
                mBoxColl = GetComponent<BoxCollider2D>();
            return mBoxColl;
        }
    }

    /// <summary>
    /// In world space
    /// </summary>
    public Vector2 center {
        get {
            var boxColl = boxCollider;

            return boxColl ? transform.localToWorldMatrix.MultiplyPoint3x4(boxColl.offset) : transform.position;
        }
    }

    /// <summary>
    /// In world space
    /// </summary>
    public Vector2 extents {
        get {
            var boxColl = boxCollider;

            return boxColl ? (Vector2)transform.localToWorldMatrix.MultiplyVector(boxColl.size * 0.5f) : Vector2.zero;
        }
    }

    /// <summary>
    /// In world space
    /// </summary>
    public Vector2 size {
        get {
            var boxColl = boxCollider;

            return boxColl ? (Vector2)transform.localToWorldMatrix.MultiplyVector(boxColl.size) : Vector2.zero;
        }
    }

    public Vector2 centerLocal {
        get {
            var boxColl = boxCollider;
            if(boxColl)
                return (Vector2)transform.position + boxColl.offset;

            return transform.position;
        }
    }

    public Vector2 extentsLocal {
        get {
            var boxColl = boxCollider;

            return boxColl ? boxColl.size * 0.5f : Vector2.zero;
        }
    }

    public Vector2 sizeLocal {
        get {
            var boxColl = boxCollider;

            return boxColl ? boxColl.size : Vector2.zero;
        }
    }

    private BoxCollider2D mBoxColl;
}
