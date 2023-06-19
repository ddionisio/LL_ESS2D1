using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandscapePreview : MonoBehaviour {
    
    public bool active { 
        get { return gameObject.activeSelf; }
        set { gameObject.SetActive(value); }
    }
}
