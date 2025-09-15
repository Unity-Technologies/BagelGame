using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Bagel
{
    [CustomEditor(typeof(Bakery))]
    public class BakeryEditor : Editor
    {
        Bakery m_Bakery;

        void OnEnable()
        {
            m_Bakery = (Bakery)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            root.Add(new Button(m_Bakery.Rebuild) { text = "Rebuild" });

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            return root;
        }
    }
}
