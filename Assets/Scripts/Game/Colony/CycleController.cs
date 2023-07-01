using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleController : MonoBehaviour {
    [Header("Info")]
    public SeasonData season; //which season this is used for
    public int regionIndex; //which region this is used for
    public CycleData data;

    [Header("Signal Invoke")]
    public SignalCycleInfo signalInvokeCycleBegin;
    public SignalCycleInfo signalInvokeCycleNext;
    public SignalCycleInfo signalInvokeCycleEnd;

    [Header("Signal Listen")]
    public M8.SignalBoolean signalListenPause;

    public AtmosphereStat[] atmosphereStats { get; private set; }

    private AtmosphereModifier[] mSeasonAtmosphereMods;
    private AtmosphereModifier[] mRegionAtmosphereMods;

    public int cycleCurIndex { get; private set; }

    public void Setup(HotspotData hotspotData) {

    }

    public void Begin() {

    }
}
