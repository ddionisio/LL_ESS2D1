using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ColonySequenceBase : MonoBehaviour {
    public bool isPauseCycle { get; protected set; }
    public bool cyclePauseAllowProgress { get; protected set; }

    public virtual void Init() { 
    }

    public virtual void Deinit() { 
    }

    public virtual IEnumerator Intro() {
        yield return null;
    }

    public virtual IEnumerator Forecast() {
        yield return null;
    }

    public virtual IEnumerator ColonyShipPreEnter() {
        yield return null;
    }

    public virtual IEnumerator ColonyShipPostEnter() {
        yield return null;
    }

    public virtual void CycleBegin() {

    }

    public virtual void CycleNext() {

    }

    public virtual IEnumerator CycleEnd() {
        yield return null;
    }
}
