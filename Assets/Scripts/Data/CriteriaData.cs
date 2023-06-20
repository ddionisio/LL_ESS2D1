using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "criteria", menuName = "Game/Criteria")]
public class CriteriaData : ScriptableObject {
    [System.Serializable]
    public class AttributeInfo {
        public AtmosphereAttributeBase atmosphere;

        public M8.RangeFloat[] criticRange; //each critic preference, must be the same length across all attributes
    }

    public AttributeInfo[] attributes;
}
