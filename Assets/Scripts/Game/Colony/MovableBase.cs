using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovableBase : MonoBehaviour {
    public enum Facing {
        None,
        Left,
        Right
    }

    [Header("Move Info")]
    [SerializeField]
    float _moveSpeed = 10f; //default move speed
    [SerializeField]
    DG.Tweening.Ease _moveEase = DG.Tweening.Ease.InOutSine;

    /// <summary>
    /// Set move speed, will not change until next call to Move (so make sure to Cancel first).
    /// </summary>
    public float moveSpeed {
        get { return mMoveSpeed; }
        set { mMoveSpeed = value; }
    }

    /// <summary>
    /// Set destination, remember to call Move to actually move. Can be changed while moving (will cancel current, and move again)
    /// </summary>
    public Vector2 moveDestination { 
        get { return mMoveDest; }
        set {
            if(mMoveDest != value) {
                mMoveDest = value;

                if(isMoving) { //cancel current, and move again
                    Cancel();
                    Move();
                }
            }
        }
    }

    public bool isMoving { get { return mRout != null; } }

    public bool isLocked { get; set; }

    public Facing facing { get; private set; }

    private Coroutine mRout;

    private DG.Tweening.EaseFunction mEaseFunc;

    private Vector2 mMoveDest;

    private float mMoveSpeed;

    public void ResetMoveSpeed() {
        mMoveSpeed = _moveSpeed;
    }

    public void Move() {
        Cancel();

        mRout = StartCoroutine(DoMove());
    }

    public virtual void Cancel() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }

    /// <summary>
    /// Return distance
    /// </summary>
    protected abstract float MoveInit(Vector2 from, Vector2 to);

    protected abstract Vector2 MoveUpdate(Vector2 from, Vector2 to, float t);

    void OnDisable() {
        Cancel();
    }

    protected virtual void Awake() {
        mMoveSpeed = _moveSpeed;
    }

    IEnumerator DoMove() {
        if(mEaseFunc == null)
            mEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(_moveEase);

        var startPos = (Vector2)transform.position;
                                
        if(mMoveSpeed > 0f) {
            var dist = MoveInit(startPos, mMoveDest);

            facing = mMoveDest.x - startPos.x < 0f ? Facing.Left : Facing.Right;

            var moveDelay = dist / mMoveSpeed;

            var curTime = 0f;
            while(curTime < moveDelay) {
                yield return null;

                curTime += Time.deltaTime;

                var t = mEaseFunc(curTime, moveDelay, 0f, 0f);

                var lastPos = transform.position;

                var toPos = MoveUpdate(startPos, mMoveDest, t);

                transform.position = toPos;

                /*if(toPos.x > lastPos.x)
                    facing = Facing.Right;
                else if(toPos.x < lastPos.x)
                    facing = Facing.Left;*/
            }
        }
        else {
            yield return null;

            transform.position = mMoveDest;
        }

        mRout = null;
    }
}
