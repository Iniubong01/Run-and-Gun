
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraLook : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float horizontalSensitivity = 0.15f;
    [SerializeField] private float verticalSensitivity = 0.15f;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [Header("Touch Settings")]
    [SerializeField] private bool useRightSideOfScreen = true;
    [SerializeField] private float touchAreaStartX = 0.5f;

    [Header("Camera")]
    [SerializeField] private Transform playerBody;

    private float xRotation;
    private int activeTouchId = -1;

    private void Start()
    {
        if (playerBody == null)
            playerBody = transform.parent;

        xRotation = transform.localEulerAngles.x;

        if (xRotation > 180f)
            xRotation -= 360f;
    }

    private void Update()
    {
        HandleTouchLook();
    }

    // Handle mobile touch look
    private void HandleTouchLook()
    {
        if (Input.touchCount == 0)
        {
            activeTouchId = -1;
            return;
        }

        // Find a touch for looking around
        if (activeTouchId == -1)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase != TouchPhase.Began)
                    continue;

                if (useRightSideOfScreen &&
                    touch.position.x < Screen.width * touchAreaStartX)
                    continue;

                if (EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                activeTouchId = touch.fingerId;
                break;
            }
        }

        // Continue looking with the selected finger
        if (activeTouchId != -1)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.fingerId != activeTouchId)
                    continue;

                if (touch.phase == TouchPhase.Moved)
                {
                    float touchX = touch.deltaPosition.x * horizontalSensitivity;
                    float touchY = touch.deltaPosition.y * verticalSensitivity;

                    ApplyLook(touchX, touchY);
                }

                if (touch.phase == TouchPhase.Ended ||
                    touch.phase == TouchPhase.Canceled)
                {
                    activeTouchId = -1;
                }

                break;
            }
        }
    }

    // Apply camera and player rotation
    private void ApplyLook(float lookX, float lookY)
    {
        // Rotate camera up and down
        xRotation -= lookY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotate player left and right
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * lookX);
    }
}