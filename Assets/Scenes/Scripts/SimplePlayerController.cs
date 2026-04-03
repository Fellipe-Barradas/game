using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 200f;

    private Rigidbody rb;
    private float rotationY;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // ROTAÇÃO COM O MOUSE (ESQUERDA/DIREITA)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        rotationY += mouseX;

        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
    }

    void FixedUpdate()
    {
        // MOVIMENTO (WASD)
        float moveZ = Input.GetAxis("Vertical");
        float moveX = Input.GetAxis("Horizontal");

        Vector3 move = transform.forward * moveZ + transform.right * moveX;

        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);
    }
}