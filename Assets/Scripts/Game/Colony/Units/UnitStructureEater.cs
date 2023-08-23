using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStructureEater : UnitTargetStructure {
    [System.Serializable]
    public struct SegmentInfo {
        public GameObject rootGO;
        public Transform left; //horizontally reposition based on target structure's width
        public Transform right; //horizontally reposition based on target structure's width

        public bool active { get { return rootGO ? rootGO.activeSelf : false; } set { if(rootGO) rootGO.SetActive(value); } }

        public void ApplyWidth(float width) {
            var ext = width * 0.5f;

            if(left) {
                var pos = left.localPosition;
                pos.x = -ext;
                left.localPosition = pos;
            }

            if(right) {
                var pos = right.localPosition;
                pos.x = ext;
                right.localPosition = pos;
            }
        }

        public void GroundSides() {
            GroundPoint grdPt;

            if(GroundPoint.GetGroundPoint(left.position.x, out grdPt))
                left.position = grdPt.position;

            if(GroundPoint.GetGroundPoint(right.position.x, out grdPt))
                right.position = grdPt.position;
        }
    }

    [Header("Segments Display")]
    public SegmentInfo[] segments;

    protected override void TargetChanged() {
        ApplyTargetDimension();
    }

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case UnitState.Idle:
                mRout = StartCoroutine(DoIdle());
                break;

            case UnitState.Act:
                mRout = StartCoroutine(DoAttack());
                break;

            case UnitState.None:
                RefreshSegmentDisplay(); //hp set to 0 from base class
                break;
        }
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Idle:
            case UnitState.Act:
                //check if target is still viable
                if(!targetStructure || targetStructure.state == StructureState.None || targetStructure.state == StructureState.Moving || (targetStructure.state == StructureState.Destroyed && !targetStructure.isReparable)) {
                    targetStructure = null;

                    hitpointsCurrent = 0; //destroy self
                }
                break;
        }
    }

    protected override void Spawned(M8.GenericParams parms) {
        base.Spawned(parms);

        ApplyTargetDimension();
        RefreshSegmentDisplay();
    }

    protected override void HitpointsChanged(int previousHitpoints) {
        RefreshSegmentDisplay();

        base.HitpointsChanged(previousHitpoints);
    }

    IEnumerator DoIdle() {
        var eaterData = data as UnitStructureEaterData;
        if(!eaterData) {
            mRout = null;
            yield break;
        }

        if(hitpointsCurrent < hitpointsMax) {
            while(hitpointsCurrent < hitpointsMax) {
                RestartStateTime();
                while(stateTimeElapsed < eaterData.growthDelay)
                    yield return null;

                hitpointsCurrent++;
            }
        }
        else {
            while(stateTimeElapsed < eaterData.attackDelay)
                yield return null;
        }

        mRout = null;
        state = UnitState.Act;
    }

    IEnumerator DoAttack() {
        if(takeAct != -1) {
            while(animator.isPlaying)
                yield return null;
        }
        else
            yield return null;

        //damage target
        if(targetStructure.isDamageable)
            targetStructure.hitpointsCurrent--;

        mRout = null;
        state = UnitState.Idle;
    }

    private void RefreshSegmentDisplay() {
        var hitpointsScale = hitpointsMax > 0 ? Mathf.Clamp01((float)hitpointsCurrent / hitpointsMax) : 0f;

        var segmentActiveMax = Mathf.RoundToInt(segments.Length * hitpointsScale) - 1;

        for(int i = 0; i < segments.Length; i++)
            segments[i].active = i <= segmentActiveMax;
    }

    private void ApplyTargetDimension() {
        if(!targetStructure)
            return;

        var width = targetStructure.boxCollider.size.x;

        if(boxCollider) {
            var size = boxCollider.size;
            size.x = width;
            boxCollider.size = size;
        }

        for(int i = 0; i < segments.Length; i++) {
            segments[i].ApplyWidth(width);
            segments[i].GroundSides();
        }
    }
}
