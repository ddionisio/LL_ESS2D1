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
        
    public AtmosphereStat[] atmosphereStats { get; private set; }
    public float daylightScale { get; private set; }

    public CycleData cycleData { get; private set; }
    public int cycleCurIndex { get; private set; }
    public WeatherTypeData cycleCurWeather { get { return cycleData.cycles[cycleCurIndex].weather; } }
    public float cycleDuration { get; private set; }
    public int cycleCount { get { return cycleData.cycles.Length; } }

    public CycleResource cycleResourceRate { get; private set; }

    public bool isPause { get; private set; }

    public bool isRunning { get { return mRout != null; } }

    public bool isHazzard { get { return cycleCurWeather ? cycleCurWeather.isHazzard : false; } }

    private AtmosphereStat[] mAtmosphereStatsDefault;
    private AtmosphereModifier[] mSeasonAtmosphereMods;
    private AtmosphereModifier[] mRegionAtmosphereMods;

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
        daylightScale = hotspotData.GetDaylightScale(season);
        cycleDuration = GameData.instance.cycleDuration / cycleData.cycles.Length;

        cycleResourceRate = cycleData.resourceRate;
    }

    public void Begin() {
        if(mRout != null) {
            var signalCycleEnd = GameData.instance.signalCycleEnd;
            if(signalCycleEnd) signalCycleEnd.Invoke();

            StopCoroutine(mRout);
        }

        mRout = StartCoroutine(DoProcess());
    }

    void OnDisable() {
        if(GameData.instance.signalPause) GameData.instance.signalPause.callback -= OnPause;
    }

    void OnEnable() {
        if(GameData.instance.signalPause) GameData.instance.signalPause.callback += OnPause;
    }

    void OnPause(bool pause) {
        isPause = pause;
    }

    IEnumerator DoProcess() {
        var gameDat = GameData.instance;

        cycleCurIndex = 0;

        ApplyCurrentCycleInfo();

        var signalCycleBegin = gameDat.signalCycleBegin;
        var signalCycleNext = gameDat.signalCycleNext;
        var signalCycleEnd = gameDat.signalCycleEnd;

        if(signalCycleBegin) signalCycleBegin.Invoke();

        while(true) {
            //time pass
            var curTime = 0f;
            while(curTime <= cycleDuration) {
                yield return null;

                if(!isPause)
                    curTime += Time.deltaTime;
            }

            cycleCurIndex++;
            if(cycleCurIndex == cycleCount)
                break;

            ApplyCurrentCycleInfo();

            if(signalCycleNext) signalCycleNext.Invoke();
        }

        if(signalCycleEnd) signalCycleEnd.Invoke();

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

        cycleResourceRate = cycleData.resourceRate + curCycle.resourceRateMod;
    }
}
