using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "criteria", menuName = "Game/Criteria")]
public class CriteriaData : ScriptableObject {
    [System.Serializable]
    public class AttributeInfo {
        public AtmosphereAttributeBase atmosphere;

        public M8.RangeFloat[] criticRange; //each critic preference, must be the same length across all attributes
        
        public M8.RangeFloat rangeBounds {
            get {
                float min = float.MaxValue, max = float.MinValue;

                for(int i = 0; i < criticRange.Length; i++) {
                    var r = criticRange[i];

                    if(r.min < min)
                        min = r.min;
                    if(r.max > max)
                        max = r.max;
                }

                return new M8.RangeFloat { min=min, max=max };
            }
        }
    }

    public AttributeInfo[] attributes;
}
