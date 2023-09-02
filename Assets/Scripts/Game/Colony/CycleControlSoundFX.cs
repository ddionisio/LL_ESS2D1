using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleControlSoundFX : CycleControlBase {
    [System.Serializable]
    public struct SfxInfo {
        public WeatherTypeData weather;
        [M8.SoundPlaylist]
        public string sfx;
        public bool isLoop;

        private M8.AudioSourceProxy mSrc;

        public void Play() {
            if(!string.IsNullOrEmpty(sfx)) {
                var src = M8.SoundPlaylist.instance.Play(sfx, isLoop);
                if(isLoop)
                    mSrc = src;
            }
        }

        public void Stop() {
            if(mSrc != null) {
                M8.SoundPlaylist.instance.Stop(mSrc);
                mSrc = null;
            }
        }
    }

    public SfxInfo[] sfxInfos;

    private int mCurSfxIndex = -1;

    protected override void Begin() {
        Play(ColonyController.instance.cycleController.cycleCurWeather);
    }

    protected override void Next() {
        Play(ColonyController.instance.cycleController.cycleCurWeather);
    }

    protected override void End() {
        StopCurrent();
    }

    void OnDisable() {
        if(mCurSfxIndex != -1) {
            sfxInfos[mCurSfxIndex].Stop();
            mCurSfxIndex = -1;
        }   
    }

    private void Play(WeatherTypeData weather) {
        var ind = -1;
        for(int i = 0; i < sfxInfos.Length; i++) {
            if(sfxInfos[i].weather == weather) {
                ind = i;
                break;
            }
        }

        if(mCurSfxIndex != ind) {
            StopCurrent();

            mCurSfxIndex = ind;

            if(mCurSfxIndex != -1)
                sfxInfos[mCurSfxIndex].Play();
        }
    }

    private void StopCurrent() {
        if(mCurSfxIndex != -1) {
            sfxInfos[mCurSfxIndex].Stop();
            mCurSfxIndex = -1;
        }
    }
}
