using UnityEngine;
using System.Collections;

public abstract class Movement : MonoBehaviour
{
    public float defaultHealth = 10f;
    public float health = 10f;
    public float attackDamage = 2f;
    public float knockbackForce = 10f;
    public float fallThresholdY;
    public float deaths;

    protected Vector3 startPosition;
    protected Renderer entityRenderer;
    protected Color defaultColor;

    protected CharacterController characterController; // Used by Player
    protected Rigidbody rigidBody; // Used by Enemy

    protected float lastGroundedTime;
    protected float coyoteTime = 0.1f;
    protected bool isGrounded;

    protected Vector3 currentKnockbackVelocity = Vector3.zero;
    protected float knockbackDecay = 5f;

    protected virtual void Awake()
    {
        entityRenderer = GetComponent<Renderer>();
        if (entityRenderer != null)
        {
            defaultColor = entityRenderer.material.color;
        }

        startPosition = transform.position;
        health = defaultHealth;
    }

    protected virtual void Update()
    {
        if (transform.position.y < fallThresholdY || health <= 0)
        {
            health = 0;
            Respawn();
        }
    }

    public virtual void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log(gameObject.name + " Health: " + health);

        if (entityRenderer != null)
        {
            StopCoroutine("FlashRed");
            StartCoroutine(FlashRed(entityRenderer));
        }

        if (health <= 0)
        {
            health = 0;
            Respawn();
        }
    }

    public virtual void ApplyKnockback(Vector3 sourcePosition, float force)
    {
        Vector3 knockbackDirection = transform.position - sourcePosition;
        knockbackDirection.y = 0;
        knockbackDirection.Normalize();

        knockbackDirection.y = 0.5f;
        knockbackDirection.Normalize();

        currentKnockbackVelocity += knockbackDirection * force;
        Debug.Log(gameObject.name + " applied knockback by force: " + force);
    }

    protected virtual void Respawn()
    {
        transform.position = startPosition;
        health = defaultHealth;
        deaths += 1;
        Debug.Log(gameObject.name + " respawning to start position. Deaths: " + deaths);

        // Reset velocity for Rigidbody-based characters
        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }
        // Reset velocity for CharacterController-based characters (handled in PlayerMovement's Respawn)
    }

    public void ChangeColor(Color newColor)
    {
        if (entityRenderer != null && entityRenderer.material != null)
        {
            entityRenderer.material.color = newColor;
            defaultColor = newColor;
        }
    }

    protected IEnumerator FlashRed(Renderer rendererToFlash)
    {
        rendererToFlash.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        rendererToFlash.material.color = defaultColor;
    }

    protected virtual void HandleMovement() { }
    protected virtual void HandleJumpAndGravity() { }
}