using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SpriteRenderCopyProperties : MonoBehaviour {
    [Header("Sprite Renders")]
    public SpriteRenderer sourceSpriteRender;
    public SpriteRenderer destinationSpriteRender;

    [Header("Copy Properties")]
    public bool copySprite = true;
    public bool copyColor = false;
    public bool copyFlipX = true;
    public bool copyFlipY = true;

    void Awake() {
        if(!destinationSpriteRender)
            destinationSpriteRender = GetComponent<SpriteRenderer>();
    }

    void Update() {
        if(sourceSpriteRender && destinationSpriteRender) {
            if(copySprite) {
                if(destinationSpriteRender.sprite != sourceSpriteRender.sprite)
                    destinationSpriteRender.sprite = sourceSpriteRender.sprite;
            }

            if(copyColor) {
                if(destinationSpriteRender.color != sourceSpriteRender.color)
                    destinationSpriteRender.color = sourceSpriteRender.color;
            }

            if(copyFlipX)
                destinationSpriteRender.flipX = sourceSpriteRender.flipX;

            if(copyFlipY)
                destinationSpriteRender.flipY = sourceSpriteRender.flipY;
        }
    }
}
