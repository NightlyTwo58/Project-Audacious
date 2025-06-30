using UnityEngine;
using System.Collections;

public class EnemyNRLOSMovement : EnemyLOSMovement
{
    protected override void Respawn()
    {
        deaths += 1;
        Debug.Log(gameObject.name + " died (no respawn)");
        Destroy(gameObject);
    }
}
