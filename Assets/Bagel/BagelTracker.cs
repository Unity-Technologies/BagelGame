using System;
using UnityEngine;

public class BagelTracker : MonoBehaviour
{
    public BagelController bagelController;

    private void Update()
    {
        transform.position = bagelController.transform.position;

        var forward = bagelController.GetAbsoluteForward();        
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}
