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
    public float coyoteTime = 0.1f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float fallThresholdY;
    private Vector3 startPosition;

    public float mouseSensitivity = 100f;

    public float interactionDistance = 20f;
    public LayerMask interactableLayer;

    private Rigidbody rb;
    private Camera playerCamera;
    private Renderer playerRenderer;
    public Color playerColor;

    public float deaths;

    private float lastGroundedTime;
    private float xRotation = 0f;
    private bool isGrounded;

    public Slider healthBarSlider;
    public TextMeshProUGUI healthTextDisplay; // Corrected type to TMPro.TextMeshProUGUI

    // For applying knockback in FixedUpdate
    private Vector3 pendingKnockbackForce = Vector3.zero;
    private bool hasPendingKnockback = false;

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
        JumpMovement(); // Jump input happens in Update, but force applied in FixedUpdate
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

                            if (enemyScript != null) // Always check for null
                            {
                                enemyScript.TakeDamage(attackDamage);
                                // Player applies knockback to the enemy
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

    void FixedUpdate() // All physics movement should be here
    {
        // Player's primary movement
        Vector3 targetHorizontalVelocity = MoveDirection() * moveSpeed;
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 horizontalVelocityChange = targetHorizontalVelocity - currentHorizontalVelocity;
        rb.AddForce(horizontalVelocityChange, ForceMode.VelocityChange);

        // Apply pending knockback
        if (hasPendingKnockback)
        {
            rb.AddForce(pendingKnockbackForce, ForceMode.Impulse);
            pendingKnockbackForce = Vector3.zero;
            hasPendingKnockback = false;
        }

        // Ground check for FixedUpdate (more reliable for physics)
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer);
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }
    }

    public Vector3 MoveDirection()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

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
        if (Input.GetButtonDown("Jump") && (isGrounded || Time.time < lastGroundedTime + coyoteTime))
        {
            // Clear current vertical velocity to ensure consistent jump height
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
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

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Debug.Log("Player respawning to start position.");
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log("Player Health: " + health);

        if (playerRenderer != null)
        {
            StopCoroutine("FlashRed"); // Stop any previous flash to start a new one cleanly
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

    // This method is called by external objects (like Enemy or Cactus) to knock back the player
    public void ApplyKnockback(Vector3 sourcePosition, float force)
    {
        if (rb != null)
        {
            Vector3 knockbackDirection = transform.position - sourcePosition;
            knockbackDirection.y = 0;
            knockbackDirection.Normalize();

            knockbackDirection.y = 0.2f;
            knockbackDirection.Normalize();

            // Store the force to be applied in FixedUpdate
            pendingKnockbackForce = knockbackDirection * force;
            hasPendingKnockback = true;

            Debug.Log(gameObject.name + " pending knockback by force: " + force);
        }
    }

    IEnumerator FlashRed(Renderer rendererToFlash)
    {
        rendererToFlash.material.color = Color.red;

        yield return new WaitForSeconds(0.2f);

        rendererToFlash.material.color = playerColor;
    }
}