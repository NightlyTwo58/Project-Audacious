using UnityEngine;
using System.Collections; // Required for Coroutines

public class DoorController : MonoBehaviour
{
    public float rotationAngle = 90f;
    public float rotationSpeed = 2f;
    public bool isOpen = false;
    private Quaternion initialRotation;
    private Quaternion openRotation;
    public float interactionDistance = 5f;

    void Start()
    {
        initialRotation = transform.localRotation;
        openRotation = initialRotation * Quaternion.Euler(0, rotationAngle, 0);
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning("DoorController: No Collider found on " + gameObject.name + ". Raycasting will not work correctly without a Collider.", this);
        }
    }

    void Update()
    {
        // Right-click
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
        isOpen = !isOpen;
        StopAllCoroutines();

        if (isOpen)
        {
            StartCoroutine(RotateDoor(openRotation));
        }
        else
        {
            StartCoroutine(RotateDoor(initialRotation));
        }
    }

    private IEnumerator RotateDoor(Quaternion targetRotation)
    {
        float time = 0;
        Quaternion startRotation = transform.localRotation; // Store the door's current rotation when the coroutine starts

        while (time < 1)
        {
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, time);

            time += Time.deltaTime * rotationSpeed;

            yield return null;
        }

        transform.localRotation = targetRotation;
    }
}