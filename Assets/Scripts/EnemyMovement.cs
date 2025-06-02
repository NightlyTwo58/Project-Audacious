using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    public float defaultHealth = 10f;
    public float health = 10f;
    public float attackDamage = 2f;
    public float accuracy = 0.7f;
    private Vector3 startPosition;
    public float deaths;

    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float coyoteTime = 0.1f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    public float decisionInterval = 2f;
    public float jumpProbability = 0.2f;

    public float attackDelay = 0.5f;
    public float attackRange = 10f;
    public float knockbackForce = 10f;

    [SerializeField] private PlayerMovement playerScript;
    private Transform playerTransform;

    public Color enemyColor;
    private Rigidbody rb;
    private Renderer enemyRenderer;
    private float lastAttackTime = 0f;

    private float nextDecisionTime;
    private int currentHorizontalDirection = 0;

    private float lastGroundedTime;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError(gameObject.name + ": Rigidbody not found! Enemy script disabled.");
            enabled = false;
            return;
        }

        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer == null)
        {
            Debug.LogWarning(gameObject.name + ": Renderer not found. Enemy color change functionality unavailable.");
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
                Debug.LogError(gameObject.name + ": PlayerMovement script not found on Player! Enemy cannot attack player.");
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
    }

    void Update()
    {
        if (playerTransform == null) return;

        AIBasicMovement();

        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);
        }

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

                    Debug.DrawRay(rayOrigin, rayDirection * attackRange, Color.red, 0.5f);

                    if (Physics.Raycast(rayOrigin, rayDirection, out hit, attackRange) && hit.collider.CompareTag("Player"))
                    {
                        playerScript.TakeDamage(attackDamage);
                        Debug.Log(gameObject.name + " hit Player: " + hit.collider.gameObject.name + " Dealing Damage: " + attackDamage);

                        Rigidbody playerRb = hit.collider.GetComponent<Rigidbody>();
                        if (playerRb != null)
                        {
                            Vector3 knockbackDirection = rayDirection;
                            knockbackDirection.y = 0.5f;
                            knockbackDirection.Normalize();
                            playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                        }
                    }
                    else
                    {
                        Debug.Log(gameObject.name + " tried to hit Player, but raycast was blocked or player moved!");
                    }
                }
                else
                {
                    Debug.Log(gameObject.name + " intentionally missed Player.");
                }
            }
        }
    }

    void AIBasicMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer);
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + decisionInterval;

            currentHorizontalDirection = Random.Range(-1, 2);

            if (Random.value < jumpProbability && (isGrounded || Time.time < lastGroundedTime + coyoteTime))
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        rb.linearVelocity = new Vector3(currentHorizontalDirection * moveSpeed, rb.linearVelocity.y, 0f);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log("Enemy Health: " + health);

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
        Debug.Log(gameObject.name + " respawning.");
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
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }
    }
}