using System.Collections;
using UnityEngine;

namespace Bagel
{
    // The toaster at the end of the track. At game over the bagel that rolled goes in it: it flies up over the
    // toaster and is sliced in two, the halves open like a book, cut faces in, and drop into the slots. They
    // stay there until the next game. With Reduce Motion, they're just there.
    public class Toaster : MonoBehaviour
    {
        [Tooltip("The slots, from the toaster's feet, as its mesh has them: how high their tops are, how far apart their middles are (X), how deep they go.")]
        [SerializeField] float m_SlotTop = 1.81f;
        [SerializeField] float m_SlotSpacing = 0.75f;
        [SerializeField] float m_SlotDepth = 1f;
        [Tooltip("Inside the bagel, where it's cut.")]
        [SerializeField] Material m_CrumbMaterial;

        [Header("Toasting")]
        [Tooltip("A moment for the camera to get there first.")]
        [SerializeField] float m_Delay = 0.5f;
        [SerializeField] float m_FlyTime = 0.7f;
        [Tooltip("How far over the slots the bagel is sliced.")]
        [SerializeField] float m_SliceHeight = 2.4f;
        [SerializeField] float m_SliceTime = 0.7f;
        [SerializeField] float m_DropTime = 0.3f;

        PlayManager m_PlayManager;
        BagelController m_Bagel;
        Transform m_Toast;
        Transform m_TopHalf;
        Transform m_BottomHalf;
        BagelType m_ToastType;
        Coroutine m_Toasting;

        public bool toasting => m_Toasting != null;
        public bool hasToast => m_Toast != null && m_Toast.gameObject.activeSelf;
        public Transform topHalf => m_TopHalf;
        public Transform bottomHalf => m_BottomHalf;

        // Where each half ends up, in the toaster: in its slot, standing on its edge, cut face to the middle.
        public Vector3 SlotPosition(bool top) => new(m_SlotSpacing * (top ? 0.5f : -0.5f), m_SlotTop - m_SlotDepth, 0f);

        void OnEnable()
        {
            m_PlayManager = FindAnyObjectByType<PlayManager>();
            m_Bagel = FindAnyObjectByType<BagelController>(FindObjectsInactive.Include);
            if (m_PlayManager != null)
                m_PlayManager.state.onStateChange += State_OnStateChange;
        }

        void OnDisable()
        {
            if (m_PlayManager != null)
                m_PlayManager.state.onStateChange -= State_OnStateChange;
            m_Toasting = null;
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state == PlayManagerState.State.Playing)
            {
                // A new game: the toast is gone, and a new bagel rolls.
                if (m_Toasting != null)
                    StopCoroutine(m_Toasting);
                m_Toasting = null;
                if (m_Toast != null)
                    m_Toast.gameObject.SetActive(false);
                ShowRollingBagel(true);
            }
            else if (state == PlayManagerState.State.GameOver && IsNearestToTheBagel())
            {
                if (m_Toasting != null)
                    StopCoroutine(m_Toasting);
                m_Toasting = StartCoroutine(Toast(m_PlayManager.state.selectedBagelType, m_PlayManager.state.settings.reduceMotion));
            }
        }

        // The bagel goes in the toaster it ended up by.
        bool IsNearestToTheBagel()
        {
            if (m_Bagel == null)
                return true;
            var distance = Vector3.Distance(transform.position, m_Bagel.transform.position);
            foreach (var toaster in FindObjectsByType<Toaster>())
            {
                if (toaster != this && Vector3.Distance(toaster.transform.position, m_Bagel.transform.position) < distance)
                    return false;
            }
            return true;
        }

        void ShowRollingBagel(bool show)
        {
            if (m_Bagel == null)
                return;
            foreach (var renderer in m_Bagel.GetComponentsInChildren<Renderer>())
                renderer.enabled = show;
        }

        IEnumerator Toast(BagelType bagelType, bool reduceMotion)
        {
            if (bagelType == null || bagelType.topHalfMesh == null || bagelType.bottomHalfMesh == null)
            {
                m_Toasting = null;
                yield break;
            }
            MakeToast(bagelType);

            // In the slots, and the half of the bagel's thickness either side of the middle when it's whole.
            var radius = bagelType.topHalfMesh.bounds.extents.x;
            var thickness = bagelType.bottomHalfMesh.bounds.size.y;
            var openRotation = Quaternion.Euler(0f, 0f, -90f);
            var inTopSlot = SlotPosition(true) + Vector3.up * radius;
            var inBottomSlot = SlotPosition(false) + Vector3.up * radius;

            if (reduceMotion)
            {
                ShowRollingBagel(false);
                m_Toast.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                m_TopHalf.SetLocalPositionAndRotation(inTopSlot, openRotation);
                m_BottomHalf.SetLocalPositionAndRotation(inBottomSlot, openRotation);
                m_Toasting = null;
                yield break;
            }

            // Whole, where the bagel was, as it was.
            var model = m_Bagel != null ? m_Bagel.GetComponentInChildren<MeshFilter>() : null;
            var fromPosition = model != null ? model.transform.TransformPoint(model.sharedMesh.bounds.center) : transform.position + Vector3.up * 4f;
            var fromRotation = model != null ? model.transform.rotation : transform.rotation;
            m_Toast.SetPositionAndRotation(fromPosition, fromRotation);
            m_TopHalf.SetLocalPositionAndRotation(Vector3.up * (thickness * 0.5f), Quaternion.identity);
            m_BottomHalf.SetLocalPositionAndRotation(Vector3.down * (thickness * 0.5f), Quaternion.identity);
            m_Toast.gameObject.SetActive(false);

            yield return new WaitForSeconds(m_Delay);
            ShowRollingBagel(false);
            m_Toast.gameObject.SetActive(true);

            // Up in an arc over the toaster, turning to lie flat.
            var hover = Vector3.up * (m_SlotTop + m_SliceHeight);
            var toPosition = transform.TransformPoint(hover);
            var arc = Mathf.Max(1.5f, (toPosition - fromPosition).magnitude * 0.25f);
            for (var time = 0f; time < m_FlyTime; time += Time.deltaTime)
            {
                var t = Smooth(time / m_FlyTime);
                m_Toast.SetPositionAndRotation(Vector3.Lerp(fromPosition, toPosition, t) + Vector3.up * (arc * 4f * t * (1f - t)), Quaternion.Slerp(fromRotation, transform.rotation, t));
                yield return null;
            }
            m_Toast.SetLocalPositionAndRotation(hover, Quaternion.identity);

            // Sliced: the top lifts off, then both halves open like a book over the slots.
            var lift = Vector3.up * (thickness * 0.5f + 0.4f);
            var overTopSlot = inTopSlot + Vector3.up * (m_SlotDepth + 0.25f) - hover;
            var overBottomSlot = inBottomSlot + Vector3.up * (m_SlotDepth + 0.25f) - hover;
            var topStart = m_TopHalf.localPosition;
            var bottomStart = m_BottomHalf.localPosition;
            for (var time = 0f; time < m_SliceTime; time += Time.deltaTime)
            {
                var t = time / m_SliceTime;
                var lifting = Smooth(Mathf.Clamp01(t / 0.35f));
                var opening = Smooth(Mathf.Clamp01((t - 0.3f) / 0.7f));
                var top = Vector3.Lerp(topStart, lift, lifting);
                m_TopHalf.SetLocalPositionAndRotation(Vector3.Lerp(top, overTopSlot, opening), Quaternion.Slerp(Quaternion.identity, openRotation, opening));
                m_BottomHalf.SetLocalPositionAndRotation(Vector3.Lerp(bottomStart, overBottomSlot, opening), Quaternion.Slerp(Quaternion.identity, openRotation, opening));
                yield return null;
            }

            // And down into the slots, faster and faster, with a little jolt as they land.
            for (var time = 0f; time < m_DropTime; time += Time.deltaTime)
            {
                var t = time / m_DropTime;
                t *= t;
                m_TopHalf.localPosition = Vector3.Lerp(overTopSlot, inTopSlot - hover, t);
                m_BottomHalf.localPosition = Vector3.Lerp(overBottomSlot, inBottomSlot - hover, t);
                yield return null;
            }
            m_Toast.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_TopHalf.SetLocalPositionAndRotation(inTopSlot, openRotation);
            m_BottomHalf.SetLocalPositionAndRotation(inBottomSlot, openRotation);

            var scale = transform.localScale;
            for (var time = 0f; time < 0.25f; time += Time.deltaTime)
            {
                var squash = Mathf.Sin(time / 0.25f * Mathf.PI) * 0.06f;
                transform.localScale = Vector3.Scale(scale, new Vector3(1f + squash * 0.5f, 1f - squash, 1f + squash * 0.5f));
                yield return null;
            }
            transform.localScale = scale;
            m_Toasting = null;
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

        // The two halves of this bagel type, under the toaster: the crust and its toppings as the bagel has them,
        // the crumb where it's cut. Each half's origin is in its middle.
        void MakeToast(BagelType bagelType)
        {
            if (m_Toast == null)
            {
                m_Toast = new GameObject("Toast").transform;
                m_Toast.SetParent(transform, false);
                m_TopHalf = MakeHalf("TopHalf");
                m_BottomHalf = MakeHalf("BottomHalf");
            }
            m_Toast.gameObject.SetActive(true);
            if (m_ToastType == bagelType)
                return;
            m_ToastType = bagelType;

            var crust = bagelType.modelPrefab != null ? bagelType.modelPrefab.GetComponentInChildren<MeshRenderer>().sharedMaterials : new Material[1];
            var topMaterials = new Material[crust.Length + 1];
            crust.CopyTo(topMaterials, 0);
            topMaterials[crust.Length] = m_CrumbMaterial;
            SetHalf(m_TopHalf, bagelType.topHalfMesh, topMaterials);
            SetHalf(m_BottomHalf, bagelType.bottomHalfMesh, new[] { crust[0], m_CrumbMaterial });
        }

        Transform MakeHalf(string name)
        {
            var half = new GameObject(name).transform;
            half.SetParent(m_Toast, false);
            var model = new GameObject("Model");
            model.transform.SetParent(half, false);
            model.AddComponent<MeshFilter>();
            model.AddComponent<MeshRenderer>();
            return half;
        }

        static void SetHalf(Transform half, Mesh mesh, Material[] materials)
        {
            var model = half.GetChild(0);
            model.GetComponent<MeshFilter>().sharedMesh = mesh;
            model.GetComponent<MeshRenderer>().sharedMaterials = materials;
            model.localPosition = -mesh.bounds.center;
        }
    }
}
