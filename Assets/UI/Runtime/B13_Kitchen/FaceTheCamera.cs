using UnityEngine;

namespace Bagel
{
    // Turns a world-space panel to face the camera around the upright axis, like a sprite: the steam over the
    // coffee looks the same from wherever the bagel sees it.
    public class FaceTheCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            // A panel shows its front to -Z: turn +Z away from the camera.
            var away = transform.position - camera.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }
    }
}
