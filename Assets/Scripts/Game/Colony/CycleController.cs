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
    public float cycleCurElapsed { get; private set; }
    public float cycleTimeScale { get; set; }
    public float cycleDuration { get; private set; }
    public int cycleCount { get { return cycleData.cycles.Length; } }

    public CycleResource cycleResourceRate { get; private set; }

    public bool isRunning { get { return mRout != null; } }

    public bool isHazzard { get { return cycleCurWeather ? cycleCurWeather.isHazzard : false; } }

    private AtmosphereStat[] mAtmosphereStatsDefault;
    private AtmosphereModifier[] mRegionAtmosphereMods;

    private Coroutine mRout;

    public float GetResourceRate(CycleResourceType cycleResourceType) {
        switch(cycleResourceType) {
            case CycleResourceType.Sun:
                return cycleResourceRate.sun;
            case CycleResourceType.Wind:
                return cycleResourceRate.wind;
            case CycleResourceType.Water:
                return cycleResourceRate.water;
            case CycleResourceType.Growth:
                return cycleResourceRate.growth;
            default:
                return 0f;
        }
    }

    public void Setup(HotspotData hotspotData, SeasonData season) {
        cycleTimeScale = 1f;

        //grab cycle data based on season
        cycleData = cycleDataDefault;
        for(int i = 0; i < cycleSeasons.Length; i++) {
            var cycleSeason = cycleSeasons[i];
            if(cycleSeason.season == season) {
                cycleData = cycleSeason.data;
                break;
            }
        }

        mRegionAtmosphereMods = hotspotData.GetRegionAtmosphereModifiers(regionIndex);

        mAtmosphereStatsDefault = hotspotData.GetAtmosphereStats(season);

        atmosphereStats = new AtmosphereStat[mAtmosphereStatsDefault.Length];

        //setup some fixed cycle info
        daylightScale = hotspotData.GetDaylightScale(season);
        cycleDuration = GameData.instance.cycleDuration / cycleData.cycles.Length;

        cycleResourceRate = cycleData.resourceRate;

        var cycleControls = GetComponentsInChildren<CycleControl>(false);
        for(int i = 0; i < cycleControls.Length; i++)
            cycleControls[i].Init();
    }

    public void Begin() {
        if(mRout != null) {
            var signalCycleEnd = GameData.instance.signalCycleEnd;
            if(signalCycleEnd) signalCycleEnd.Invoke();

            StopCoroutine(mRout);
        }

        mRout = StartCoroutine(DoProcess());
    }

    IEnumerator DoProcess() {
        var gameDat = GameData.instance;

        cycleCurIndex = 0;
        cycleCurElapsed = 0f;

        ApplyCurrentCycleInfo();

        var signalCycleBegin = gameDat.signalCycleBegin;
        var signalCycleNext = gameDat.signalCycleNext;
        var signalCycleEnd = gameDat.signalCycleEnd;

        if(signalCycleBegin) signalCycleBegin.Invoke();

        while(true) {
            //time pass
            while(cycleCurElapsed <= cycleDuration) {
                yield return null;

                cycleCurElapsed += Time.deltaTime * cycleTimeScale;
            }

            if(cycleCurIndex + 1 == cycleCount)
                break;

            cycleCurIndex++;
            cycleCurElapsed = 0f;

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

        if(mRegionAtmosphereMods != null)
            AtmosphereModifier.Apply(atmosphereStats, mRegionAtmosphereMods);

        if(curCycle.atmosphereMods != null)
            AtmosphereModifier.Apply(atmosphereStats, curCycle.atmosphereMods);

        cycleResourceRate = cycleData.resourceRate + curCycle.resourceRateMod;
    }
}
