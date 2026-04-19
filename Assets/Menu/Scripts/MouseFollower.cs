using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    public Camera cam;
    public float distanceFromCamera = 10f;

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;

        mousePos.z = distanceFromCamera;

        Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);

        transform.position = worldPos;
    }
}           