using UnityEngine;

using TMPro;

namespace LoLExt {
    public class TextFromAsset : MonoBehaviour {
        public TMP_Text textWidget;
        public TextAsset textAsset;

        private void Awake() {
            textWidget.text = textAsset.text;
        }
    }
}