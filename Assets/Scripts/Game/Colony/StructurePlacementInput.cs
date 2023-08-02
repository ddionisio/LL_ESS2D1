using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using LoLExt;

public class StructurePlacementInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
    public class GhostItem {
        public StructureData structure { get; private set; }
        public StructureGhost ghost { get; private set; }

        public bool active { get { return mGO.activeSelf; } set { mGO.SetActive(value); } }

        private GameObject mGO;

        public GhostItem(StructureData data, Transform root) {
            structure = data;

            ghost = Instantiate(structure.ghostPrefab, root);

            mGO = ghost.gameObject;
            mGO.SetActive(false);

            var trans = ghost.transform;
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = Vector3.one;
        }
    }

    [Header("Info")]
    public StructurePlacementCursor cursor;
    public Transform ghostRoot; //structure's 'ghost' is placed here and activated during placement

    [Header("Signal Invoke")]
    public M8.SignalBoolean signalInvokePlacementClick; //false = placement is moving, true = clicked, placement move stopped

    public StructureGhost currentGhost { get { return mCurGhostItem != null ? mCurGhostItem.ghost : null; } }

    private GhostItem[] mGhostStructures;
    private GhostItem mCurGhostItem;

    private PointerEventData mPointerEvent;
    private bool mIsDragging;

    private bool mIsClicked;

    public void Init(StructurePaletteData paletteData) {
        var ghostStructureList = new List<GhostItem>();

        for(int i = 0; i < paletteData.groups.Length; i++) {
            var group = paletteData.groups[i];

            for(int j = 0; j < group.structures.Length; j++) {
                var structure = group.structures[j].data;

                //setup ghost
                ghostStructureList.Add(new GhostItem(structure, ghostRoot));
            }
        }

        mGhostStructures = ghostStructureList.ToArray();

        cursor.active = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Returns true if activate success (ghost found), false otherwise.
    /// </summary>
    public bool Activate(StructureData data) {
        //grab ghost
        for(int i = 0; i < mGhostStructures.Length; i++) {
            var ghostItem = mGhostStructures[i];
            if(ghostItem.structure == data) {
                mCurGhostItem = ghostItem;
                break;
            }
        }

        //no ghost found...
        if(mCurGhostItem == null)
            return false;

        mCurGhostItem.active = true;

        cursor.width = mCurGhostItem.ghost.placementBounds.size.x;
        
        mIsClicked = false;

        gameObject.SetActive(true);

        return true;
    }

    public void Deactivate() {
        if(mCurGhostItem != null) {
            mCurGhostItem.active = false;
            mCurGhostItem = null;
        }

        cursor.active = false;

        gameObject.SetActive(false);
    }

    void Update() {
        if(!(mIsClicked || mIsDragging || mPointerEvent == null)) {
            UpdateCursor(mPointerEvent);
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        mPointerEvent = eventData;

        if(!cursor.active)
            cursor.active = true;
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        mPointerEvent = null;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        UpdateCursor(eventData);

        //click
        mIsClicked = cursor.isValid;
        signalInvokePlacementClick?.Invoke(mIsClicked);
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
        mIsDragging = true;

        mIsClicked = false;
        signalInvokePlacementClick?.Invoke(false);

        UpdateCursor(eventData);
    }

    void IDragHandler.OnDrag(PointerEventData eventData) {
        if(mIsDragging)
            UpdateCursor(eventData);
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
        if(mIsDragging) {
            mIsDragging = false;

            UpdateCursor(eventData);

            //click
            mIsClicked = cursor.isValid;
            signalInvokePlacementClick?.Invoke(mIsClicked);
        }
    }

    private void UpdateCursor(PointerEventData pointerEventData) {
        var ptrRaycast = pointerEventData.pointerCurrentRaycast;
        if(ptrRaycast.isValid) {
            cursor.active = true;

            var worldPos = ptrRaycast.worldPosition;

            var itemBounds = mCurGhostItem.ghost.placementBounds;

            //clamp from bounds
            var pos = new Vector2(
                ClampX(worldPos.x, itemBounds.extents.x),
                ClampY(worldPos.y, 0f));

            cursor.position = pos;

            //check if valid placement
            cursor.isValid = mCurGhostItem.structure.IsPlacementValid(pos, itemBounds.size);
        }
        else {
            cursor.isValid = false;
            cursor.active = false;
        }
    }

    private float ClampX(float centerX, float extX) {
        var bounds = ColonyController.instance.bounds;

        var minX = bounds.min.x + extX;
        var maxX = bounds.max.x - extX;

        var rExtX = bounds.size.x * 0.5f;

        if(rExtX > extX)
            centerX = Mathf.Clamp(centerX, minX, maxX);
        else
            centerX = bounds.center.x;

        return centerX;
    }

    private float ClampY(float centerY, float extY) {
        var bounds = ColonyController.instance.bounds;

        var minY = bounds.min.y + extY;
        var maxY = bounds.max.y - extY;

        var rExtY = bounds.size.y * 0.5f;

        if(rExtY > extY)
            centerY = Mathf.Clamp(centerY, minY, maxY);
        else
            centerY = bounds.center.y;

        return centerY;
    }
}
