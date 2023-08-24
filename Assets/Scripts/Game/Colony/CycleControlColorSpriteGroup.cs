using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleControlColorSpriteGroup : CycleControlColorBase {
    [Header("Display")]
    public M8.SpriteColorGroup spriteColorGroup;

    protected override void ApplyColor(Color color) {
        if(spriteColorGroup)
            spriteColorGroup.ApplyColor(color);
    }
}
