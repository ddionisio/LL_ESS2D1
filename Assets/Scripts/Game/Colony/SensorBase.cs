using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SensorBase : MonoBehaviour {
    public enum CheckType {
        None,
        Point,
        Box,
        Circle
    }

    public CheckType checkType;
    [M8.TagSelector]
    public string checkTagFilter;
    public LayerMask checkLayermask;
    public Vector2 checkOffset;
    public Vector2 checkSize;
    public float checkDelay = 0.3f;
    public int checkCapacity = 8;

    private Collider2D[] mColls;
    private float mLastTime;

    /// <summary>
    /// Process given overlapped collision, return true to continue processing other overlaps, false to stop the entire process.
    /// </summary>
    protected abstract bool Process(Collider2D coll);

    void OnEnable() {
        mLastTime = Time.time;
    }

    void Awake() {
        mColls = new Collider2D[checkCapacity];
    }

    void Update() {
        if(Time.time - mLastTime >= checkDelay) {
            Vector2 pos = (Vector2)transform.position + checkOffset;
            int overlapCount = 0;

            switch(checkType) {
                case CheckType.Point:
                    overlapCount = Physics2D.OverlapPointNonAlloc(pos, mColls, checkLayermask);
                    break;
                case CheckType.Box:
                    overlapCount = Physics2D.OverlapBoxNonAlloc(pos, checkSize, 0f, mColls, checkLayermask);
                    break;
                case CheckType.Circle:
                    overlapCount = Physics2D.OverlapCircleNonAlloc(pos, checkSize.x, mColls, checkLayermask);
                    break;
            }

            for(int i = 0; i < overlapCount; i++) {
                var coll = mColls[i];
                if(coll.gameObject != gameObject && (string.IsNullOrEmpty(checkTagFilter) || coll.CompareTag(checkTagFilter))) {
                    if(!Process(coll))
                        break;
                }
            }

            mLastTime = Time.time;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.yellow;

        Vector3 ofs = checkOffset;

        switch(checkType) {
            case CheckType.Point:
                Gizmos.DrawSphere(transform.position + ofs, 0.1f);
                break;
            case CheckType.Box:
                Gizmos.DrawWireCube(transform.position + ofs, checkSize);
                break;
            case CheckType.Circle:
                Gizmos.DrawWireSphere(transform.position + ofs, checkSize.x);
                break;
        }
    }
}
