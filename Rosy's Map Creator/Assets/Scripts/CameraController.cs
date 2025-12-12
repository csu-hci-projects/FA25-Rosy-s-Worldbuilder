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
    public float dragSpeed = 1f;       // multiplier for middle-button panning
    public float dragRotateSpeed = 1f; // multiplier for right-button rotation

    // --- Click-to-follow settings ---
    [Header("Click To Follow")]
    public LayerMask selectableLayers;      // set in Inspector
    public bool clickToToggleFollow = true; // click same target again to stop following

    private Transform followTarget = null;
    private Vector3 followOffset = Vector3.zero;

    // internal drag state
    private bool isMiddleDragging = false;
    private bool isRightDragging = false;
    private Vector2 lastMousePosition;

    public Vector3 newPosition;
    public Quaternion newRotation;
    public Vector3 newZoom;

    void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = Camera.main.transform.localPosition;
    }

    void Update()
    {
        HandleClickToFollow();
        HandleMovementInput();
    }

    void HandleClickToFollow()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Ignore clicks on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            RaycastHit hit;

            // No layer mask — just raycast everything
            if (Physics.Raycast(ray, out hit, 1000f))
            {
                // Check tag instead of layer
                if (hit.transform.CompareTag("Units"))
                {
                    // If clicking same target, toggle off
                    if (followTarget == hit.transform && clickToToggleFollow)
                    {
                        followTarget = null;
                        return;
                    }

                    followTarget = hit.transform;
                    followOffset = newPosition - followTarget.position;

                    followTarget.GetComponent<UnitController>()?.Selected();
                }
                else
                {
                    // Clicked something without the tag → stop following (optional)
                    if (clickToToggleFollow)
                    {
                        followTarget.GetComponent<UnitController>()?.Deselect();
                        followTarget = null;
                    }
                }
            }
            else
            {
                // Clicked empty space → stop following (optional)
                if (clickToToggleFollow)
                {
                    followTarget?.GetComponent<UnitController>()?.Deselect();
                    followTarget = null;
                }
            }
        }
    }

    // ---------------- MAIN MOVEMENT ----------------
    void HandleMovementInput()
    {
        Vector3 direction = Vector3.zero;
        var mouse = Mouse.current;

        // If we are following something, lock position to it each frame
        if (followTarget != null)
        {
            newPosition = followTarget.position + followOffset;
        }

        // --- Mouse handling: middle-click drag to pan, right-click drag to rotate ---
        if (mouse != null)
        {
            // Start middle drag
            if (mouse.middleButton.wasPressedThisFrame)
            {
                // Disable panning when following a unit
                if (followTarget == null)
                {
                    isMiddleDragging = true;
                    lastMousePosition = mouse.position.ReadValue();
                }
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

            // Middle drag: pan camera target (only if not following)
            if (isMiddleDragging && mouse.middleButton.isPressed && followTarget == null)
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

            // Only allow WASD panning when not following a target
            if (followTarget == null)
            {
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
            }

            // Rotation keys allowed even when following
            if (keyboard.qKey.isPressed)
                newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
            if (keyboard.eKey.isPressed)
                newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);

            // Zoom keys allowed even when following
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

        // Scroll wheel zoom
        if (mouse != null)
        {
            float scroll = mouse.scroll.y.ReadValue();

            // Stop zooming if hovering over UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                scroll = 0f;

            if (scroll != 0)
            {
                float zoomStrength = Mathf.Lerp(
                    zoomAmount.magnitude * 0.5f,
                    zoomAmount.magnitude * 2f,
                    Mathf.InverseLerp(5f, 50f, newZoom.magnitude)
                );
                newZoom += zoomAmount * scroll * zoomStrength * 1f;
                newZoom = ClampZoom(newZoom);
            }
        }

        // Apply smoothed transform updates
        transform.position = Vector3.Lerp(transform.position, newPosition, movementTime * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, movementTime * Time.deltaTime);
        Camera.main.transform.localPosition = Vector3.Lerp(Camera.main.transform.localPosition, newZoom, movementTime * Time.deltaTime);
    }


    private Vector3 ClampZoom(Vector3 zoom)
    {
        zoom.y = Mathf.Clamp(zoom.y, 50f, 500f);
        zoom.z = Mathf.Clamp(zoom.z, -500f, -50f);
        return zoom;
    }
}
