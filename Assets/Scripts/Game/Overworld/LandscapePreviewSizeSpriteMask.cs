using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteMask))]
public class LandscapePreviewSizeSpriteMask : MonoBehaviour {

    private SpriteMask mSpriteMask;

    public bool RefreshTransform() {
        if(!mSpriteMask)
            mSpriteMask = GetComponent<SpriteMask>();

        var spr = mSpriteMask.sprite;
        if(!spr) 
            return false;

        var size = GameData.instance.landscapePreviewSize;

        var ppu = spr.pixelsPerUnit;

        var spriteSize = spr.rect.size;

        Vector2 curScale = transform.localScale;

        var toScale = new Vector2 {
            x = (ppu / spriteSize.x) * size.x,
            y = (ppu / spriteSize.y) * size.y
        };

        if(curScale != toScale) {
            transform.localScale = new Vector3 { x = toScale.x, y = toScale.y, z = 1f };

            return true;
        }

        return false;
    }

    void Awake() {
        RefreshTransform();
    }

    void OnDrawGizmos() {
        var size = GameData.instance.landscapePreviewSize;

        Gizmos.color = GameData.instance.landscapePreviewBoundsColor;

        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y));
    }
}
