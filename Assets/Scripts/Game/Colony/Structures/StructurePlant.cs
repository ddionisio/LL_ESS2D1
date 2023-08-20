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
        public bool isActive { get { return mGrowth.isBlossomed; } }
        public bool isBusy { get { return mRout != null; } }

        public Vector2 topPosition { get { return mGrowth.topPosition; } }

        private StructurePlantGrowth mGrowth;
        private Transform mBlossomTrans;
        private Vector2 mBlossomDefaultPos;
        private Coroutine mRout;

        public BloomItem(StructurePlantGrowth growth) {
            mGrowth = growth;

            mBlossomTrans = mGrowth.blossomTransform;
            if(mBlossomTrans)
                mBlossomDefaultPos = mBlossomTrans.localPosition;
        }

        public void Grab(MonoBehaviour root, Transform destTrans, float height, float delay) {
            if(mBlossomTrans)
                mRout = root.StartCoroutine(DoGrab(destTrans, height, delay));
        }

        public void GrabCancel(MonoBehaviour root) {
            if(mRout != null) {
                root.StopCoroutine(mRout);
                mRout = null;
            }

            if(mBlossomTrans) mBlossomTrans.localPosition = mBlossomDefaultPos;
        }

        public void Clear(MonoBehaviour root) {
            GrabCancel(root);

            mGrowth.HideBlossom();
        }

        IEnumerator DoGrab(Transform destTrans, float height, float delay) {
            Vector2 from = mBlossomTrans.position, to = destTrans.position;

            var topY = Mathf.Max(from.y, to.y);

            var midPoint = new Vector2(Mathf.Lerp(from.x, to.x, 0.5f), topY + height);

            var curTime = 0f;
            while(curTime < delay) {
                yield return null;

                curTime += Time.deltaTime;

                var t = Mathf.Clamp01(curTime / delay);

                mBlossomTrans.position = M8.MathUtil.Bezier(from, midPoint, to, t);
            }

            mGrowth.HideBlossom();

            mRout = null;
        }
    }

    [Header("Bloom Info")]
    public StructurePlantGrowth[] blooms; //determines number of resources that can be collected
    public float bloomGrabDelay;
    public M8.RangeFloat bloomGrabHeightRange;

    [Header("Growth Display")]
    public GameObject growthRootGO;

    [Header("Growth Animation")]
    public M8.Animator.Animate growthAnimator;
    [M8.Animator.TakeSelector(animatorField = "growthAnimator")]
    public int growthReadyTake = -1;
    [M8.Animator.TakeSelector(animatorField = "growthAnimator")]
    public int growthDecayTake = -1;

    public GrowthState growthState { get; private set; }

    public int bloomCount { get { return blooms.Length; } }

    private float mReadyTime; //time before ready to grow
    private float mBloomTime;
    private float mBloomTimeScale;

    private BloomItem[] mBloomItems;

    public int BloomGrabAvailableIndex() {
        for(int i = 0; i < mBloomItems.Length; i++) {
            var itm = mBloomItems[i];
            if(itm.isActive && !itm.isBusy)
                return i;
        }

        return -1;
    }
        
    public void BloomClear(int bloomIndex) {
        if(bloomIndex >= 0 && bloomIndex < mBloomItems.Length)
            mBloomItems[bloomIndex].Clear(this);
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

    public bool BloomIsAvailable(int bloomIndex) {
        if(bloomIndex >= 0 && bloomIndex < mBloomItems.Length) {
            var itm = mBloomItems[bloomIndex];
            return itm.isActive && !itm.isBusy;
        }

        return false;
    }

    public Vector2 BloomPosition(int bloomIndex) {
        if(bloomIndex >= 0 && bloomIndex < mBloomItems.Length) {
            var itm = mBloomItems[bloomIndex];
            return itm.topPosition;
        }

        return position;
    }

    public override void WorkAdd() {
        base.WorkAdd();

        //refresh growth scale
        if(growthState == GrowthState.Growing)
            ApplyGrowthScale();
    }

    public override void WorkRemove() {
        base.WorkRemove();

        //refresh growth scale
        if(growthState == GrowthState.Growing)
            ApplyGrowthScale();
    }

    protected override void Init() {
        //initial state
        growthState = GrowthState.None;

        if(growthRootGO) growthRootGO.SetActive(false);
                
        //setup blooms
        mBloomItems = new BloomItem[blooms.Length];
        for(int i = 0; i < blooms.Length; i++) {
            var bloom = blooms[i];
            bloom.Init();

            mBloomItems[i] = new BloomItem(bloom);
        }
    }

    protected override void Spawned() {
        var plantDat = data as StructurePlantData;

        GameData.instance.signalCycleNext.callback += OnCycleNext;
    }

    protected override void Despawned() {
        GameData.instance.signalCycleNext.callback -= OnCycleNext;
    }

    protected override void ClearCurrentState() {
        base.ClearCurrentState();

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
            ApplyGrowthScale();
    }

    IEnumerator DoActive() {
        var plantDat = data as StructurePlantData;

        var growthDelay = plantDat.growthDelay;
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

                    if(growthReadyTake != -1)
                        yield return growthAnimator.PlayWait(growthReadyTake);

                    growthState = GrowthState.Growing;
                    ApplyCurrentGrowthState();
                    break;

                case GrowthState.Growing:
                    if(mBloomTime < growthDelay) {
                        mBloomTime += Time.deltaTime * mBloomTimeScale;

                        var t = Mathf.Clamp01(mBloomTime / growthDelay);

                        for(int i = 0; i < blooms.Length; i++)
                            blooms[i].ApplyGrowth(t);
                    }
                    else {
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
                    if(growthDecayTake != -1)
                        yield return growthAnimator.PlayWait(growthDecayTake);

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
                mBloomTime = 0f;
                mBloomTimeScale = 0f;

                //hide blooms
                for(int i = 0; i < mBloomItems.Length; i++)
                    mBloomItems[i].Clear(this);

                //reset growth
                for(int i = 0; i < blooms.Length; i++)
                    blooms[i].ApplyGrowth(0f);

                if(growthRootGO) growthRootGO.SetActive(false);
                                
                if(growthAnimator)
                    growthAnimator.Stop();

                SetStatusState(StructureStatus.Growth, StructureStatusState.None);
                break;

            case GrowthState.Growing:
                ApplyGrowthScale();
                break;

            case GrowthState.Bloom:
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

    private void ApplyGrowthScale() {
        if(!growthAnimator) return;

        mBloomTimeScale = 0f;

        //add atmosphere attribute
        var resScale = ColonyController.instance.cycleController.GetResourceScale(CycleResourceType.Growth);
        mBloomTimeScale += resScale;

        //add worker attribute
        mBloomTimeScale += workCount * GameData.instance.growthScalePerWork;

        if(mBloomTimeScale < 0f)
            mBloomTimeScale = 0f;

        if(mBloomTimeScale > 0f)
            SetStatusState(StructureStatus.Growth, StructureStatusState.Progress);
        else
            SetStatusState(StructureStatus.Growth, StructureStatusState.Require);
    }
}
