using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place this inside Structure hierarchy
/// </summary>
public class StructureStatusGroupWidget : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolDespawn {
    public GameObject displayRootGO;
    public Transform statusItemRoot;

    private StructureStatusWidget[] mStatusItemWidgets;

    private Structure mStructure;

    void M8.IPoolDespawn.OnDespawned() {
        if(mStructure) {
            mStructure.statusUpdateCallback -= OnStatusUpdate;
            mStructure.stateChangedCallback -= RefreshDisplayFromStructureState;
        }

        if(displayRootGO) displayRootGO.SetActive(false);
    }

    void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
        //apply current status display
        if(mStructure) {
            mStructure.statusUpdateCallback += OnStatusUpdate;
            mStructure.stateChangedCallback += RefreshDisplayFromStructureState;
        }
    }

    void M8.IPoolInit.OnInit() {
        mStructure = GetComponentInParent<Structure>(true);

        //setup item widgets corresponding to StructureStatus via name
        var statusEnumNames = System.Enum.GetNames(typeof(StructureStatus));

        mStatusItemWidgets = new StructureStatusWidget[statusEnumNames.Length];

        for(int i = 0; i < statusItemRoot.childCount; i++) {
            var child = statusItemRoot.GetChild(i);

            var widget = child.GetComponent<StructureStatusWidget>();
            if(widget) {
                widget.ResetState();

                int index = -1;
                for(int j = 0; j < statusEnumNames.Length; j++) {
                    if(string.Compare(statusEnumNames[j], child.name, true) == 0) {
                        index = j;
                        break;
                    }
                }

                if(index != -1)
                    mStatusItemWidgets[index] = widget;
            }
        }

        if(displayRootGO) displayRootGO.SetActive(false);
    }

    void OnStatusUpdate(StructureStatusInfo inf) {
        var widget = mStatusItemWidgets[(int)inf.type];
        if(widget)
            widget.Apply(inf);
    }

    private void RefreshStatusDisplay() {
        for(int i = 0; i < mStatusItemWidgets.Length; i++) {
            var statusType = (StructureStatus)i;

            var widget = mStatusItemWidgets[i];
            if(widget) {
                var inf = mStructure.GetStatusInfo(statusType);

                widget.Apply(inf);
            }
        }
    }

    private void RefreshDisplayFromStructureState(StructureState state) {
        bool isVisible;

        switch(state) {
            case StructureState.Spawning:
            case StructureState.MoveReady:
            case StructureState.Moving:
            case StructureState.Victory:
            case StructureState.None:
                isVisible = false;
                break;

            default:
                isVisible = true;
                break;
        }

        if(displayRootGO)
            displayRootGO.SetActive(isVisible);
    }
}
