using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro; // Add this for TextMeshPro

public class PlayerMovement : MonoBehaviour
{
    public bool canAttack = true;
    public float defaultHealth = 10f;
    public float health = 10f;
    public float attackDamage = 2f;
    public float knockbackForce = 10f; // Force this player applies to others

    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float gravity = -9.81f;
    public float coyoteTime = 0.1f;
    // We won't need groundCheck or groundLayer directly for CharacterController.isGrounded
    // public Transform groundCheck;
    // public LayerMask groundLayer;
    public float fallThresholdY;
    private Vector3 startPosition;

    public float mouseSensitivity = 100f;

    public float interactionDistance = 20f;
    public LayerMask interactableLayer;

    private CharacterController controller; // Changed from Rigidbody to CharacterController
    private Camera playerCamera;
    private Renderer playerRenderer;
    public Color playerColor;

    public float deaths;

    private float lastGroundedTime;
    private float xRotation = 0f;
    private Vector3 verticalVelocity;

    public Slider healthBarSlider;
    public TextMeshProUGUI healthTextDisplay;

    // For applying knockback with CharacterController (different approach needed)
    private Vector3 currentKnockbackVelocity = Vector3.zero;
    public float knockbackDecay = 5f; // How quickly knockback force dissipates

    void Start()
    {
        controller = GetComponent<CharacterController>(); // Get the CharacterController
        if (controller == null)
        {
            Debug.LogError("CharacterController not found on PlayerMovement script's GameObject! Movement disabled.");
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        health = defaultHealth;

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = defaultHealth;
            healthBarSlider.minValue = 0f;
            healthBarSlider.value = health;
        }
        else
        {
            Debug.LogWarning("Health Bar Slider not assigned to PlayerMovement script.");
        }

        if (healthTextDisplay != null)
        {
            healthTextDisplay.text = Mathf.CeilToInt(health).ToString() + "/" + defaultHealth.ToString();
        }

        playerColor = Color.white;
    }

    void Update()
    {
        // Handle horizontal movement
        HandleMovement();

        // Handle vertical movement (gravity and jump)
        HandleJumpAndGravity();

        MouseLook();

        if (transform.position.y < fallThresholdY || health <= 0)
        {
            health = 0;
            RespawnPlayer();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                if (canAttack)
                {
                    Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                    Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
                    {
                        if (hit.collider.CompareTag("Enemy"))
                        {
                            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow);
                            Debug.Log("Hit Enemy: " + hit.collider.gameObject.name + " Dealing Damage: " + attackDamage);

                            EnemyMovement enemyScript = hit.collider.GetComponent<EnemyMovement>();

                            if (enemyScript != null)
                            {
                                enemyScript.TakeDamage(attackDamage);
                                enemyScript.ApplyKnockback(transform.position, knockbackForce);
                            }
                        }
                        else
                        {
                            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.white);
                            Debug.Log("Hit: " + hit.collider.gameObject.name + " but it's not an Enemy.");
                        }
                    }
                    else
                    {
                        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.white);
                        Debug.Log("Did not Hit anything on interactableLayer.");
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ChangeCubeColor(Random.ColorHSV());
        }
    }

    void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        moveDirection.Normalize(); // Normalize to prevent faster diagonal movement

        // Apply knockback first, then regular movement, and decay knockback
        Vector3 totalMovement = (moveDirection * moveSpeed + currentKnockbackVelocity) * Time.deltaTime;
        controller.Move(totalMovement + verticalVelocity * Time.deltaTime);

        // Decay knockback over time
        if (currentKnockbackVelocity.magnitude > 0.1f) // Threshold to stop decaying
        {
            currentKnockbackVelocity = Vector3.Lerp(currentKnockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
        }
        else
        {
            currentKnockbackVelocity = Vector3.zero; // Snap to zero to prevent tiny residual values
        }
    }

    void HandleJumpAndGravity()
    {
        // isGrounded from CharacterController is more reliable than custom ground check
        bool isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; // Small downward force to keep grounded
            lastGroundedTime = Time.time;
        }

        if (Input.GetButtonDown("Jump") && (isGrounded || Time.time < lastGroundedTime + coyoteTime))
        {
            verticalVelocity.y = jumpForce;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
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

    public void ChangeCubeColor(Color newColor)
    {
        if (playerRenderer != null && playerRenderer.material != null)
        {
            playerRenderer.material.color = newColor;
            playerColor = newColor;
        }
    }

    public void RespawnPlayer()
    {
        transform.position = startPosition;
        health = defaultHealth;
        deaths += 1;

        healthBarSlider.value = health;
        healthTextDisplay.text = Mathf.CeilToInt(health).ToString() + "/" + defaultHealth.ToString();

        // Reset CharacterController velocity after respawn
        verticalVelocity = Vector3.zero;
        currentKnockbackVelocity = Vector3.zero;

        Debug.Log("Player respawning to start position.");
    }

    // OnDrawGizmos is still useful for debugging, but groundCheck won't be used for CharacterController's isGrounded
    // void OnDrawGizmos()
    // {
    //     if (groundCheck != null)
    //     {
    //         Gizmos.color = Color.yellow;
    //         Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
    //     }
    // }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log("Player Health: " + health);

        if (playerRenderer != null)
        {
            StopCoroutine("FlashRed");
            StartCoroutine(FlashRed(playerRenderer));
        }

        healthBarSlider.value = health;
        healthTextDisplay.text = Mathf.CeilToInt(health).ToString() + "/" + defaultHealth.ToString();

        if (health <= 0)
        {
            health = 0;
            RespawnPlayer();
        }
    }

    public void ApplyKnockback(Vector3 sourcePosition, float force)
    {
        Vector3 knockbackDirection = transform.position - sourcePosition;
        knockbackDirection.y = 0; // Keep knockback horizontal
        knockbackDirection.Normalize();

        // Apply an upward component to the knockback for a more "bouncy" feel
        knockbackDirection.y = 0.5f;
        knockbackDirection.Normalize();

        currentKnockbackVelocity += knockbackDirection * force;
        Debug.Log(gameObject.name + " applied knockback by force: " + force);
    }

    IEnumerator FlashRed(Renderer rendererToFlash)
    {
        rendererToFlash.material.color = Color.red;

        yield return new WaitForSeconds(0.2f);

        rendererToFlash.material.color = playerColor;
    }
}