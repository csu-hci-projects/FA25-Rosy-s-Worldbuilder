using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{

    public float nomralSpeed;
    public float fastSpeed;
    public float movementSpeed;
    public float movementTime;
    public float rotationAmount;
    public Vector3 zoomAmount;
    public float dragSpeed = 1f; // multiplier for middle-button panning
    public float dragRotateSpeed = 1f; // multiplier for right-button rotation

    // internal drag state
    private bool isMiddleDragging = false;
    private bool isRightDragging = false;
    private Vector2 lastMousePosition;

    public Vector3 newPosition;
    public Quaternion newRotation;
    public Vector3 newZoom;

    public CameraLimits cameraLimits = new CameraLimits();

    void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = Camera.main.transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovementInput();
    }

    void HandleMovementInput()
    {
        Vector3 direction = Vector3.zero;
        var mouse = Mouse.current;

        // --- Mouse handling: middle-click drag to pan, right-click drag to rotate ---
        if (mouse != null)
        {
            // Start middle drag
            if (mouse.middleButton.wasPressedThisFrame)
            {
                isMiddleDragging = true;
                lastMousePosition = mouse.position.ReadValue();
            }
            // End middle drag
            if (mouse.middleButton.wasReleasedThisFrame)
            {
                isMiddleDragging = false;
            }

            // Start right drag
            if (mouse.rightButton.wasPressedThisFrame)
            {
                isRightDragging = true;
                lastMousePosition = mouse.position.ReadValue();
            }
            // End right drag
            if (mouse.rightButton.wasReleasedThisFrame)
            {
                isRightDragging = false;
            }

            // Middle drag: pan camera target
            if (isMiddleDragging && mouse.middleButton.isPressed)
            {
                Vector2 current = mouse.position.ReadValue();
                Vector2 delta = current - lastMousePosition;
                lastMousePosition = current;

                Vector3 right = transform.right;
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 move = (right * -delta.x + forward * -delta.y) * (dragSpeed * 0.01f);
                newPosition += move;
            }

            // Right drag: rotate camera target (yaw and pitch)
            if (isRightDragging && mouse.rightButton.isPressed)
            {
                Vector2 current = mouse.position.ReadValue();
                Vector2 delta = current - lastMousePosition;
                lastMousePosition = current;

                float yawDelta = delta.x * dragRotateSpeed * 0.1f;
                float pitchDelta = -delta.y * dragRotateSpeed * 0.1f; // invert so dragging up looks up

                // Convert current rotation to Euler to clamp pitch
                Vector3 euler = newRotation.eulerAngles;
                float currentPitch = euler.x;
                if (currentPitch > 180f) currentPitch -= 360f; // convert to -180..180
                float newPitch = Mathf.Clamp(currentPitch + pitchDelta, -15f, 45f);
                float newYaw = euler.y + yawDelta;

                newRotation = Quaternion.Euler(newPitch, newYaw, 0f);
            }
        }

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.shiftKey.isPressed)
            {
                movementSpeed = fastSpeed;
            }
            else
            {
                movementSpeed = nomralSpeed;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                direction += transform.forward;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                direction -= transform.forward;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                direction -= transform.right;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                direction += transform.right;

            if (direction != Vector3.zero)
                newPosition += direction.normalized * movementSpeed;

            if (keyboard.qKey.isPressed)
                newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
            if (keyboard.eKey.isPressed)
                newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);
            if (keyboard.rKey.isPressed)
            {
                newZoom += zoomAmount;
                newZoom = ClampZoom(newZoom);
            }
            if (keyboard.fKey.isPressed)
            {
                newZoom -= zoomAmount;
                newZoom = ClampZoom(newZoom);
            }
        }
        //Scroll wheel zoom

        float scroll = Mouse.current.scroll.y.ReadValue();
        // Stop zooming if hovering over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            scroll = 0f;


        if (scroll != 0)
        {
            float zoomStrength = Mathf.Lerp(zoomAmount.magnitude * 0.5f, zoomAmount.magnitude * 2f, Mathf.InverseLerp(5f, 50f, newZoom.magnitude));
            newZoom += zoomAmount * scroll * zoomStrength * 1f;
            newZoom = ClampZoom(newZoom);
        }


        transform.position = Vector3.Lerp(transform.position, newPosition, movementTime * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, movementTime * Time.deltaTime);
        Camera.main.transform.localPosition = Vector3.Lerp(Camera.main.transform.localPosition, newZoom, movementTime * Time.deltaTime);

    }

    public void SetCameraLimits(float minX, float maxX, float minZ, float maxZ)
    {
        cameraLimits.minX = minX;
        cameraLimits.maxX = maxX;
        cameraLimits.minZ = minZ;
        cameraLimits.maxZ = maxZ;
    }

    private Vector3 ClampZoom(Vector3 zoom)
    {
        zoom.y = Mathf.Clamp(zoom.y, 50f, 500f);
        zoom.z = Mathf.Clamp(zoom.z, -500f, -50f);
        return zoom;
    }


}
