using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "gridData", menuName = "Game/GridData")]
public class GridData : M8.SingletonScriptableObject<GridData> {
	[System.Serializable]
	public struct AtmosphereMod {
		public float temperature;
		public float humidity;
		public float wind;

		public static AtmosphereMod operator +(AtmosphereMod a, AtmosphereMod b) {
			return new AtmosphereMod {
				temperature = a.temperature + b.temperature,
				humidity = a.humidity + b.humidity,
				wind = a.wind + b.humidity,
			};
		}
	}

	[Header("Layer Info")]
	public LayerMask gridLayerMask;
	public LayerMask gridInvalidLayerMask;

	[Header("Atmosphere Info")]
	public AtmosphereAttributeBase temperatureAtmosphere;
	public AtmosphereAttributeBase humidityAtmosphere;
	public AtmosphereAttributeBase windAtmosphere;

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
