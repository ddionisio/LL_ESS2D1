using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructurePaletteWidget : MonoBehaviour {
    [Header("Group Info")]
    public StructureGroupWidget groupWidgetTemplate; //not prefab
    public Transform groupWidgetRoot;

    [Header("Item Info")]
    public StructureItemWidget itemWidgetTemplate;
    public Transform itemWidgetContainer;

    private StructureGroupWidget[] mGroupWidgets; //corresponds to group arrays in structure controller

    private StructureGroupWidget mGroupWidgetActive;

    private M8.CacheList<StructureItemWidget> mStructureItemWidgetActives;
    private M8.CacheList<StructureItemWidget> mStructureItemWidgetCache;

    public void Setup(StructurePaletteData palette) {
        int itemMaxCapacity = 0;

        //generate group items
        if(groupWidgetRoot && groupWidgetTemplate) {
            mGroupWidgets = new StructureGroupWidget[palette.groups.Length];

            for(int i = 0; i < palette.groups.Length; i++) {
                var grp = palette.groups[i];

                var itmCount = grp.structures.Length;

                if(itemMaxCapacity < itmCount)
                    itemMaxCapacity = itmCount;

                var newGroupWidget = Instantiate(groupWidgetTemplate);

                newGroupWidget.Setup(grp);

                newGroupWidget.clickCallback += OnGroupClick;

                newGroupWidget.transform.SetParent(groupWidgetRoot, false);
                newGroupWidget.gameObject.SetActive(true);

                mGroupWidgets[i] = newGroupWidget;
            }

            groupWidgetTemplate.gameObject.SetActive(false);
        }

        //generate item cache
        if(itemWidgetContainer && itemWidgetTemplate) {
            mStructureItemWidgetActives = new M8.CacheList<StructureItemWidget>(itemMaxCapacity);
            mStructureItemWidgetCache = new M8.CacheList<StructureItemWidget>(itemMaxCapacity);
                        
            for(int i = 0; i < itemMaxCapacity; i++) {
                var newItm = Instantiate(itemWidgetTemplate);

                newItm.clickCallback += OnItemClick;

                newItm.transform.SetParent(itemWidgetContainer, false);

                mStructureItemWidgetCache.Add(newItm);
            }

            itemWidgetContainer.gameObject.SetActive(false);
            itemWidgetTemplate.gameObject.SetActive(false);
        }
    }

    public void RefreshGroups() {
        if(mGroupWidgets == null) return; //fail-safe

        var structureCtrl = ColonyController.instance.structureController;

        for(int i = 0; i < mGroupWidgets.Length; i++) {
            var groupInf = structureCtrl.GetGroupInfo(i);

            var groupWidget = mGroupWidgets[i];

            groupWidget.count = groupInf.capacity - groupInf.count;
            groupWidget.active = !groupInf.isHidden;
        }
    }

    public void ClearGroupActive() {
        if(mGroupWidgetActive) {
            mGroupWidgetActive.itemsActive = false;
            mGroupWidgetActive = null;
        }

        for(int i = 0; i < mStructureItemWidgetActives.Count; i++) {
            var itm = mStructureItemWidgetActives[i];

            itm.transform.SetParent(itemWidgetContainer, false);

            mStructureItemWidgetCache.Add(itm);
        }

        mStructureItemWidgetActives.Clear();
    }

    void OnGroupClick(StructureGroupWidget groupWidget) {

        if(mGroupWidgetActive != groupWidget) {
            ClearGroupActive();

            mGroupWidgetActive = groupWidget;

            //setup items
            for(int i = 0; i < groupWidget.structures.Length; i++) {
                if(mStructureItemWidgetCache.Count == 0) //shouldn't happen
                    break;

                var structureDat = groupWidget.structures[i];

                var itm = mStructureItemWidgetCache.RemoveLast();

                itm.Setup(structureDat);

                itm.transform.SetParent(mGroupWidgetActive.itemsContainerRoot, false);

                mStructureItemWidgetActives.Add(itm);
            }

            mGroupWidgetActive.itemsActive = true;
        }
        else //toggle
            ClearGroupActive();
    }

    void OnItemClick(StructureItemWidget itemWidget) {
        var structureCtrl = ColonyController.instance.structureController;
        structureCtrl.PlacementStart(itemWidget.data);

        ClearGroupActive();
    }
}
