using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableAirWave : MovableBase {
    [Header("Wave Info")]
    [Tooltip("Distance to make full wave")]
    public float waveDistance;
    public M8.RangeFloat heightRange;
        
    private float mSinHeight;
    private float mSinLength;
    private float mSinPIShift;
    private uint mSinPIShiftCounter;

    public override void Cancel() {
        base.Cancel();

        mSinPIShiftCounter = 0;
    }

    protected override float MoveInit(Vector2 from, Vector2 to) {
        var horzDist = Mathf.Abs(from.x - to.x);

        float waveCount = waveDistance > 0f ? Mathf.Floor(horzDist / waveDistance) : 1f;
        if(waveCount < 1f)
            waveCount = 1f;

        mSinLength = waveCount * Mathf.PI;

        mSinHeight = heightRange.random;

        mSinPIShift = Mathf.PI * mSinPIShiftCounter;
        mSinPIShiftCounter += (uint)waveCount;

        var waveEnd = Vector2.Lerp(from, to, Mathf.Clamp01((horzDist / waveCount) / horzDist));

        var mid = Vector2.Lerp(from, waveEnd, 0.5f);
        mid.y += mSinHeight;

        return ((from - mid).magnitude + (mid - waveEnd).magnitude) * waveCount; //approx
    }

    protected override Vector2 MoveUpdate(Vector2 from, Vector2 to, float t) {
        return new Vector2(Mathf.Lerp(from.x, to.x, t), Mathf.Lerp(from.y, to.y, t) + mSinHeight * Mathf.Sin(t * mSinLength + mSinPIShift));
    }
}
