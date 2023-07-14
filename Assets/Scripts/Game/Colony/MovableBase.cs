using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovableBase : MonoBehaviour {
    [Header("Move Info")]
    public float moveSpeed = 10f;
    public DG.Tweening.Ease moveEase = DG.Tweening.Ease.InOutSine;

    public bool isMoving { get { return mRout != null; } }

    public bool isLocked { get; set; }

    private Coroutine mRout;

    private DG.Tweening.EaseFunction mEaseFunc;

    public void Move(Vector2 dest) {
        Cancel();

        mRout = StartCoroutine(DoMove(dest));
    }

    public void Cancel() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }

    /// <summary>
    /// Return distance
    /// </summary>
    protected abstract float MoveStart(Vector2 from, Vector2 to);

    protected abstract Vector2 MoveUpdate(Vector2 from, Vector2 to, float t);

    void OnDisable() {
        Cancel();
    }

    IEnumerator DoMove(Vector2 dest) {
        if(mEaseFunc == null)
            mEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(moveEase);

        var startPos = (Vector2)transform.position;
                
        if(moveSpeed > 0f) {
            var dist = MoveStart(startPos, dest);

            var moveDelay = dist / moveSpeed;

            var curTime = 0f;
            while(curTime < moveDelay) {
                yield return null;

                curTime += Time.deltaTime;

                var t = mEaseFunc(curTime, moveDelay, 0f, 0f);

                var toPos = MoveUpdate(startPos, dest, t);

                transform.position = toPos;
            }
        }
        else {
            yield return null;

            transform.position = dest;
        }

        mRout = null;
    }
}
