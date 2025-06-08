using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    public float defaultHealth = 10f;
    public float health = 10f;
    public float attackDamage = 2f;
    public float accuracy = 0.7f;
    private Vector3 startPosition;
    public float fallThresholdY;
    public float deaths;

    public float moveSpeed = 4.5f;
    public float jumpForce = 16f;
    public float coyoteTime = 0.1f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float pathfindingRayLength = 100f;

    public float decisionInterval = 2f;
    public float randomMoveChance = 0.5f;

    public float attackDelay = 0.5f;
    public float attackRange = 10f;
    public float knockbackForce = 13f; // Force this enemy applies to others
    public float selfKnockbackResistance = 1.0f; // How much *this enemy* resists knockback

    [SerializeField] private PlayerMovement playerScript;
    private Transform playerTransform;

    public Color enemyColor;
    private Rigidbody rb;
    private Renderer enemyRenderer;
    private float lastAttackTime = 0f;

    private float nextDecisionTime;
    private Vector3 currentHorizontalMoveDirection;

    private float lastGroundedTime;
    private bool isGrounded;

    // For applying knockback in FixedUpdate
    private Vector3 pendingKnockbackForce = Vector3.zero;
    private bool hasPendingKnockback = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            enabled = false;
            return;
        }

        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer == null)
        {

        }

        health = defaultHealth;

        if (playerScript == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerScript = playerObj.GetComponent<PlayerMovement>();
            }

            if (playerScript == null)
            {
                enabled = false;
                return;
            }
        }
        playerTransform = playerScript.transform;

        startPosition = transform.position;

        Color randomColor = Random.ColorHSV();
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = randomColor;
        }
        enemyColor = randomColor;

        nextDecisionTime = Time.time + decisionInterval;

        Vector3 initialDirection = playerTransform.position - transform.position;
        initialDirection.y = 0;
        currentHorizontalMoveDirection = initialDirection.normalized;
    }

    void Update()
    {
        if (playerTransform == null) return;

        if (transform.position.y < fallThresholdY || health <= 0)
        {
            health = 0;
            RespawnEnemy();
        }

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + decisionInterval;

            if (Random.value < randomMoveChance)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized;
                currentHorizontalMoveDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
            }
            else
            {
                Vector3 directionToPlayerTemp = playerTransform.position - transform.position;
                directionToPlayerTemp.y = 0;
                currentHorizontalMoveDirection = directionToPlayerTemp.normalized;
            }

            // Jumping logic initiation
            if (playerTransform.position.y > transform.position.y + 0.5f)
            {
                RaycastHit hit;
                Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
                Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized;

                if (!Physics.Raycast(rayOrigin, rayDirection, out hit, pathfindingRayLength, ~LayerMask.GetMask("Player", "Enemy")))
                {
                    isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer);
                    if (isGrounded || Time.time < lastGroundedTime + coyoteTime)
                    {
                        // Clear current vertical velocity to ensure consistent jump height
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    }
                }
            }
        }

        // Enemy rotation (non-physics)
        Vector3 directionToPlayerHorizontal = playerTransform.position - transform.position;
        directionToPlayerHorizontal.y = 0;
        if (directionToPlayerHorizontal != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayerHorizontal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);
        }

        // Attack timing and raycast (non-physics, event-driven)
        if (Time.time >= lastAttackTime + attackDelay)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) <= attackRange)
            {
                lastAttackTime = Time.time;

                if (Random.value < accuracy)
                {
                    RaycastHit hit;
                    Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
                    Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized;

                    if (Physics.Raycast(rayOrigin, rayDirection, out hit, attackRange) && hit.collider.CompareTag("Player"))
                    {
                        // Enemy calls Player's methods
                        Debug.Log("Hit player: " + hit.collider.gameObject.name + " Dealing Damage: " + attackDamage);

                        playerScript.TakeDamage(attackDamage);
                        playerScript.ApplyKnockback(transform.position, knockbackForce); // Enemy's position is the source
                    }
                }
            }
        }
    }

    void FixedUpdate() // All physics calculations should go here
    {
        // Ground check for FixedUpdate
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer);
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        // Apply enemy horizontal movement force
        Vector3 targetVelocity = currentHorizontalMoveDirection * moveSpeed;
        Vector3 velocityChange = targetVelocity - new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        // Apply pending knockback (if any)
        if (hasPendingKnockback)
        {
            rb.AddForce(pendingKnockbackForce, ForceMode.Impulse);
            pendingKnockbackForce = Vector3.zero;
            hasPendingKnockback = false;
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log(gameObject.name + " Health: " + health);

        if (enemyRenderer != null)
        {
            StopCoroutine("FlashRed");
            StartCoroutine(FlashRed(enemyRenderer));
        }

        if (health <= 0)
        {
            health = 0;
            RespawnEnemy();
        }
    }

    public void RespawnEnemy()
    {
        transform.position = startPosition;
        health = defaultHealth;
        deaths += 1;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Debug.Log(gameObject.name + " respawning to start position.");
    }

    // This method is called by external objects (like Player or Cactus) to knock back this enemy
    public void ApplyKnockback(Vector3 sourcePosition, float force)
    {
        if (rb != null)
        {
            Vector3 knockbackDirection = transform.position - sourcePosition;
            knockbackDirection.y = 0;
            knockbackDirection.Normalize();

            knockbackDirection.y = 0.1f;
            knockbackDirection.Normalize();

            // Apply resistance if enemy has it
            rb.AddForce(knockbackDirection * force * selfKnockbackResistance, ForceMode.Impulse);
            Debug.Log(gameObject.name + " knocked back by force: " + force * selfKnockbackResistance);
        }
    }

    IEnumerator FlashRed(Renderer rendererToFlash)
    {
        rendererToFlash.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        rendererToFlash.material.color = enemyColor;
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }
    }
}