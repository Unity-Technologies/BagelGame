using System;
using UnityEngine;

namespace Bagel
{
    // On the lazy Susan's bodies (its top, and each bagel and card on it): tells whoever listens what knocks into it.
    [RequireComponent(typeof(Rigidbody))]
    public class BagelCarouselKnocks : MonoBehaviour
    {
        public event Action<Collision> onKnock;

        void OnCollisionEnter(Collision collision) => onKnock?.Invoke(collision);
    }
}
