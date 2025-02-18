using UnityEngine;

public class BagelController : MonoBehaviour
{

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Get current orientation:
        var currentOrientation = transform.forward.normalized;

        if (Input.GetKey(KeyCode.W))
            rb.AddForce(currentOrientation * 1);
        if (Input.GetKey(KeyCode.S))
            rb.AddForce(currentOrientation * -1);
        if (Input.GetKey(KeyCode.A))
            rb.AddTorque(Vector3.up * 1);
        if (Input.GetKey(KeyCode.D))
            rb.AddTorque(Vector3.down * 1);

    }
}
