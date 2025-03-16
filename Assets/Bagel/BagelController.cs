using UnityEditor;
using UnityEngine;

public class BagelController : MonoBehaviour
{
    public float rollTorque = 10.0f; // Torque applied for rolling
    public float turnSpeed = 100.0f;
    public float turnTorque = 5.0f;

    public float tiltRecoverySpeed = 10.0f; // Speed at which it recovers its upright tilt

    Rigidbody rb;

    float orientationAngle = 0f;

    public Vector3 GetAbsoluteRight()
    {
        var right = transform.right;
        right.y = 0f;
        right.Normalize();
        return right;
    }

    public Vector3 GetAbsoluteForward()
    {
        var right = GetAbsoluteRight();
        var forward = Quaternion.AngleAxis(-90f, Vector3.up) * right;
        return forward;
    }

    public Vector3 GetNonRotatedRelativeUp()
    {
        var right = transform.right;
        var absoluteForward = GetAbsoluteForward();
        var up = Vector3.Cross(absoluteForward, right);
        return up;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        orientationAngle = 0f;
    }

    void FixedUpdate()
    {
        // Get current orientation:
        //var currentOrientation = transform.forward.normalized;

        if (Input.GetKey(KeyCode.W))
            rb.AddTorque(transform.right * rollTorque, ForceMode.Force);
        if (Input.GetKey(KeyCode.S))
            rb.AddTorque(transform.right * -rollTorque, ForceMode.Force);
        if (Input.GetKey(KeyCode.A)) {
            rb.AddTorque(GetNonRotatedRelativeUp() * -turnTorque, ForceMode.Force);
            //rb.transform.Rotate(transform.up, -turnSpeed * Time.deltaTime);
            //orientationAngle -= turnSpeed * Time.deltaTime;

        }
        if (Input.GetKey(KeyCode.D)) {
            rb.AddTorque(GetNonRotatedRelativeUp() * turnTorque, ForceMode.Force);
            //rb.transform.Rotate(transform.up, turnSpeed * Time.deltaTime);
            //orientationAngle += turnSpeed * Time.deltaTime;
        }

        CorrectForOrientation();
        TiltUpright();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, GetAbsoluteForward() * 5f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, GetAbsoluteRight() * 5f);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, GetNonRotatedRelativeUp() * 5f);
    }

    void CorrectForOrientation()
    {
        // Get the current orientation
        var currentForward = GetAbsoluteForward();
        Debug.Log("Current Forward: " + currentForward);
        var orientationForward = Quaternion.AngleAxis(orientationAngle, Vector3.up) * Vector3.forward;
        Debug.Log("Orientation Forward: " + orientationForward);

        float angle = Vector3.SignedAngle(currentForward, orientationForward, Vector3.up);
        Debug.Log("Angle: " + angle);

        // Calculate the torque needed to correct the orientation
        var torqueAxis = transform.up;
        var correctiveTorque = torqueAxis * (angle * turnTorque);
        Debug.Log("Corrective Torque: " + correctiveTorque);

        //rb.AddTorque(correctiveTorque, ForceMode.Force);
    }

    void TiltUpright()
    {
        var currentRight = transform.right;
        var globalUp = Vector3.up;

        float tiltAngle = Vector3.Angle(currentRight, globalUp) - 90f;
        Debug.Log("Tilt Angle: " + tiltAngle);

        var tiltAxis = GetAbsoluteForward();
        var correctiveTorque = tiltAxis * (tiltAngle * tiltRecoverySpeed);

        rb.AddTorque(correctiveTorque, ForceMode.Force);
    }
}
