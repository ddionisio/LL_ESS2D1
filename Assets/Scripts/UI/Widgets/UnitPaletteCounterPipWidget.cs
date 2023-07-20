using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitPaletteCounterPipWidget : MonoBehaviour {
    [Header("Display")]
    public GameObject lineGO;
    public GameObject baseGO;
    public Graphic baseGraphic;
    public Color baseActionNoneColor = Color.white;
    public Color baseActionIncreaseColor = Color.green;
    public Color baseActionDecreaseColor = Color.red;

    public UnitItemWidget.Action action {
        get { return mAction; }
        set {
            if(mAction != value) {
                mAction = value;
                ApplyActionDisplay();
            }
        }
    }

    public bool active { 
        get { return gameObject.activeSelf; } 
        set { gameObject.SetActive(value); } 
    }

    public bool baseActive {
        get { return baseGO ? baseGO.activeSelf : false; }
        set { if(baseGO) baseGO.SetActive(value); }
    }

    public bool lineActive {
        get { return lineGO ? lineGO.activeSelf : false; }
        set { if(lineGO) lineGO.SetActive(value); }
    }

    private UnitItemWidget.Action mAction;

    void Awake() {
        mAction = UnitItemWidget.Action.None;
        ApplyActionDisplay();
    }

    private void ApplyActionDisplay() {
        switch(mAction) {
            case UnitItemWidget.Action.None:
                if(baseGraphic) baseGraphic.color = baseActionNoneColor;
                break;
            case UnitItemWidget.Action.Increase:
                if(baseGraphic) baseGraphic.color = baseActionIncreaseColor;
                break;
            case UnitItemWidget.Action.Decrease:
                if(baseGraphic) baseGraphic.color = baseActionDecreaseColor;
                break;
        }
    }
}
