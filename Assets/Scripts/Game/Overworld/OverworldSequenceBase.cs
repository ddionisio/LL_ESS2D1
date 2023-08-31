using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class OverworldSequenceBase : MonoBehaviour {
    
    public virtual void Init() { }
    public virtual void Deinit() { }

    public virtual IEnumerator StartBegin() { yield return null; }

    public virtual IEnumerator StartFinish() { yield return null; }

    public virtual void SeasonToggle(SeasonData season) { }

    public virtual void HotspotClick(Hotspot hotspot) { }

    public virtual IEnumerator InvestigationEnterBegin() { yield return null; }

    public virtual IEnumerator InvestigationEnterEnd() { yield return null; }
}
