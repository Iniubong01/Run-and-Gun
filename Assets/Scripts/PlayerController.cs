using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform camTransform;
    private Rigidbody rb;
    private Vector2 moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement = forward * moveInput.y + right * moveInput.x;

        if(movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        // Move player
        rb.linearVelocity = new Vector3(movement.x * moveSpeed,
        rb.linearVelocity.y, movement.z * moveSpeed);
    }

    public void MovePlayer (InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}
