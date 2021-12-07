using UnityEngine;

public class CameraController : MonoBehaviour {
	public float CameraSpeed = 1f;
	public float CameraZoomSpeed = 1f;
	private Camera _camera;

	// Start is called before the first frame update
	void Start() {
		this._camera = GetComponent<Camera>();
	}

	// Update is called once per frame
	void Update() {
		Vector2 inputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * Time.deltaTime * this.CameraSpeed;
		if(_camera.orthographic) {
			this.transform.Translate(inputVector * _camera.orthographicSize);
			_camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize * (1 - Input.mouseScrollDelta.y * CameraZoomSpeed), 5, 30);
		} else {
			inputVector *= -this.transform.position.z;
			Vector3 movementVector = new Vector3(inputVector.x,inputVector.y, Input.mouseScrollDelta.y * CameraZoomSpeed * (-this.transform.position.z));
			this.transform.Translate(movementVector);
			float distanceClampCorrection = this.transform.position.z - Mathf.Clamp(this.transform.position.z, -500, -1);
			if(distanceClampCorrection != 0) {
				this.transform.Translate(Vector3.back * distanceClampCorrection);
			}
		}
	}
}
