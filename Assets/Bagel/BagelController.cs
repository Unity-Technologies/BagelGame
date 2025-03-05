using UnityEngine;

public class BagelController : MonoBehaviour
{
    public float rollTorque = 20.0f; // Torque applied for rolling
    public float tiltRecoverySpeed = 5.0f; // Speed at which it recovers its upright tilt

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Get current orientation:
        //var currentOrientation = transform.forward.normalized;

        if (Input.GetKey(KeyCode.W))
            rb.AddTorque(Vector3.right * rollTorque, ForceMode.Force);
        if (Input.GetKey(KeyCode.S))
            rb.AddTorque(Vector3.right * -rollTorque, ForceMode.Force);
        if (Input.GetKey(KeyCode.A))
            rb.AddTorque(Vector3.up * rollTorque, ForceMode.Force);
        if (Input.GetKey(KeyCode.D))
            rb.AddTorque(Vector3.down * rollTorque, ForceMode.Force);

        TiltUpright();
    }

    void TiltUpright()
    {
        Vector3 currentUp = transform.up;
        Vector3 targetUp = Vector3.up; // Always align with world's "up" direction
        Vector3 tiltCorrectionTorque = Vector3.Cross(currentUp, targetUp) * tiltRecoverySpeed;
        rb.AddTorque(tiltCorrectionTorque, ForceMode.Force);
    }
}
