using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "gridData", menuName = "Game/GridData")]
public class GridData : M8.SingletonScriptableObject<GridData> {
	public enum TopographyType {
		None,

		Mountain,
		Hill,
		Forest,
		Jungle,
		Vegetation,
		River,
		Lake,
		Ocean,
		Swamp,
		Oasis
	}

	[System.Serializable]
	public struct AtmosphereMod {
		public float temperature;
		public float humidity;
		public float wind;

		public static AtmosphereMod operator +(AtmosphereMod a, AtmosphereMod b) {
			return new AtmosphereMod {
				temperature = a.temperature + b.temperature,
				humidity = a.humidity + b.humidity,
				wind = a.wind + b.wind,
			};
		}
	}

	[Header("Topography Info")]
	[M8.Localize]
	public string topographyMountainTextRef;
	[M8.Localize]
	public string topographyHillTextRef;
	[M8.Localize]
	public string topographyForestTextRef;
	[M8.Localize]
	public string topographyJungleTextRef;
	[M8.Localize]
	public string topographyVegetationTextRef;
	[M8.Localize]
	public string topographyRiverTextRef;
	[M8.Localize]
	public string topographyLakeTextRef;
	[M8.Localize]
	public string topographyOceanTextRef;
	[M8.Localize]
	public string topographySwampTextRef;
	[M8.Localize]
	public string topographyOasisTextRef;

	[Header("Layer Info")]
	public LayerMask gridLayerMask;
	public LayerMask gridInvalidLayerMask;

	[Header("Atmosphere Info")]
	public AtmosphereAttributeBase temperatureAtmosphere;
	public AtmosphereAttributeBase humidityAtmosphere;
	public AtmosphereAttributeBase windAtmosphere;

	public string GetTopographyText(TopographyType topographyType) {
		switch(topographyType) {
			case TopographyType.Mountain:
				return M8.Localize.Get(topographyMountainTextRef);
			case TopographyType.Hill:
				return M8.Localize.Get(topographyHillTextRef);
			case TopographyType.Forest:
				return M8.Localize.Get(topographyForestTextRef);
			case TopographyType.Jungle:
				return M8.Localize.Get(topographyJungleTextRef);
			case TopographyType.Vegetation:
				return M8.Localize.Get(topographyVegetationTextRef);
			case TopographyType.River:
				return M8.Localize.Get(topographyRiverTextRef);
			case TopographyType.Lake:
				return M8.Localize.Get(topographyLakeTextRef);
			case TopographyType.Ocean:
				return M8.Localize.Get(topographyOceanTextRef);
			case TopographyType.Swamp:
				return M8.Localize.Get(topographySwampTextRef);
			case TopographyType.Oasis:
				return M8.Localize.Get(topographyOasisTextRef);

			default:
				return "";
		}
	}

	public void ApplyMod(AtmosphereStat[] stats, AtmosphereMod mod) {
		for(int i = 0; i < stats.Length; i++) {
			var stat = stats[i];

			if(stat.atmosphere == temperatureAtmosphere) {
				var range = stat.range;

				stat.range = temperatureAtmosphere.ClampRange(range.min + mod.temperature, range.max + mod.temperature);

				stats[i] = stat;
			}
			else if(stat.atmosphere == humidityAtmosphere) {
				var range = stat.range;

				stat.range = humidityAtmosphere.ClampRange(range.min + mod.humidity, range.max + mod.humidity);

				stats[i] = stat;
			}
			else if(stat.atmosphere == windAtmosphere) {
				var range = stat.range;

				stat.range = windAtmosphere.ClampRange(range.min + mod.wind, range.max + mod.wind);

				stats[i] = stat;
			}
		}
	}
}
