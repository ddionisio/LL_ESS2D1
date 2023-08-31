using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LoLExt {
    [System.Serializable]
    public class ModalDialogFlow {
        public Sprite portrait;

        [M8.Localize]
        public string[] dialogTextRefs;

        private bool mIsNext;

        public IEnumerator Play() {
            yield return Play(ModalDialog.modalNameGeneric, portrait, null);
        }

        public IEnumerator Play(string modal, Sprite portrait, string nameTextRef) {
            for(int i = 0; i < dialogTextRefs.Length; i++) {
                string textRef = dialogTextRefs[i];
                if(string.IsNullOrEmpty(textRef))
                    continue;

                mIsNext = false;

                if(portrait)
                    ModalDialog.OpenApplyPortrait(modal, portrait, nameTextRef, textRef, OnDialogNext);
                else
                    ModalDialog.Open(modal, nameTextRef, textRef, OnDialogNext);

                while(!mIsNext)
                    yield return null;
            }

            if(M8.ModalManager.main.IsInStack(modal))
                M8.ModalManager.main.CloseUpTo(modal, true);

            //wait for dialog to close
            while(M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(modal))
                yield return null;
        }

        void OnDialogNext() {
            mIsNext = true;
        }
    }

    [System.Serializable]
    public class ModalDialogFlowIncremental {
        public Sprite portrait;

        public string prefix;
        public int startIndex;
        public int count; //set to 0 for infinite

        private System.Text.StringBuilder mSB;
        private bool mIsNext;

        public IEnumerator Play() {
            yield return Play(ModalDialog.modalNameGeneric, portrait, null);
        }

        public IEnumerator Play(Sprite portrait) {
            yield return Play(ModalDialog.modalNameGeneric, portrait, null);
        }

        public IEnumerator Play(string modal, Sprite portrait, string nameTextRef) {
            if(mSB == null)
                mSB = new System.Text.StringBuilder(prefix.Length + 2);

            int ind = 0;
            
            while(true) {
                mSB.Clear();
                mSB.Append(prefix);
                mSB.Append(ind + startIndex);

                string textRef = mSB.ToString();
                if(M8.Localize.Contains(textRef)) {
                    mIsNext = false;

                    if(portrait)
                        ModalDialog.OpenApplyPortrait(modal, portrait, nameTextRef, textRef, OnDialogNext);
                    else
                        ModalDialog.Open(modal, nameTextRef, textRef, OnDialogNext);

                    while(!mIsNext)
                        yield return null;

                    ind++;
                    if(count > 0 && ind == count)
                        break;
                }
                else
                    break;
            }

            if(M8.ModalManager.main.IsInStack(modal))
                M8.ModalManager.main.CloseUpTo(modal, true);

            //wait for dialog to close
            while(M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(modal))
                yield return null;
        }

        void OnDialogNext() {
            mIsNext = true;
        }
    }
}