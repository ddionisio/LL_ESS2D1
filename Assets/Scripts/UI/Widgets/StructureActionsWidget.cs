using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StructureActionsWidget : MonoBehaviour {
    public struct ActionItem {
        public StructureAction action;
        public Button button;

        public bool active {
            get { return button ? button.gameObject.activeSelf : false; }
            set {
                if(button)
                    button.gameObject.SetActive(value); 
            }
        }
    }

    public Transform itemRoot; //put buttons here, along with their name corresponding to StructureAction items

    public bool active {
        get { return gameObject.activeSelf; }
        set { gameObject.SetActive(value); }
    }

    public int activeActionCount { get; private set; }

    public event System.Action<StructureAction> clickCallback;

    private ActionItem[] mActionItems;

    private bool mIsInit;

    public void SetActionsActive(StructureAction actionMask) {
        if(!mIsInit)
            Init();

        activeActionCount = 0;

        for(int i = 0; i < mActionItems.Length; i++) {
            var itm = mActionItems[i];

            var isActive = (itm.action & actionMask) != StructureAction.None;

            if(isActive) {
                itm.active = true;
                activeActionCount++;
            }
            else
                itm.active = false;
        }
    }

    void Awake() {
        if(!mIsInit)
            Init();
    }

    private void Init() {
        mIsInit = true;

        var enumNames = System.Enum.GetNames(typeof(StructureAction));
        var enumVals = System.Enum.GetValues(typeof(StructureAction));

        mActionItems = new ActionItem[enumNames.Length];

        for(int i = 0; i < itemRoot.childCount; i++) {
            var child = itemRoot.GetChild(i);
            var childGO = child.gameObject;

            var btn = childGO.GetComponent<Button>();
            if(btn) {
                int index = -1;
                for(int j = 0; j < enumNames.Length; j++) {
                    if(string.Compare(child.name, enumNames[j], true) == 0) {
                        index = j;
                        break;
                    }
                }

                if(index != -1) {
                    var enumVal = (StructureAction)enumVals.GetValue(index);

                    btn.onClick.AddListener(delegate () { clickCallback?.Invoke(enumVal); });

                    mActionItems[index] = new ActionItem { action = enumVal, button = btn };
                }

                childGO.SetActive(false);
            }
        }

        activeActionCount = 0;
    }
}
