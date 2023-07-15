using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BackgroundClick : MonoBehaviour, IPointerClickHandler {
    //Just a simple click signal to cancel some menus (for now)

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        GameData.instance.signalClickCategory?.Invoke(GameData.clickCategoryBackground);
    }
}
