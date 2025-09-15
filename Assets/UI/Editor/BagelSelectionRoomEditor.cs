using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Bagel
{
    [CustomEditor(typeof(BagelSelectionRoom))]
    public class BagelSelectionRoomEditor : Editor
    {
        BagelSelectionRoom m_BagelSelectionRoom;

        void OnEnable()
        {
            m_BagelSelectionRoom = (BagelSelectionRoom)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            root.Add(new Button(m_BagelSelectionRoom.InitBagelCollection) { text = "Rebuild" });

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            return root;
        }
    }
}
