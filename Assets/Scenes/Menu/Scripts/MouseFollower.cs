using UnityEngine;
using UnityEngine.InputSystem;

public class MouseFollower : MonoBehaviour
{
    public Camera cam;
    public float distanceFromCamera = 10f;

    void Update()
    {
        Vector3 mousePos = Mouse.current != null
            ? new Vector3(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue(), 0f)
            : Vector3.zero;

        mousePos.z = distanceFromCamera;

        Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);

        transform.position = worldPos;
    }
}           