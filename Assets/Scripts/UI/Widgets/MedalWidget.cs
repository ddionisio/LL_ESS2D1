using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MedalWidget : MonoBehaviour {
    [Header("Info")]
    public Sprite[] medalSprites;

    [Header("Display")]
    public Image iconImage;
    public bool iconUseNativeSize;

    public void ApplyRank(float t) {
        if(medalSprites.Length == 0)
            return;

        int index = Mathf.Clamp(Mathf.FloorToInt(t * (medalSprites.Length - 1)), 0, medalSprites.Length - 1);

        if(iconImage) {
            iconImage.sprite = medalSprites[index];
            if(iconUseNativeSize)
                iconImage.SetNativeSize();
        }
    }
}
