using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleControlColorSprite : CycleControlColorBase {
    [Header("Display")]
    public SpriteRenderer spriteRender;

    protected override void ApplyColor(Color color) {
        if(spriteRender)
            spriteRender.color = color;
    }
}