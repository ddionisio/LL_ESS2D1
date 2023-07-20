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

                actionChangeCallback?.Invoke(this);
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
    public event System.Action<UnitItemWidget> actionChangeCallback;

    private Action mAction;
    private bool mInteractable;
    private int mCounter;

    private bool mIsInit;
    private PointerEventData mEnterPointerEventData;

    public void Setup(UnitData aUnitData) {
        if(!mIsInit) Init();

        unitData = aUnitData;

        if(iconImage) iconImage.sprite = aUnitData.icon;
        if(nameLabel) nameLabel.text = M8.Localize.Get(aUnitData.nameRef);
    }

    void OnDisable() {
        action = Action.None;
        mEnterPointerEventData = null;
    }

    void Awake() {
        if(!mIsInit) Init();
    }

    void Update() {
        if(mEnterPointerEventData != null)
            UpdateAction(mEnterPointerEventData);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        GameData.instance.signalClickCategory?.Invoke(GameData.clickCategoryUnitPalette);

        if(!mInteractable) return;

        //UpdateAction(eventData);

        clickCallback?.Invoke(this);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        mEnterPointerEventData = eventData;
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        mEnterPointerEventData = null;
        action = Action.None;
    }

    private void UpdateAction(PointerEventData eventData) {
        Action toAction = Action.None;

        Vector2 localPt;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, null, out localPt)) {
            var rect = rectTransform.rect;
            if(localPt.y > rect.center.y)
                toAction = Action.Increase;
            else if(localPt.y < rect.center.y)
                toAction = Action.Decrease;
        }

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
                if(decreaseGO) decreaseGO.SetActive(false);
                break;
            case Action.Increase:
                if(increaseGO) increaseGO.SetActive(true);
                if(decreaseGO) decreaseGO.SetActive(false);
                break;
            case Action.Decrease:
                if(increaseGO) increaseGO.SetActive(false);
                if(decreaseGO) decreaseGO.SetActive(true);
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
