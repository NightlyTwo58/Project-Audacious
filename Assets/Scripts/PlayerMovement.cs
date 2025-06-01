using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Combat
    public bool canAttack = true; // Use this to control if attacking is allowed
    public float health = 10f;
    public float attackDamage = 2f;


    // --- Movement Settings (Adjust in Inspector) ---
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float coyoteTime = 0.1f;
    public GameObject Enemy; // Consider making this an interface or a more generic type
    public Transform groundCheck;
    public LayerMask groundLayer;

    // --- Mouse Look Settings (Adjust in Inspector) ---
    public float mouseSensitivity = 100f;

    // NEW: Raycast settings (Move these up with other public settings)
    public float interactionDistance = 20f; // How far the player can "point"
    public LayerMask interactableLayer;    // Select layers the raycast should hit (e.g., "Enemy" layer)


    // --- Private References (Automatically assigned in Start) ---
    private Rigidbody rb;
    private Camera playerCamera;
    private Renderer playerRenderer; // For changing cube color

    // --- Internal State Variables ---
    private float lastGroundedTime;
    private float xRotation = 0f; // Stores vertical camera rotation (pitch)
    private bool isGrounded;       // Tracks if the player is on the ground

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on PlayerMovement script's GameObject! Movement disabled.");
            enabled = false;
            return;
        }

        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("Main Camera not found! Ensure it's tagged 'MainCamera' in the Inspector. Camera control disabled.");
        }

        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null)
        {
            Debug.LogWarning("Renderer not found on PlayerMovement script's GameObject. Color change functionality unavailable.");
        }

        // Lock and hide the mouse cursor at the very start of the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Player Movement (Horizontal - XZ Plane) ---
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        // Debug.Log($"Vertical Input: {verticalInput}"); // Temporarily remove debug logs if spamming
        // Debug.Log($"Horizontal Input: {horizontalInput}"); // Temporarily remove debug logs if spamming


        Vector3 camForward = playerCamera != null ? playerCamera.transform.forward : Vector3.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = playerCamera != null ? playerCamera.transform.right : Vector3.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDirection = camForward * verticalInput + camRight * horizontalInput;

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);

        // --- Jumping ---
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer);

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        if (Input.GetButtonDown("Jump") && (isGrounded || Time.time < lastGroundedTime + coyoteTime))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // --- Mouse Look (POV Control) ---
        // Only allow mouse look if the cursor is locked (i.e., we are in "gameplay" mode)
        if (playerCamera != null && Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            transform.Rotate(Vector3.up * mouseX);
        }

        // --- Cursor Lock/Unlock & Primary Mouse Button Logic ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If Escape is pressed, unlock and show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0)) // 0 is the left mouse button
        {
            // If Left Mouse Button is clicked:
            if (Cursor.lockState == CursorLockMode.None)
            {
                // If cursor was unlocked, lock it and hide it (resume gameplay)
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else // Cursor was already locked (we are in gameplay mode), so this is an attack/interact click
            {
                // Attacking logic
                if (canAttack) // Only attack if allowed
                {
                    Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                    Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red); // Use interactionDistance for debug draw
                    RaycastHit hit;

                    // Use the interactableLayer for the raycast if you want to filter what can be attacked
                    if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer) && hit.collider.CompareTag("Enemy"))
                    {
                        Debug.Log("Hit Enemy: " + hit.collider.gameObject.name + " Dealing Damage: " + attackDamage);
                        // Apply damage
                        // EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                        // if (enemyHealth != null) { enemyHealth.TakeDamage(attackDamage); }

                        // Change color for visual feedback
                        if (Enemy != null && Enemy.GetComponent<Renderer>() != null)
                        {
                            Enemy.GetComponent<Renderer>().material.color = Color.blue;
                        }

                    }
                    else
                    {
                        Debug.Log("Left click but didn't hit an Enemy.");
                    }
                }
            }
        }

        // --- Example: Change cube color on 'P' key press (Legacy Input Manager) ---
        if (Input.GetKeyDown(KeyCode.P))
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