using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts {

	public static class Randomization {
		public static T GetRandom<T>(IEnumerable<(float chance, T value)> valueChances) {
			List<(float chanceCumulative, T value)> cumulativeChances = new List<(float chanceCumulative, T value)>();
			float cumulativeChance = 0;
			foreach(var valueChance in valueChances) {
				cumulativeChance += valueChance.chance;
				cumulativeChances.Add((cumulativeChance, valueChance.value));
			}

			return cumulativeChances
				.First(x => x.chanceCumulative >= Random.Range(0f, cumulativeChance))
				.value;
		}

		public static T GetAndRemoveRandom<T>(this IList<T> list) {
			T result = list[Random.Range(0, list.Count)];
			list.Remove(result);
			return result;
		}
	}
}
