using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    public float rotationAngle = 90f;
    public float rotationSpeed = 2f;
    public bool isOpen = false;

    [SerializeField] private Transform doorAxel;
    private Quaternion closedRotation;
    private Transform playerTransform;
    private bool isRotating = false;

    public float interactionDistance = 5f;

    void Start()
    {
        if (doorAxel == null)
        {
            Debug.LogError("DoorController: 'doorAxel' Transform is not assigned. Please assign the GameObject representing the door's axel in the Inspector.", this);
            enabled = false;
            return;
        }

        closedRotation = doorAxel.rotation;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        playerTransform = playerObject.transform;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionDistance))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    ToggleDoor();
                }
            }
        }
    }

    public void ToggleDoor()
    {
        if (isRotating) return;

        isRotating = true;
        StopAllCoroutines();

        Quaternion targetRotation;

        if (isOpen)
        {
            targetRotation = closedRotation;
        }
        else
        {
            Vector3 playerDir = playerTransform.position - doorAxel.position;
            playerDir.y = 0;

            Vector3 doorLocalForward = transform.position - doorAxel.position;
            doorLocalForward.y = 0;

            float crossProductY = Vector3.Cross(doorLocalForward.normalized, playerDir.normalized).y;

            if (crossProductY < 0)
            {
                targetRotation = closedRotation * Quaternion.Euler(0, rotationAngle, 0);
            }
            else
            {
                targetRotation = closedRotation * Quaternion.Euler(0, -rotationAngle, 0);
            }
        }

        StartCoroutine(RotateDoorSmoothly(targetRotation));
    }

    private IEnumerator RotateDoorSmoothly(Quaternion targetRotation)
    {
        float time = 0;
        Quaternion startRotation = doorAxel.rotation;

        while (time < 1)
        {
            doorAxel.rotation = Quaternion.Slerp(startRotation, targetRotation, time);
            time += Time.deltaTime * rotationSpeed;
            yield return null;
        }

        doorAxel.rotation = targetRotation;
        isOpen = !isOpen;
        isRotating = false;
    }
}
