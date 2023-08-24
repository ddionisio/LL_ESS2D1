using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleControlColorMaterial : CycleControlColorBase {
    [Header("Display")]
    [SerializeField]
    Renderer _renderer;
    public bool useSharedMaterial;
    public string colorProperty;
    public bool colorIsMultiply;

    private Material mMaterial;
    private int mColorPropertyId;
    private Color mColorDefault;

    protected override void ApplyColor(Color color) {
        if(mMaterial) {
            if(colorIsMultiply)
                mMaterial.SetColor(mColorPropertyId, color * mColorDefault);
            else
                mMaterial.SetColor(mColorPropertyId, color);
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();

        if(!useSharedMaterial && mMaterial)
            Destroy(mMaterial);
    }

    protected override void Awake() {
        mColorPropertyId = Shader.PropertyToID(colorProperty);

        if(_renderer) {
            if(useSharedMaterial)
                mMaterial = _renderer.sharedMaterial;
            else {
                mMaterial = new Material(_renderer.sharedMaterial);
                _renderer.sharedMaterial = mMaterial;
            }

            mColorDefault = mMaterial.GetColor(mColorPropertyId);
        }

        base.Awake();
    }
}
