using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // --- Movement Settings (Adjust in Inspector) ---
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float coyoteTime = 0.1f;
    public Transform groundCheck; // Drag your 'GroundCheck' Empty GameObject here
    public LayerMask groundLayer; // Select the 'Ground' layer here

    // --- Mouse Look Settings (Adjust in Inspector) ---
    public float mouseSensitivity = 100f;

    // --- Private References (Automatically assigned in Start) ---
    private Rigidbody rb;
    private Camera playerCamera;
    private Renderer playerRenderer; // For changing cube color

    // --- Internal State Variables ---
    private float lastGroundedTime;
    private float xRotation = 0f; // Stores vertical camera rotation (pitch)
    private bool isGrounded;      // Tracks if the player is on the ground

    void Start()
    {
        // Get the Rigidbody component from this GameObject
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on PlayerMovement script's GameObject! Movement disabled.");
            enabled = false; // Disable the script if essential component is missing
            return;
        }

        // Get the Main Camera reference
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("Main Camera not found! Ensure it's tagged 'MainCamera' in the Inspector. Camera control disabled.");
            // Don't return, as movement might still work
        }

        // Get the Renderer component (for changing color)
        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null)
        {
            Debug.LogWarning("Renderer not found on PlayerMovement script's GameObject. Color change functionality unavailable.");
        }

        // Lock and hide the mouse cursor for game control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Player Movement (Horizontal - XZ Plane) ---
        // Get input using the Legacy Input Manager
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow
        float verticalInput = Input.GetAxis("Vertical");   // W/S or Up/Down Arrow
        Debug.Log($"Vertical Input: {verticalInput}");
        Debug.Log($"Horizontal Input: {horizontalInput}");


        // Calculate the camera's forward and right vectors, flattened to the XZ plane.
        Vector3 camForward = playerCamera != null ? playerCamera.transform.forward : Vector3.forward;
        camForward.y = 0; // Flatten the vector
        camForward.Normalize(); // Ensure unit length

        Vector3 camRight = playerCamera != null ? playerCamera.transform.right : Vector3.right;
        camRight.y = 0; // Flatten the vector
        camRight.Normalize(); // Ensure unit length

        // Combine input with flattened camera directions
        Vector3 moveDirection = camForward * verticalInput + camRight * horizontalInput;

        // Prevent faster diagonal movement by normalizing if magnitude > 1
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // Apply movement using Rigidbody.linearVelocity
        rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);

        // --- Jumping ---
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer);

        if (isGrounded) {
            lastGroundedTime = Time.time;
        }

        if (Input.GetButtonDown("Jump") && (isGrounded || Time.time < lastGroundedTime + coyoteTime)) {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // --- Mouse Look (POV Control) ---
        if (playerCamera != null)
        {
            // Get mouse movement using Legacy Input Manager
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            // Vertical rotation (looking up/down) applied to the camera itself
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Horizontal rotation (looking left/right) applied to the player object
            transform.Rotate(Vector3.up * mouseX);
        }

        // --- Optional: Reset cursor lock on Escape key press (Legacy Input Manager) ---
        // --- Cursor Lock/Unlock Logic ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If Escape is pressed, unlock and show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0)) // 0 is the left mouse button
        {
            // If Left Mouse Button is clicked, lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // --- Example: Change cube color on 'P' key press (Legacy Input Manager) ---
        if (Input.GetKeyDown(KeyCode.P)) // Using KeyCode for a specific key
        {
            ChangeCubeColor(Random.ColorHSV());
        }
    }

    // --- Helper Method for Color Change (No change needed here) ---
    public void ChangeCubeColor(Color newColor)
    {
        if (playerRenderer != null && playerRenderer.material != null)
        {
            playerRenderer.material.color = newColor;
        }
    }

    // --- Editor-only Visualization (No change needed here) ---
    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }
    }
}