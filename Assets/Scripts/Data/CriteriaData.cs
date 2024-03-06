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

    public bool GetRange(AtmosphereAttributeBase atmosphere, out M8.RangeFloat rangeOut) {
        for(int i = 0; i < attributes.Length; i++) {
            var attr = attributes[i];
            if(attr.atmosphere == atmosphere) {
                rangeOut = attr.rangeBounds;
				return true;
            }
		}

        rangeOut = new M8.RangeFloat(0f, 0f);
        return false;
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
    /// 0 = value interesects
    /// -1 = value is out of bounds (below min)
    /// 1 = value is out of bounds (above max)
    /// </summary>
    public bool AtmosphereValueCompare(AtmosphereAttributeBase atmosphere, float val, out int result) {
		AttributeInfo attrInf = null;
        for(int i = 0; i < attributes.Length; i++) {
            var attrItm = attributes[i];
            if(attrItm.atmosphere == atmosphere) {
                attrInf = attrItm;
                break;
            }
        }

        if(attrInf != null) {
            var rangeBounds = attrInf.rangeBounds;
            if(val < rangeBounds.min)
				result = - 1;
            else if(val > rangeBounds.max)
                result = 1;
            else
                result = 0;

            return true;
        }

        result = -1;
        return false;
    }

    /// <summary>
    /// 0 = neutral (intersects)
    /// -1 = bad (out of bounds)
    /// 1 = good (inside bounds)
    /// </summary>
    public void Evaluate(int[] criticResults, AtmosphereStat[] stats, bool statUseMedianValue) {
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
                        result = statUseMedianValue ? AtmosphereStat.CheckBounds(attr.criticRange[j], stat.median) : AtmosphereStat.CheckBounds(attr.criticRange[j], stat.range);
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
