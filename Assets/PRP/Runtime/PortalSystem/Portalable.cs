using UnityEngine;

namespace PRP.PortalSystem {
	public class Portalable : MonoBehaviour {

		private Vector3 previousPosition;

		private void Awake() {
			previousPosition = transform.position;
		}

		private void Update() {
			Vector3 currentPosition = transform.position;

			Portal transporter;
			if (PortalsManager.I.CheckThroughPortal(previousPosition, currentPosition, out transporter)) {
				transform.position = currentPosition = transporter.TransformPosition(currentPosition);
			}
			previousPosition = currentPosition;
		}

	}
}
