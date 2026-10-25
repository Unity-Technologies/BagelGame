using System;
using UnityEngine;

namespace Bagel
{
    // The clock on the bakery's far wall tells the time where the player is. Its hands turn around their
    // up axis, from twelve o'clock.
    public class WallClock : MonoBehaviour
    {
        [SerializeField] Transform m_HourHand;
        [SerializeField] Transform m_MinuteHand;

        void Update()
        {
            var now = DateTime.Now;
            var minutes = now.Minute + now.Second / 60f;
            var hours = now.Hour % 12 + minutes / 60f;
            if (m_MinuteHand != null)
                m_MinuteHand.localRotation = Quaternion.Euler(0f, minutes * 6f, 0f);
            if (m_HourHand != null)
                m_HourHand.localRotation = Quaternion.Euler(0f, hours * 30f, 0f);
        }
    }
}
