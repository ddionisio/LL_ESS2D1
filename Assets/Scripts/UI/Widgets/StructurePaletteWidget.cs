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
    public int itemWidgetCapacity = 4;

    private StructureGroupWidget[] mGroupWidgets; //corresponds to group arrays in structure controller
    private int mGroupWidgetCount;

    private StructureGroupWidget mGroupWidgetActive;

    private M8.CacheList<StructureItemWidget> mStructureItemWidgetActives;
    private M8.CacheList<StructureItemWidget> mStructureItemWidgetCache;

    public void Setup(StructurePaletteData palette) {
        //generate group items
        if(groupWidgetRoot && groupWidgetTemplate) {
            mGroupWidgetCount = palette.groups.Length;

            //allocate widgets if new or larger
            if(mGroupWidgets == null) {
                mGroupWidgets = new StructureGroupWidget[mGroupWidgetCount];
                for(int i = 0; i < mGroupWidgetCount; i++) {
                    var newWidget = Instantiate(groupWidgetTemplate, groupWidgetRoot);
                    newWidget.clickCallback += OnGroupClick;
                    mGroupWidgets[i] = newWidget;
                }
            }
            else if(mGroupWidgets.Length < mGroupWidgetCount) {
                var lastSize = mGroupWidgets.Length;
                System.Array.Resize(ref mGroupWidgets, mGroupWidgetCount);

                for(int i = lastSize; i < mGroupWidgetCount; i++) {
                    var newWidget = Instantiate(groupWidgetTemplate, groupWidgetRoot);
                    newWidget.clickCallback += OnGroupClick;
                    mGroupWidgets[i] = newWidget;
                }
            }

            //setup widgets
            for(int i = 0; i < mGroupWidgetCount; i++) {
                var grp = palette.groups[i];

                var groupWidget = mGroupWidgets[i];

                groupWidget.Setup(i, grp);
                                
                groupWidget.active = true;
            }

            for(int i = mGroupWidgetCount; i < mGroupWidgets.Length; i++)
                mGroupWidgets[i].active = false;
        }

        //generate item cache
        if(mStructureItemWidgetActives == null && itemWidgetContainer && itemWidgetTemplate) {
            mStructureItemWidgetActives = new M8.CacheList<StructureItemWidget>(itemWidgetCapacity);
            mStructureItemWidgetCache = new M8.CacheList<StructureItemWidget>(itemWidgetCapacity);

            for(int i = 0; i < itemWidgetCapacity; i++) {
                var newItm = Instantiate(itemWidgetTemplate, itemWidgetContainer);

                newItm.clickCallback += OnItemClick;
                newItm.hoverCallback += OnItemHover;

                mStructureItemWidgetCache.Add(newItm);
            }

            itemWidgetContainer.gameObject.SetActive(false);
        }
    }

    public void RefreshGroups() {
        if(mGroupWidgets == null) return; //fail-safe

        var structureCtrl = ColonyController.instance.structurePaletteController;

        for(int i = 0; i < mGroupWidgetCount; i++) {
            var groupWidget = mGroupWidgets[i];
            if(!groupWidget) //fail-safe
                continue;

            var groupInf = structureCtrl.GroupGetInfo(i);

            if(groupInf != null) {
                groupWidget.count = groupInf.capacity - groupInf.count;
                groupWidget.active = groupInf.visibleStructuresCount > 0;
            }
            else //fail-safe
                groupWidget.active = false;
        }
    }

    public void RefreshGroup(StructureData structureData) {
        var structureCtrl = ColonyController.instance.structurePaletteController;
        var groupInd = structureCtrl.GroupGetIndex(structureData);

        RefreshGroup(groupInd);
    }

    public void RefreshGroup(int groupIndex) {
        if(mGroupWidgets == null || groupIndex < 0 || groupIndex >= mGroupWidgetCount) return; //fail-safe

        var groupWidget = mGroupWidgets[groupIndex];

        var structureCtrl = ColonyController.instance.structurePaletteController;

        
        if(groupWidget) { //fail-safe
            var groupInf = structureCtrl.GroupGetInfo(groupIndex);

            if(groupInf != null) {
                groupWidget.count = groupInf.capacity - groupInf.count;

                //determine visibility and 'new' highlight
                int newCount = 0, visibleCount = 0;
                for(int i = 0; i < groupInf.structures.Length; i++) {
                    var structInf = groupInf.structures[i];

                    if(!structInf.isHidden) {
                        visibleCount++;
                        if(structInf.isNew)
                            newCount++;
                    }
                }

                if(visibleCount > 0) {
                    groupWidget.active = true;
                    groupWidget.newHighlightActive = (groupInf.highlightOnAvailable && groupInf.count < groupInf.capacity) || newCount > 0;
					groupWidget.pointerHighlightActive = groupInf.pointerOnAvailable && groupInf.count < groupInf.capacity;
				}
                else
                    groupWidget.active = false;
            }
            else //fail-safe
                groupWidget.active = false;
        }
    }

    private void RefreshGroupWidgetNewHighlight(StructureGroupWidget groupWidget, StructurePaletteController.GroupInfo groupInfo) {
        int newCount = 0;
        for(int i = 0; i < groupInfo.structures.Length; i++) {
            if(groupInfo.structures[i].isNew)
                newCount++;
        }

        groupWidget.newHighlightActive = (groupInfo.highlightOnAvailable && groupInfo.count < groupInfo.capacity) || newCount > 0;
		groupWidget.pointerHighlightActive = groupInfo.pointerOnAvailable && groupInfo.count < groupInfo.capacity;
	}

    public void ClearGroupActive() {
        if(mGroupWidgetActive) {
            //refresh newHighlight status
            var grpInf = ColonyController.instance.structurePaletteController.GroupGetInfo(mGroupWidgetActive.index);
            RefreshGroupWidgetNewHighlight(mGroupWidgetActive, grpInf);

            mGroupWidgetActive.itemsActive = false;
            mGroupWidgetActive = null;
        }

        if(mStructureItemWidgetActives != null) {
            for(int i = 0; i < mStructureItemWidgetActives.Count; i++) {
                var itm = mStructureItemWidgetActives[i];
                if(itm) {
                    itm.transform.SetParent(itemWidgetContainer, false);

                    mStructureItemWidgetCache.Add(itm);
                }
            }

            mStructureItemWidgetActives.Clear();
        }
    }

    void OnDisable() {
        if(GameData.isInstantiated)
            GameData.instance.signalClickCategory.callback -= OnClickCategory;

        ClearGroupActive();
    }

    void OnEnable() {
        GameData.instance.signalClickCategory.callback += OnClickCategory;
    }

    void Awake() {
        if(groupWidgetTemplate) groupWidgetTemplate.active = false;
    }

    void OnGroupClick(StructureGroupWidget groupWidget) {
        GameData.instance.signalClickCategory?.Invoke(GameData.clickCategoryStructurePalette);

        if(mGroupWidgetActive != groupWidget) {
            ClearGroupActive();

            mGroupWidgetActive = groupWidget;

            var structureCtrl = ColonyController.instance.structurePaletteController;

            var grpInfo = structureCtrl.GroupGetInfo(groupWidget.index);

            //setup items
            for(int i = 0; i < grpInfo.structures.Length; i++) {
                if(mStructureItemWidgetCache.Count == 0) //shouldn't happen
                    break;

                var structureInf = grpInfo.structures[i];
                if(structureInf.isHidden) //skip hidden
                    continue;

                var itm = mStructureItemWidgetCache.RemoveLast();

                itm.Setup(structureInf.data);

                itm.newHighlightActive = structureInf.isNew;

                itm.transform.SetParent(mGroupWidgetActive.itemsContainerRoot, false);

                mStructureItemWidgetActives.Add(itm);
            }

            mGroupWidgetActive.itemsActive = true;

            ColonyController.instance.Pause();
        }
        else { //toggle
            ClearGroupActive();

            if(ColonyController.instance.timeState == ColonyController.TimeState.Pause)
                ColonyController.instance.Resume();
        }
    }

    void OnItemClick(StructureItemWidget itemWidget) {
        var structureCtrl = ColonyController.instance.structurePaletteController;

        if(itemWidget.newHighlightActive)
            structureCtrl.SetStructureSeen(itemWidget.data);
        
        structureCtrl.PlacementStart(itemWidget.data);

        ClearGroupActive();
    }

    void OnItemHover(StructureItemWidget itemWidget, bool isHover) {
        if(!isHover) {
            //remove new highlight
            if(itemWidget.newHighlightActive) {
                itemWidget.newHighlightActive = false;

                var structureCtrl = ColonyController.instance.structurePaletteController;

                structureCtrl.SetStructureSeen(itemWidget.data);

                if(mGroupWidgetActive) {
                    var grpInf = ColonyController.instance.structurePaletteController.GroupGetInfo(mGroupWidgetActive.index);
                    RefreshGroupWidgetNewHighlight(mGroupWidgetActive, grpInf);
                }
            }
        }
    }

    void OnClickCategory(int category) {
        if(category != GameData.clickCategoryStructurePalette) {
            ClearGroupActive();

            if(ColonyController.instance.timeState == ColonyController.TimeState.Pause)
                ColonyController.instance.Resume();
        }
    }
}
