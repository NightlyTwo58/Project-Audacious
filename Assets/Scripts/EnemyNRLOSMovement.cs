using UnityEngine;
using System.Collections;

public class EnemyNRLOSMovement : EnemyLOSMovement
{
    protected override void Respawn()
    {
        Debug.Log(gameObject.name + " died (no respawn)");
        Destroy(gameObject);
    }
}
