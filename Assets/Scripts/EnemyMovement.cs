using UnityEngine;
using System.Collections;

public class EnemyMovement : Movement
{
    public float moveSpeed = 4.5f;
    public float jumpForce = 16f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float pathfindingRayLength = 100f;

    public float decisionInterval = 0.5f;
    public float randomMoveChance = 0.1f;

    public float attackDelay = 0.5f;
    public float attackRange = 10f;
    public float accuracy = 0.7f;
    public float selfKnockbackResistance = 1.0f;

    [SerializeField] protected PlayerMovement playerScript;
    protected Transform playerTransform;

    protected float lastAttackTime = 0f;
    protected float nextDecisionTime;
    protected Vector3 currentHorizontalMoveDirection;

    protected Vector3 pendingKnockbackForce = Vector3.zero;
    protected bool hasPendingKnockback = false;

    protected override void Awake()
    {
        base.Awake();
        rigidBody = GetComponent<Rigidbody>();
        if (rigidBody == null)
        {
            Debug.LogError("Rigidbody not found on EnemyMovement script's GameObject! Movement disabled.");
            enabled = false;
            return;
        }

        if (entityRenderer == null)
        {
            Debug.LogWarning("Renderer not found on EnemyMovement script's GameObject.");
        }
        else
        {
            entityRenderer.material.color = Random.ColorHSV();
            defaultColor = entityRenderer.material.color;
        }

        InitializePlayerReference();

        nextDecisionTime = Time.time + decisionInterval;
        if (playerTransform != null)
        {
            Vector3 initialDirection = playerTransform.position - transform.position;
            initialDirection.y = 0;
            currentHorizontalMoveDirection = initialDirection.normalized;
        }
        else
        {
            currentHorizontalMoveDirection = Vector3.forward;
        }
    }

    protected virtual void InitializePlayerReference()
    {
        if (playerScript == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerScript = playerObj.GetComponent<PlayerMovement>();
            }

            if (playerScript == null)
            {
                Debug.LogWarning("PlayerMovement script not found on GameObject with tag 'Player'. Enemy AI might not function correctly.");
                enabled = false;
                return;
            }
        }
        playerTransform = playerScript.transform;
    }

    protected override void Update()
    {
        base.Update();

        if (playerTransform == null) return;

        MakeDecision();
        FacePlayer();
        PerformAttack();
    }

    protected virtual void MakeDecision()
    {
        if (Time.time < nextDecisionTime) return;

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

        AttemptJump();
    }

    protected virtual void AttemptJump()
    {
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
                    rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, 0, rigidBody.linearVelocity.z);
                    rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                }
            }
        }
    }

    protected virtual void FacePlayer()
    {
        Vector3 directionToPlayerHorizontal = playerTransform.position - transform.position;
        directionToPlayerHorizontal.y = 0;
        if (directionToPlayerHorizontal != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayerHorizontal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);
        }
    }

    protected virtual void PerformAttack()
    {
        if (Time.time < lastAttackTime + attackDelay) return;
        if (Vector3.Distance(transform.position, playerTransform.position) > attackRange) return;

        lastAttackTime = Time.time;
        if (Random.value < accuracy)
        {
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized;

            if (Physics.Raycast(rayOrigin, rayDirection, out hit, attackRange) && hit.collider.CompareTag("Player"))
            {
                playerScript.TakeDamage(attackDamage);
                playerScript.ApplyKnockback(transform.position, knockbackForce);
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer);
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        Vector3 targetVelocity = currentHorizontalMoveDirection * moveSpeed;
        Vector3 velocityChange = targetVelocity - new Vector3(rigidBody.linearVelocity.x, 0, rigidBody.linearVelocity.z);
        rigidBody.AddForce(velocityChange, ForceMode.VelocityChange);

        if (hasPendingKnockback)
        {
            rigidBody.AddForce(pendingKnockbackForce * selfKnockbackResistance, ForceMode.Impulse);
            pendingKnockbackForce = Vector3.zero;
            hasPendingKnockback = false;
        }
    }

    public override void ApplyKnockback(Vector3 sourcePosition, float force)
    {
        Vector3 knockbackDirection = transform.position - sourcePosition;
        knockbackDirection.y = 0;
        knockbackDirection.Normalize();

        knockbackDirection.y = 0.1f;
        knockbackDirection.Normalize();

        pendingKnockbackForce = knockbackDirection * force;
        hasPendingKnockback = true;
    }

    protected override void Respawn()
    {
        base.Respawn();
    }

    protected virtual void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }
    }
}