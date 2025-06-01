using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    // Combat
    public bool canAttack = true; // Use this to control if attacking is allowed
    public float health = 10f;
    public float attackDamage = 2f;

    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float coyoteTime = 0.1f;
    public GameObject Enemy; // Consider making this an interface or a more generic type
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float fallThresholdY;
    private Vector3 startPosition;

    public float mouseSensitivity = 100f;

    public float interactionDistance = 20f; // How far the player can "point"
    public LayerMask interactableLayer;    // Select layers the raycast should hit (e.g., "Enemy" layer)

    private Rigidbody rb;
    private Camera playerCamera;
    private Renderer playerRenderer; // For changing cube color

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

        startPosition = transform.position;

        // Lock and hide the mouse cursor at the very start of the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        rb.linearVelocity = new Vector3(MoveDirection().x * moveSpeed, rb.linearVelocity.y, MoveDirection().z * moveSpeed);

        JumpMovement();

        MouseLook();

        if (transform.position.y < fallThresholdY)
        {
            RespawnPlayer();
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

                    if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, interactableLayer))
                    {
                        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
                        Debug.Log("Hit Enemy: " + hit.collider.gameObject.name + " Dealing Damage: " + attackDamage);
                        // Apply damage
                        // EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                        // if (enemyHealth != null) { enemyHealth.TakeDamage(attackDamage); }


                        Rigidbody enemyRb = hit.collider.GetComponent<Rigidbody>();
                        enemyRb.AddForce(MoveDirection() * moveSpeed, ForceMode.Impulse);

                        //hit.collider.GetComponent<Rigidbody>.linearVelocity = new Vector3(MoveDirection().x * moveSpeed, rb.linearVelocity.y, MoveDirection().z * moveSpeed);
                        Renderer enemyRenderer = Enemy.GetComponent<Renderer>();
                        StartCoroutine(FlashRed(enemyRenderer));
                    }
                    else
                    {
                        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
                        Debug.Log("Did not Hit");
                    }
                }
            }
        }

        // color change logic
        if (Input.GetKeyDown(KeyCode.P))
        {
            ChangeCubeColor(Random.ColorHSV());
        }
    }

    public Vector3 MoveDirection()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        // Debug.Log($"Vertical Input: {verticalInput}");
        // Debug.Log($"Horizontal Input: {horizontalInput}");

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

        return moveDirection;
    }

    public void JumpMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer);

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        if (Input.GetButtonDown("Jump") && (isGrounded || Time.time < lastGroundedTime + coyoteTime))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    public void MouseLook()
    {
        if (playerCamera != null && Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            transform.Rotate(Vector3.up * mouseX);
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

    public void RespawnPlayer()
    {
        transform.position = startPosition;

        // reset velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("Player fell too far! Respawning to start position.");
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

    IEnumerator FlashRed(Renderer rendererToFlash)
    {
        Color originalColor = rendererToFlash.material.color;
        rendererToFlash.material.color = Color.red;

        yield return new WaitForSeconds(0.2f);

        rendererToFlash.material.color = originalColor;
    }
}