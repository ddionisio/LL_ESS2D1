using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructurePlant : Structure {
    public enum GrowthState {
        None,
        Growing,
        Bloom,
        Decay
    }

    public class BloomItem {
        public Transform transform { get; private set; }

        public bool isActive { get { return mGO ? mGO.activeSelf : false; } }
        public bool isBusy { get { return mRout != null; } }

        private GameObject mGO;
        private Vector2 mDefaultLocalPos;
        private Coroutine mRout;

        public BloomItem(Transform t) {
            transform = t;

            if(t) {
                mGO = t.gameObject;
                mGO.SetActive(false);

                mDefaultLocalPos = t.localPosition;
            }
        }

        public void Active() {
            if(transform)
                transform.localPosition = mDefaultLocalPos;

            if(mGO)
                mGO.SetActive(true);
        }

        public void Grab(MonoBehaviour root, Transform destTrans, float height, float delay) {
            if(transform)
                mRout = root.StartCoroutine(DoGrab(destTrans, height, delay));
        }

        public void GrabCancel(MonoBehaviour root) {
            if(mRout != null) {
                root.StopCoroutine(mRout);
                mRout = null;
            }

            if(transform)
                transform.localPosition = mDefaultLocalPos;
        }

        public void Clear(MonoBehaviour root) {
            GrabCancel(root);

            if(mGO)
                mGO.SetActive(false);
        }

        IEnumerator DoGrab(Transform destTrans, float height, float delay) {
            Vector2 from = transform.position, to = destTrans.position;

            var topY = Mathf.Max(from.y, to.y);

            var midPoint = new Vector2(Mathf.Lerp(from.x, to.x, 0.5f), topY + height);

            var curTime = 0f;
            while(curTime < delay) {
                yield return null;

                curTime += Time.deltaTime;

                var t = Mathf.Clamp01(curTime / delay);

                transform.position = M8.MathUtil.Bezier(from, midPoint, to, t);
            }

            if(mGO)
                mGO.SetActive(false);

            mRout = null;
        }
    }

    [Header("Bloom Info")]
    public Transform[] bloomRoots; //determines number of resources that can be collected
    public float bloomGrabDelay;
    public M8.RangeFloat bloomGrabHeightRange;

    [Header("Growth Display")]
    public GameObject growthRootGO;

    [Header("Growth Animation")]
    public M8.Animator.Animate growthAnimator;
    [M8.Animator.TakeSelector(animatorField = "growthAnimator")]
    public string growthReadyTake;
    [M8.Animator.TakeSelector(animatorField = "growthAnimator")]
    public string growthTake;
    [M8.Animator.TakeSelector(animatorField = "growthAnimator")]
    public string growthDecayTake;

    public GrowthState growthState { get; private set; }

    private float mReadyTime; //time before ready to grow

    private float mGrowthAnimScaleBase; //base growth animation
    private float mGrowthAnimTotalTime;

    private int mGrowthReadyTakeInd = -1;
    private int mGrowthTakeInd = -1;
    private int mGrowthDecayTakeInd = -1;

    private BloomItem[] mBloomItems;

    public int BloomGrabAvailableIndex() {
        for(int i = 0; i < mBloomItems.Length; i++) {
            var itm = mBloomItems[i];
            if(itm.isActive && !itm.isBusy)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Returns bloom index, -1 if no blooms to remove
    /// </summary>
    public int BloomClear() {
        //ignore busy blooms (being grabbed by someone else)
        for(int i = 0; i < mBloomItems.Length; i++) {
            var itm = mBloomItems[i];
            if(itm.isActive && !itm.isBusy) {
                itm.Clear(this);
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns bloom index, -1 if no blooms available. Use this index to check its state (isBusy)
    /// </summary>
    public int BloomGrab(Transform dest) {
        for(int i = 0; i < mBloomItems.Length; i++) {
            var itm = mBloomItems[i];
            if(itm.isActive && !itm.isBusy) {
                itm.Grab(this, dest, bloomGrabHeightRange.random, bloomGrabDelay);
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Start grab proceedure with given bloom index. Returns true if successfully grabbed.
    /// </summary>
    public bool BloomGrab(int bloomIndex, Transform dest) {
        if(bloomIndex >= 0 && bloomIndex < mBloomItems.Length) {
            var itm = mBloomItems[bloomIndex];
            if(itm.isActive && !itm.isBusy) {
                itm.Grab(this, dest, bloomGrabHeightRange.random, bloomGrabDelay);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Call this after BloomGrab to check its state, if 'false', then do whatever is next.
    /// </summary>
    public bool BloomIsBusy(int bloomIndex) {
        if(bloomIndex >= 0 && bloomIndex < mBloomItems.Length)
            return mBloomItems[bloomIndex].isBusy;

        return false;
    }

    public void BloomGrabCancel(int bloomIndex) {
        if(bloomIndex >= 0 && bloomIndex < mBloomItems.Length)
            mBloomItems[bloomIndex].GrabCancel(this);
    }

    public override void WorkAdd() {
        base.WorkAdd();

        //refresh growth scale
        if(growthState == GrowthState.Growing)
            ApplyGrowthAnimScale();
    }

    public override void WorkRemove() {
        base.WorkRemove();

        //refresh growth scale
        if(growthState == GrowthState.Growing)
            ApplyGrowthAnimScale();
    }

    protected override void Init() {
        //initial state
        growthState = GrowthState.None;

        if(growthRootGO) growthRootGO.SetActive(false);

        mReadyTime = 0f;

        //setup anim
        if(growthAnimator) {
            mGrowthReadyTakeInd = growthAnimator.GetTakeIndex(growthReadyTake);
            mGrowthTakeInd = growthAnimator.GetTakeIndex(growthTake);
            mGrowthDecayTakeInd = growthAnimator.GetTakeIndex(growthDecayTake);
        }
                
        //setup blooms
        mBloomItems = new BloomItem[bloomRoots.Length];
        for(int i = 0; i < bloomRoots.Length; i++)
            mBloomItems[i] = new BloomItem(bloomRoots[i]);
    }

    protected override void Spawned() {
        var plantDat = data as StructurePlantData;

        //setup base growth scale
        if(mGrowthTakeInd != -1) {
            mGrowthAnimTotalTime = growthAnimator.GetTakeTotalTime(mGrowthTakeInd);

            mGrowthAnimScaleBase = plantDat.growthDelay > 0f ? mGrowthAnimTotalTime / plantDat.growthDelay : 1f;
        }

        GameData.instance.signalCycleNext.callback += OnCycleNext;
    }

    protected override void Despawned() {
        GameData.instance.signalCycleNext.callback -= OnCycleNext;
    }

    protected override void ClearCurrentState() {
        base.ClearCurrentState();

        switch(state) {
            case StructureState.Active:
                switch(growthState) {
                    case GrowthState.Growing:
                        if(mGrowthTakeInd != -1)
                            growthAnimator.Pause();
                        break;
                }
                break;
        }
    }

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case StructureState.Spawning:
                //do seed toss from colony ship to our location
                break;

            case StructureState.Active:
                if(growthState == GrowthState.Growing) //resume growth
                    ApplyCurrentGrowthState();

                mRout = StartCoroutine(DoActive());
                break;

            case StructureState.Moving: //just in case we make this movable
            case StructureState.Destroyed:
            case StructureState.Demolish:
            case StructureState.None:
                growthState = GrowthState.None;
                ApplyCurrentGrowthState();
                break;
        }
    }

    void OnCycleNext() {
        //refresh growth scale
        if(growthState == GrowthState.Growing)
            ApplyGrowthAnimScale();
    }

    IEnumerator DoActive() {
        var plantDat = data as StructurePlantData;

        var readyDelay = plantDat.readyDelay;

        while(true) {
            yield return null;

            switch(growthState) {
                case GrowthState.None:
                    while(mReadyTime < readyDelay) {
                        yield return null;
                        mReadyTime += Time.deltaTime;
                    }

                    if(growthRootGO) growthRootGO.SetActive(true);

                    if(mGrowthReadyTakeInd != -1)
                        yield return growthAnimator.PlayWait(mGrowthReadyTakeInd);

                    growthState = GrowthState.Growing;
                    ApplyCurrentGrowthState();
                    break;

                case GrowthState.Growing:
                    if(mGrowthTakeInd != -1) {
                        while(growthAnimator.isPlaying) {
                            SetStatusProgress(StructureStatus.Growth, Mathf.Clamp01(growthAnimator.runningTime / mGrowthAnimTotalTime));
                            yield return null;
                        }

                        growthState = GrowthState.Bloom;
                        ApplyCurrentGrowthState();
                    }
                    else { //fail-safe
                        growthState = GrowthState.Bloom;
                        ApplyCurrentGrowthState();
                    }
                    break;

                case GrowthState.Bloom:                    
                    while(true) {
                        int bloomActive = 0;
                        for(int i = 0; i < mBloomItems.Length; i++) {
                            if(mBloomItems[i].isActive)
                                bloomActive++;
                        }

                        if(bloomActive == 0)
                            break;

                        yield return null;
                    }

                    growthState = GrowthState.Decay;
                    ApplyCurrentGrowthState();
                    break;

                case GrowthState.Decay:
                    if(mGrowthDecayTakeInd != -1)
                        yield return growthAnimator.PlayWait(mGrowthDecayTakeInd);

                    //regrow
                    growthState = GrowthState.None;
                    ApplyCurrentGrowthState();
                    break;
            }
        }
    }

    private void ApplyCurrentGrowthState() {
        switch(growthState) {
            case GrowthState.None:
                mReadyTime = 0f;

                //hide blooms
                for(int i = 0; i < mBloomItems.Length; i++)
                    mBloomItems[i].Clear(this);

                //reset growth
                if(growthRootGO) growthRootGO.SetActive(false);

                if(growthAnimator) {
                    growthAnimator.Stop();
                    growthAnimator.animScale = 1f;
                }

                SetStatusState(StructureStatus.Growth, StructureStatusState.None);
                break;

            case GrowthState.Growing:
                if(mGrowthTakeInd != -1) {
                    ApplyGrowthAnimScale();

                    if(growthAnimator.currentPlayingTakeIndex == mGrowthTakeInd)
                        growthAnimator.Resume();
                    else
                        growthAnimator.Play(mGrowthTakeInd);
                }
                break;

            case GrowthState.Bloom:
                //show blooms
                for(int i = 0; i < mBloomItems.Length; i++)
                    mBloomItems[i].Active();

                //choose sprite?

                SetStatusState(StructureStatus.Growth, StructureStatusState.None);
                break;

            case GrowthState.Decay:
                //hide blooms
                for(int i = 0; i < mBloomItems.Length; i++)
                    mBloomItems[i].Clear(this);
                break;
        }
    }

    private void ApplyGrowthAnimScale() {
        if(!growthAnimator) return;

        var scale = 0f;

        //add atmosphere attribute
        var resRate = ColonyController.instance.cycleController.GetResourceRate(CycleResourceType.Growth);
        scale += resRate;

        //add worker attribute
        scale += workCount * GameData.instance.growthScalePerWork;

        if(scale < 0f)
            scale = 0f;

        growthAnimator.animScale = mGrowthAnimScaleBase * scale;

        if(scale > 0f)
            SetStatusState(StructureStatus.Growth, StructureStatusState.Progress);
        else
            SetStatusState(StructureStatus.Growth, StructureStatusState.Require);
    }
}
