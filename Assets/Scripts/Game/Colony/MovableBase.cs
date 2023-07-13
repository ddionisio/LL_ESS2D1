using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovableBase : MonoBehaviour {
    [Header("Move Info")]
    public float moveDelay = 1f;
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

    protected abstract void MoveStart(Vector2 from, Vector2 to);

    protected abstract Vector2 MoveUpdate(Vector2 from, Vector2 to, float t);

    void OnDisable() {
        Cancel();
    }

    IEnumerator DoMove(Vector2 dest) {
        if(mEaseFunc == null)
            mEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(moveEase);

        var startPos = (Vector2)transform.position;

        MoveStart(startPos, dest);

        var curTime = 0f;
        while(curTime < moveDelay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = mEaseFunc(curTime, moveDelay, 0f, 0f);

            var toPos = MoveUpdate(startPos, dest, t);

            transform.position = toPos;
        }

        mRout = null;
    }
}
