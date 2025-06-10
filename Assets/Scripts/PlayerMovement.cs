using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class PlayerMovement : Movement
{
    public bool canAttack = true;
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float gravity = -9.81f;

    public float mouseSensitivity = 100f;
    public float interactionDistance = 20f;
    public LayerMask interactableLayer;

    private Camera playerCamera;
    private float xRotation = 0f;
    private Vector3 verticalVelocity;

    public Slider healthBarSlider;
    public TextMeshProUGUI healthTextDisplay;

    protected override void Awake()
    {
        base.Awake();
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
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

        if (entityRenderer == null)
        {
            Debug.LogWarning("Renderer not found on PlayerMovement script's GameObject. Color change functionality unavailable.");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateHealthUI();
    }

    protected override void Update()
    {
        base.Update();

        HandleMovement();
        HandleJumpAndGravity();
        MouseLook();
        HandleInput();
    }

    protected override void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        moveDirection.Normalize();

        Vector3 totalMovement = (moveDirection * moveSpeed + currentKnockbackVelocity) * Time.deltaTime;
        characterController.Move(totalMovement + verticalVelocity * Time.deltaTime);

        if (currentKnockbackVelocity.magnitude > 0.1f)
        {
            currentKnockbackVelocity = Vector3.Lerp(currentKnockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
        }
        else
        {
            currentKnockbackVelocity = Vector3.zero;
        }
    }

    protected override void HandleJumpAndGravity()
    {
        isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
            lastGroundedTime = Time.time;
        }

        if (Input.GetButtonDown("Jump") && (isGrounded || Time.time < lastGroundedTime + coyoteTime))
        {
            verticalVelocity.y = jumpForce;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
    }

    private void MouseLook()
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

    private void HandleInput()
    {
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
                PerformAttack();
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ChangeColor(Random.ColorHSV());
        }
    }

    private void PerformAttack()
    {
        if (!canAttack) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red, 1f);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow, 1f);
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
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.white, 1f);
                Debug.Log("Hit: " + hit.collider.gameObject.name + " but it's not an Enemy.");
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.white, 1f);
            Debug.Log("Did not Hit anything on interactableLayer.");
        }
    }

    public void ChangeColor(Color newColor)
    {
        if (entityRenderer != null && entityRenderer.material != null)
        {
            entityRenderer.material.color = newColor;
            defaultColor = newColor;
        }
    }

    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
        UpdateHealthUI();
    }

    protected override void Respawn()
    {
        base.Respawn();
        verticalVelocity = Vector3.zero;
        currentKnockbackVelocity = Vector3.zero;
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = defaultHealth;
            healthBarSlider.value = health;
        }
        if (healthTextDisplay != null)
        {
            healthTextDisplay.text = Mathf.CeilToInt(health).ToString() + "/" + defaultHealth.ToString();
        }
    }
}