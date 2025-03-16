using System;
using UnityEngine;

public class BagelTracker : MonoBehaviour
{
    public BagelController bagelController;

    private void Awake()
    {
        
    }

    private void Update()
    {
        transform.position = bagelController.transform.position;
        transform.rotation = Quaternion.Euler( 0f, bagelController.transform.rotation.eulerAngles.y, 0f );
    }
}
