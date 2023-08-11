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

    public int criticCount {
        get {
            int count = 0;

            for(int i = 0; i < attributes.Length; i++) {
                var attr = attributes[i];
                if(attr.criticRange.Length > count)
                    count = attr.criticRange.Length;
            }

            return count;
        }
    }

    public AtmosphereStat[] GenerateAtmosphereStats() {
        var stats = new AtmosphereStat[attributes.Length];

        for(int i = 0; i < stats.Length; i++) {
            var attr = attributes[i];
            stats[i] = new AtmosphereStat { atmosphere = attr.atmosphere, range = attr.rangeBounds };
        }

        return stats;
    }

    /// <summary>
    /// 0 = neutral (intersects)
    /// -1 = bad (out of bounds)
    /// 1 = good (inside bounds)
    /// </summary>
    public void Evaluate(int[] criticResults, AtmosphereStat[] stats) {
        for(int i = 0; i < attributes.Length; i++) {
            var attr = attributes[i];

            int statInd = -1;
            for(int j = 0; j < stats.Length; j++) {
                if(stats[j].atmosphere == attr.atmosphere) {
                    statInd = j;
                    break;
                }
            }

            if(statInd != -1) {
                var stat = stats[statInd];

                for(int j = 0; j < criticResults.Length; j++) {
                    int result;

                    if(j < attr.criticRange.Length)
                        result = stat.CheckBounds(attr.criticRange[j]);
                    else
                        result = 1;

                    if(i == 0) //first attribute result is applied
                        criticResults[j] = result;
                    else if(result == -1 || criticResults[j] == 1) //prioritize bad result, then neutral, good can only be possible if they all match
                        criticResults[j] = result;
                }
            }
            else if(i == 0) { //no match, just apply 'good' if it's the first attribute
                for(int j = 0; j < criticResults.Length; j++)
                    criticResults[j] = 1;
            }
        }
    }
}
