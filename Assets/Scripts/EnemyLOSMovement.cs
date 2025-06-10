using UnityEngine;
using System.Collections;

public class EnemyLOSMovement : EnemyMovement
{
    private bool hasLOS;
    public float losCheckInterval = 0.1f; // How often to check for LOS
    private float nextLOSCheckTime;

    protected override void Awake()
    {
        base.Awake();
        nextLOSCheckTime = Time.time + losCheckInterval;
    }

    protected override void Update()
    {
        base.Update();

        if (playerTransform == null) return;

        CheckLineOfSight();
    }

    private void CheckLineOfSight()
    {
        if (Time.time < nextLOSCheckTime) return;

        nextLOSCheckTime = Time.time + losCheckInterval;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f; // Adjust this origin based on enemy's "eyes" or center
        Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized;

        RaycastHit hit;

        // Construct the LayerMask:
        // Layers to EXCLUDE from the raycast:
        int layersToIgnore = LayerMask.GetMask("Enemy", "Ignore Raycast"); // "Enemy" is your enemy's own layer, "Ignore Raycast" is a Unity built-in layer for non-blocking objects

        // Invert the ignore mask to get a mask for layers to HIT.
        // This means the ray will hit everything *except* the "Enemy" and "Ignore Raycast" layers.
        // This is crucial because "Player" and "Default" (terrain) layers WILL be hit.
        int finalLayerMask = ~layersToIgnore;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, pathfindingRayLength, finalLayerMask))
        {
            // The ray hit something. Now, check if that something is the player.
            if (hit.collider.CompareTag("Player"))
            {
                // The ray hit the player first (or directly), so LOS is clear.
                hasLOS = true;
                Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.green, losCheckInterval); // Green for clear LOS
            }
            else
            {
                // The ray hit something else (like the terrain on the "Default" layer) before hitting the player.
                // This means LOS is blocked.
                hasLOS = false;
                Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.red, losCheckInterval); // Red for blocked LOS
                // Uncomment for debugging: Debug.Log(gameObject.name + " LOS blocked by: " + hit.collider.gameObject.name);
            }
        }
        else
        {
            // The ray didn't hit anything within the specified 'pathfindingRayLength'.
            // This means either the player is out of range, or there are no obstacles and no player in range.
            // For LOS logic, if the ray doesn't hit the player, then LOS is considered false.
            hasLOS = false;
            Debug.DrawRay(rayOrigin, rayDirection * pathfindingRayLength, Color.blue, losCheckInterval); // Blue for nothing hit in range
        }

        Debug.Log(hasLOS);
    }

    protected override void MakeDecision()
    {
        if (Time.time < nextDecisionTime) return;

        nextDecisionTime = Time.time + decisionInterval;

        if (hasLOS)
        {
            // If LOS is clear, move towards the player (or use random chance if desired)
            if (Random.value < randomMoveChance)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized;
                currentHorizontalMoveDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
                // Debug.Log(gameObject.name + " LOS: Random move.");
            }
            else
            {
                Vector3 directionToPlayerTemp = playerTransform.position - transform.position;
                directionToPlayerTemp.y = 0;
                currentHorizontalMoveDirection = directionToPlayerTemp.normalized;
                // Debug.Log(gameObject.name + " LOS: Moving towards player.");
            }
        }
        else
        {
            // If LOS is blocked, just move randomly
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            currentHorizontalMoveDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
            // Debug.Log(gameObject.name + " LOS: Blocked, moving randomly.");
        }

        AttemptJump();
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
    }
}