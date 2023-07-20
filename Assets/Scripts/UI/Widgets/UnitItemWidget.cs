using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class UnitItemWidget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
    public enum Action {
        None,
        Increase,
        Decrease
    }

    [Header("Display")]
    public Image iconImage;
    public TMP_Text nameLabel;

    public TMP_Text counterLabel;

    public GameObject increaseGO;
    public GameObject decreaseGO;

    public GameObject activeGO;
    public GameObject inactiveGO;

    public bool interactable {
        get { return mInteractable; }
        set {
            if(mInteractable != value) {
                mInteractable = value;
                ApplyInteractable();
            }
        }
    }

    public Action action {
        get { return mAction; }
        private set {
            if(mAction != value) {
                mAction = value;
                ApplyAction();
            }
        }
    }

    public int counter {
        get { return mCounter; }
        set {
            if(mCounter != value) {
                mCounter = value;
                ApplyCounter();
            }
        }
    }

    public bool active { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }

    public UnitData unitData { get; private set; }

    public RectTransform rectTransform { get; private set; }

    public event System.Action<UnitItemWidget> clickCallback;

    private Action mAction;
    private bool mInteractable;
    private int mCounter;

    private bool mIsInit;

    public void Setup(UnitData aUnitData) {
        if(!mIsInit) Init();

        unitData = aUnitData;

        if(iconImage) iconImage.sprite = aUnitData.icon;
        if(nameLabel) nameLabel.text = M8.Localize.Get(aUnitData.nameRef);
    }

    void Awake() {
        if(!mIsInit) Init();
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if(!mInteractable) return;

        UpdateAction(eventData);

        clickCallback?.Invoke(this);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        UpdateAction(eventData);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        action = Action.None;
    }

    private void UpdateAction(PointerEventData eventData) {
        Action toAction;

        Vector2 localPt;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, null, out localPt)) {
            var rect = rectTransform.rect;
            if(localPt.y > rect.center.y)
                toAction = Action.Increase;
            else
                toAction = Action.Decrease;
        }
        else
            toAction = Action.None;

        action = toAction;
    }

    private void ApplyCounter() {
        if(counterLabel)
            counterLabel.text = mCounter.ToString();
    }

    private void ApplyAction() {
        switch(mAction) {
            case Action.None:
                if(increaseGO) increaseGO.SetActive(false);
                if(decreaseGO) increaseGO.SetActive(false);
                break;
            case Action.Increase:
                if(increaseGO) increaseGO.SetActive(true);
                if(decreaseGO) increaseGO.SetActive(false);
                break;
            case Action.Decrease:
                if(increaseGO) increaseGO.SetActive(false);
                if(decreaseGO) increaseGO.SetActive(true);
                break;
        }
    }

    private void ApplyInteractable() {
        if(activeGO) activeGO.SetActive(mInteractable);
        if(inactiveGO) inactiveGO.SetActive(!mInteractable);
    }

    private void Init() {
        rectTransform = transform as RectTransform;

        mInteractable = false;
        ApplyInteractable();

        mAction = Action.None;
        ApplyAction();

        mCounter = 0;
        ApplyCounter();

        mIsInit = true;
    }
}
