using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[AddComponentMenu("Camera/Simple Smooth Mouse Look ")]
public class BasicPlayerController : MonoBehaviour {
	public static bool initialStartupGood = false;
	public bool active = false;
	public float speed = 2.0f;
	public float rotateSpeed = 3.0f;
    Vector2 _mouseAbsolute;
    Vector2 _smoothMouse;
 
    public Vector2 clampInDegrees = new Vector2(360, 180);
    public bool lockCursor;
    public Vector2 sensitivity = new Vector2(2, 2);
    public Vector2 smoothing = new Vector2(3, 3);
    public Vector2 targetDirection;
    public Vector2 targetCharacterDirection;
    public Vector3 velocity = Vector3.zero;
    // Assign this if there's a parent object controlling motion, such as a Character Controller.
    // Yaw rotation will affect this object instead of the camera if set.
    public GameObject characterBody;
 
    void Start()
    {
        // Set target direction to the camera's initial orientation.
        targetDirection = transform.localRotation.eulerAngles;
 
        // Set target direction for the character body to its inital state.
        if (characterBody)
            targetCharacterDirection = characterBody.transform.localRotation.eulerAngles;
    }
	void resetPositionOnTerrain()
	{
		characterBody.GetComponent<CharacterController> ().enabled = false;
		active = false;
		Vector3 origin = characterBody.transform.position;
		origin.y = 1000.0f;
		Vector3 direction = new Vector3 (0, -1.0f, 0);
		RaycastHit hit;
		if (Physics.Raycast (origin, direction, out hit)) {
			characterBody.transform.position = hit.point + new Vector3(0, 2, 0);
		}
	}

	void FixedUpdate()
	{
		//make sure the chunk we are in is loaded, otherwise stop us from moving.
		Chunk c = TerrainManager.getSingleton().getChunkByGrid (TerrainManager.getSingleton().worldPositionToChunkGrid (characterBody.transform.position));
		if (c == null || c.getChunkMesh () == null) {
            resetPositionOnTerrain();
        } else {
			characterBody.GetComponent<CharacterController> ().enabled = true;
			active = true;
			if (characterBody.GetComponent<CharacterController> ().isGrounded)
				initialStartupGood = true;
		}
		
	}
    void Update()
    {


        // Ensure the cursor is always locked when set
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
 
        // Allow the script to clamp based on a desired target value.
        var targetOrientation = Quaternion.Euler(targetDirection);
        var targetCharacterOrientation = Quaternion.Euler(targetCharacterDirection);
 
        // Get raw mouse input for a cleaner reading on more sensitive mice.
        var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
 
        // Scale input against the sensitivity setting and multiply that against the smoothing value.
        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));
 
        // Interpolate mouse movement over time to apply smoothing delta.
        _smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
        _smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);
 
        // Find the absolute mouse movement value from point zero.
        _mouseAbsolute += _smoothMouse;
 
        // Clamp and apply the local x value first, so as not to be affected by world transforms.
        if (clampInDegrees.x < 360)
            _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);
 
        // Then clamp and apply the global y value.
        if (clampInDegrees.y < 360)
            _mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);
 
        transform.localRotation = Quaternion.AngleAxis(-_mouseAbsolute.y, targetOrientation * Vector3.right) * targetOrientation;
 
        // If there's a character body that acts as a parent to the camera
        if (characterBody)
        {
            var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, Vector3.up);
            characterBody.transform.localRotation = yRotation * targetCharacterOrientation;
        }
        else
        {
            var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, transform.InverseTransformDirection(Vector3.up));
            transform.localRotation *= yRotation;
        }

		if (transform.position.y < 0) {
			//transform.position = new Vector3 (0, 500, 0);
			resetPositionOnTerrain ();
		}
		CharacterController controller = characterBody.GetComponent<CharacterController>();

		// Move forward / backward
		Vector3 forward = characterBody.transform.TransformDirection(Vector3.forward);
		Vector3 right = characterBody.transform.TransformDirection(Vector3.right);
		Vector3 jump = Vector3.zero;
		float gravity = 9.8f;
        velocity -= velocity * Time.deltaTime;
        velocity.y -= gravity * Time.deltaTime;
		if (controller.isGrounded)
			velocity.y = 0;
		if(controller.isGrounded&&Input.GetButtonDown("Jump"))
		{
			velocity.y=9.8f;
		}

		float curSpeedV = speed * Input.GetAxis("Vertical");
		float curSpeedH = speed * Input.GetAxis("Horizontal");
        float crouchMult = 1.0f;
        if(Input.GetButton("Crouch"))
        {
            crouchMult = 0.25f;
            controller.height -= 1.0f*Time.deltaTime;
            if (controller.height < 0.99f)
                controller.height = 0.99f;
        }
        else
        {
            controller.height += 1.0f * Time.deltaTime;
            if(controller.height>1.99f)
                controller.height = 1.99f;
        }
        float sprintMult = 1.0f;
        if (Input.GetButton("Sprint"))
        {
            sprintMult = 1.5f;
        }

		if (active) {
			controller.Move (((forward * curSpeedV * crouchMult * sprintMult) + (right * curSpeedH * crouchMult) + velocity) * Time.deltaTime);

		}
		TerrainManager.getSingleton ().CameraLocation = characterBody.transform.position;
    }
}
