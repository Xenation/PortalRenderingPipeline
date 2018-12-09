using UnityEngine;

namespace PRPDemo {
	[RequireComponent(typeof(Rigidbody))]
	public class PlayerController : MonoBehaviour {

		[SerializeField] private float speed = 5f;
		[SerializeField] private float lookSpeed = 7.5f;
		[SerializeField] private float jumpForce = 10f;

		private Rigidbody rb;
		private Transform cam;

		private Vector3 velocity;
		private bool onGround = false;

		private void Awake() {
			rb = GetComponent<Rigidbody>();
			cam = GetComponentInChildren<Camera>().transform;
		}

		private void Update() {
			if (Input.GetMouseButtonDown(0)) {
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}

			if (Input.GetKeyDown(KeyCode.Escape)) {
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}

			float mouseX = Input.GetAxisRaw("Mouse X");
			float mouseY = Input.GetAxisRaw("Mouse Y");
			transform.rotation *= Quaternion.Euler(0f, mouseX * lookSpeed, 0f);
			cam.rotation *= Quaternion.Euler(-mouseY * lookSpeed, 0f, 0f);

			float strafe = Input.GetAxisRaw("Horizontal");
			float walk = Input.GetAxisRaw("Vertical");

			Vector3 groundVel = transform.forward * walk + transform.right * strafe;
			groundVel = groundVel.normalized * speed;
			velocity.x = groundVel.x;
			velocity.z = groundVel.z;

			if (Physics.CheckBox(transform.position + Vector3.down, new Vector3(.25f, .1f, .25f), Quaternion.identity, ~LayerMask.GetMask("Player"))) {
				onGround = true;
			} else {
				onGround = false;
			}

			if (onGround && Input.GetButtonDown("Jump")) {
				rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
			}
		}

		private void FixedUpdate() {
			velocity.y = rb.velocity.y;
			if (velocity.y > 20f) velocity.y = 20f;
			rb.velocity = velocity;
		}

	}
}
