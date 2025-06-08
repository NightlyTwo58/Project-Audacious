using UnityEngine;
using System.Collections;

public class CactusScript : MonoBehaviour
{
    public float damageAmount = 2f;
    public float damageInterval = 0.5f;
    public float damageRange = 1.5f;
    public float knockbackForce = 13f;

    private Transform playerTransform;
    [SerializeField] private PlayerMovement playerScript;

    private bool isAttacking = false;

    void Start()
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
                enabled = false;
                return;
            }
        }
        playerTransform = playerScript.transform;
    }

    void Update()
    {
        if (playerTransform == null)
        {
            enabled = false;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= damageRange && !isAttacking)
        {
            StartCoroutine(DamagePlayerCoroutine());
        }
        else if (distanceToPlayer > damageRange && isAttacking)
        {
            StopAllCoroutines();
            isAttacking = false;
        }
    }

    private IEnumerator DamagePlayerCoroutine()
    {
        isAttacking = true;

        while (Vector3.Distance(transform.position, playerTransform.position) <= damageRange)
        {
            playerScript.TakeDamage(damageAmount);

            playerScript.ApplyKnockback(transform.position, knockbackForce);

            yield return new WaitForSeconds(damageInterval);
        }

        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRange);
    }
}