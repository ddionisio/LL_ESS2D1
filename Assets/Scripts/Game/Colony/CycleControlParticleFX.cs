using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleControlParticleFX : CycleControlBase {
    [System.Serializable]
    public struct ParticleInfo {
        public WeatherTypeData weather;
        public ParticleSystem fx;

        public void Play() {
            var mod = fx.main;
            mod.loop = true;

            fx.Play();
        }

        public void Stop() {
            var mod = fx.main;
            mod.loop = false;
        }
    }

    public ParticleInfo[] particleInfos;

    private int mCurParticleIndex = -1;

    protected override void Begin() {
        Play(ColonyController.instance.cycleController.cycleCurWeather);
    }

    protected override void Next() {
        Play(ColonyController.instance.cycleController.cycleCurWeather);
    }

    protected override void End() {
        StopCurrent();
    }

    private void Play(WeatherTypeData weather) {
        var ind = -1;
        for(int i = 0; i < particleInfos.Length; i++) {
            if(particleInfos[i].weather == weather) {
                ind = i;
                break;
            }
        }

        if(mCurParticleIndex != ind) {
            StopCurrent();

            mCurParticleIndex = ind;

            if(mCurParticleIndex != -1)
                particleInfos[mCurParticleIndex].Play();
        }
    }

    private void StopCurrent() {
        if(mCurParticleIndex != -1) {
            particleInfos[mCurParticleIndex].Stop();
            mCurParticleIndex = -1;
        }
    }
}
