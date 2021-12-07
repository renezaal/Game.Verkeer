namespace Assets.Scripts.Extensions {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public static class ArrayExtensions {
		public static IEnumerable<T> AsEnumerable<T>(this T[,] array) {
			foreach(var item in array) {
				yield return item;
			}
		}
		public static IEnumerable<T> AsEnumerable<T>(this T[,,] array) {
			foreach(var item in array) {
				yield return item;
			}
		}
		public static IEnumerable<T> AsEnumerable<T>(this T[,,,] array) {
			foreach(var item in array) {
				yield return item;
			}
		}
	}
}
