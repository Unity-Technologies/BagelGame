using System;
using UnityEngine;

public class BagelTracker : MonoBehaviour
{
    public BagelController bagelController;

    private void Update()
    {
        transform.position = bagelController.transform.position;

        var right = bagelController.transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 forward = Quaternion.AngleAxis(-90f, Vector3.up) * right;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}
