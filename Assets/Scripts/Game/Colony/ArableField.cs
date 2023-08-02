using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArableField : MonoBehaviour {
    [Header("Info")]
    [SerializeField]
    int _healthCount = 3;
    [SerializeField]
    float _healthDecayDelay = 10f;

    [SerializeField]
    [M8.TagSelector]
    string _plantTag;

    [SerializeField]
    Bounds _plantCheckBounds;

    [SerializeField]
    float _plantHurtDelay = 1f;

    [Header("Display")]
    public GameObject vegetationActiveGO;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takeGrow;

    public Vector2 position {
        get {
            return transform.position + _plantCheckBounds.center;
        }

        set {
            transform.position = value;
        }
    }

    public int health {
        get { return mHealthCurCount; }
        set {
            var _val = Mathf.Clamp(value, 0, _healthCount);
            if(mHealthCurCount != _val) {
                mHealthCurCount = _val;
                mDecayCurTime = 0f;
                mPlantHurtCurTime = 0f;

                ApplyAvailability();
                RefreshDisplay();
            }
        }
    }

    public int healthMax { get { return _healthCount; } }

    public bool isHealthFull { get { return mHealthCurCount >= _healthCount; } }

    public bool isMarked { get { return mMarkCount > 0; } }

    public static M8.CacheList<ArableField> arableFieldAvailable { get { return mArableFieldAvailable; } }

    private static M8.CacheList<ArableField> mArableFieldAvailable = new M8.CacheList<ArableField>(8);

    private int mTakeGrowInd;
    private float mTakeGrowTotalTime;

    private int mHealthCurCount;
    private float mDecayCurTime;

    private float mPlantHurtCurTime;

    private Collider2D[] mCollChecks = new Collider2D[8];

    private bool mIsAvailableAdded = false;

    private int mMarkCount = 0;

    public void AddMark() { mMarkCount++; }
    public void RemoveMark() { if(mMarkCount > 0) mMarkCount--; }

    void OnDisable() {
        mMarkCount = 0;

        if(mIsAvailableAdded) {
            mArableFieldAvailable.Remove(this);
            mIsAvailableAdded = false;
        }
    }

    void OnEnable() {
        mHealthCurCount = 0;
        mDecayCurTime = 0f;
        mPlantHurtCurTime = 0f;

        ApplyAvailability();
        RefreshDisplay();
    }

    void Awake() {
        if(animator) {
            mTakeGrowInd = animator.GetTakeIndex(takeGrow);

            if(mTakeGrowInd != -1)
                mTakeGrowTotalTime = animator.GetTakeTotalTime(mTakeGrowInd);
        }
    }

    void Update() {
        if(mHealthCurCount > 0) {
            var cycleCtrl = ColonyController.instance.cycleController;

            mDecayCurTime += Time.deltaTime * cycleCtrl.cycleTimeScale;
            if(mDecayCurTime >= _healthDecayDelay) {
                if(mHealthCurCount > 0) {
                    mHealthCurCount--;
                    ApplyAvailability();
                }

                mDecayCurTime = 0f;
            }

            RefreshDisplay();
        }
        else { //check for plants
            mPlantHurtCurTime += Time.deltaTime;
            if(mPlantHurtCurTime >= _plantHurtDelay) {
                int checkCount = Physics2D.OverlapBoxNonAlloc(transform.position + _plantCheckBounds.center, _plantCheckBounds.size, 0f, mCollChecks);
                for(int i = 0; i < checkCount; i++) {
                    var coll = mCollChecks[i];
                    if(coll.CompareTag(_plantTag)) {
                        var plant = coll.GetComponent<StructurePlant>();
                        if(plant)
                            plant.hitpointsCurrent--;
                    }
                }

                mPlantHurtCurTime = 0f;
            }
        }
    }

    private void ApplyAvailability() {
        var _available = mHealthCurCount < _healthCount;

        if(mIsAvailableAdded != _available) {
            mIsAvailableAdded = _available;

            if(_available) {
                if(mArableFieldAvailable.IsFull)
                    mArableFieldAvailable.Expand();

                mArableFieldAvailable.Add(this);
            }
            else
                mArableFieldAvailable.Remove(this);
        }
    }

    private void RefreshDisplay() {
        if(mTakeGrowInd != -1) {
            float growScale;

            if(_healthCount > 0) {
                var fHealthCurCount = (float)(mHealthCurCount - 1);
                var fHealthCount = (float)_healthCount;

                growScale = Mathf.Clamp01((fHealthCurCount / fHealthCount) + (1f / fHealthCount) * ((_healthDecayDelay - mDecayCurTime) / _healthDecayDelay));
            }
            else
                growScale = 0f;

            animator.Goto(mTakeGrowInd, mTakeGrowTotalTime * growScale);
        }

        if(vegetationActiveGO) vegetationActiveGO.SetActive(mHealthCurCount > 0);
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + _plantCheckBounds.center, _plantCheckBounds.size);
    }
}
