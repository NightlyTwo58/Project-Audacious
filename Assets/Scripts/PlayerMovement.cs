using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    public bool canAttack = true;
    public float defaultHealth = 10f;
    public float health = 10f;
    public float attackDamage = 2f;

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
    public TMPro.TextMeshProUGUI healthTextDisplay;

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

        playerColor = Color.white;
    }

    void Update()
    {
        rb.linearVelocity = new Vector3(MoveDirection().x * moveSpeed, rb.linearVelocity.y, MoveDirection().z * moveSpeed);

        JumpMovement();

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
                            }

                            Rigidbody enemyRb = hit.collider.GetComponent<Rigidbody>();
                            if (enemyRb != null)
                            {
                                Vector3 pushDirection = ray.direction;
                                pushDirection.y = 0.5f;
                                pushDirection.Normalize();
                                enemyRb.AddForce(pushDirection * 5f, ForceMode.Impulse);
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
            StartCoroutine(FlashRed(playerRenderer));
        }

        if (healthBarSlider != null)
        {
            healthBarSlider.value = health;
        }
        if (healthTextDisplay != null)
        {
            healthTextDisplay.text = Mathf.CeilToInt(health).ToString() + "/" + defaultHealth.ToString();
        }

        if (health <= 0)
        {
            health = 0;
            RespawnPlayer();
        }
    }

    IEnumerator FlashRed(Renderer rendererToFlash)
    {
        rendererToFlash.material.color = Color.red;

        yield return new WaitForSeconds(0.2f);

        rendererToFlash.material.color = playerColor;
    }
}