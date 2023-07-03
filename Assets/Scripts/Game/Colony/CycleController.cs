using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleController : MonoBehaviour {
    [System.Serializable]
    public struct CycleItemInfo {
        public SeasonData season; //which season this is used for
        public CycleData data;
    }

    [Header("Info")]    
    public int regionIndex; //which region this is used for    
    public CycleItemInfo[] cycleSeasons;
    public CycleData cycleDataDefault; //if no season match, or we just don't want to deal with season specifics

    [Header("Signal Invoke")]
    public SignalCycleInfo signalInvokeCycleBegin;
    public SignalCycleInfo signalInvokeCycleNext;
    public SignalCycleInfo signalInvokeCycleEnd;

    [Header("Signal Listen")]
    public M8.SignalBoolean signalListenPause;

    public AtmosphereStat[] atmosphereStats { get; private set; }

    public CycleData cycleData { get; private set; }
    public int cycleCurIndex { get; private set; }

    public bool isPause { get; private set; }

    public bool isRunning { get { return mRout != null; } }

    private AtmosphereStat[] mAtmosphereStatsDefault;
    private AtmosphereModifier[] mSeasonAtmosphereMods;
    private AtmosphereModifier[] mRegionAtmosphereMods;

    private CycleInfo mCycleInfo;

    private Coroutine mRout;

    public void Setup(HotspotData hotspotData, SeasonData season) {
        //grab cycle data based on season
        cycleData = cycleDataDefault;
        for(int i = 0; i < cycleSeasons.Length; i++) {
            var cycleSeason = cycleSeasons[i];
            if(cycleSeason.season == season) {
                cycleData = cycleSeason.data;
                break;
            }
        }

        mSeasonAtmosphereMods = hotspotData.climate.GetModifiers(season);

        mRegionAtmosphereMods = hotspotData.GetRegionAtmosphereModifiers(regionIndex);

        mAtmosphereStatsDefault = hotspotData.atmosphereStats;

        atmosphereStats = new AtmosphereStat[mAtmosphereStatsDefault.Length];

        //setup some fixed cycle info
        mCycleInfo = new CycleInfo();
        mCycleInfo.atmosphereStats = atmosphereStats;
        mCycleInfo.daylightScale = hotspotData.GetDaylightScale(season);
        mCycleInfo.cycleDuration = GameData.instance.cycleDuration / cycleData.cycles.Length;
        mCycleInfo.cycleCount = cycleData.cycles.Length;
    }

    public void Begin() {
        if(mRout != null) {
            if(signalInvokeCycleEnd) signalInvokeCycleEnd.Invoke(mCycleInfo);

            StopCoroutine(mRout);
        }

        mRout = StartCoroutine(DoProcess());
    }

    void OnDisable() {
        if(signalListenPause) signalListenPause.callback -= OnPause;
    }

    void OnEnable() {
        if(signalListenPause) signalListenPause.callback += OnPause;
    }

    void OnPause(bool pause) {
        isPause = pause;
    }

    IEnumerator DoProcess() {

        cycleCurIndex = 0;

        ApplyCurrentCycleInfo();

        if(signalInvokeCycleBegin) signalInvokeCycleBegin.Invoke(mCycleInfo);

        var delay = mCycleInfo.cycleDuration;

        while(true) {
            //time pass
            var curTime = 0f;
            while(curTime <= delay) {
                yield return null;

                if(!isPause)
                    curTime += Time.deltaTime;
            }

            cycleCurIndex++;
            if(cycleCurIndex == mCycleInfo.cycleCount)
                break;

            ApplyCurrentCycleInfo();

            if(signalInvokeCycleNext) signalInvokeCycleNext.Invoke(mCycleInfo);
        }

        if(signalInvokeCycleEnd) signalInvokeCycleEnd.Invoke(mCycleInfo);

        mRout = null;
    }

    private void ApplyCurrentCycleInfo() {
        var curCycle = cycleData.cycles[cycleCurIndex];

        //apply mods to stats
        System.Array.Copy(mAtmosphereStatsDefault, atmosphereStats, atmosphereStats.Length);

        if(mSeasonAtmosphereMods != null)
            AtmosphereModifier.Apply(atmosphereStats, mSeasonAtmosphereMods);

        if(mRegionAtmosphereMods != null)
            AtmosphereModifier.Apply(atmosphereStats, mRegionAtmosphereMods);

        if(curCycle.atmosphereMods != null)
            AtmosphereModifier.Apply(atmosphereStats, curCycle.atmosphereMods);

        mCycleInfo.weather = curCycle.weather;
        mCycleInfo.cycleIndex = cycleCurIndex;
    }
}
