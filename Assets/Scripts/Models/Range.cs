namespace Assets.Scripts.Models {
	using UnityEngine;

	public struct Range {
		public float Minimum;
		public float Maximum;
		public float GetRandomInRange() => Random.Range(this.Minimum, this.Maximum);
	}
}
