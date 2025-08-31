using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [CustomEditor(typeof(BagelSelectionRoom))]
    public class BagelSelectionRoomEditor : Editor
    {
        //BagelSelectionRoom bagelSelectionRoom;

        //void OnEnable()
        //{
        //    bagelSelectionRoom = (BagelSelectionRoom)target;
        //}

        //public override VisualElement CreateInspectorGUI()
        //{
        //    var root = new VisualElement();

        //    InspectorElement.FillDefaultInspector(root, serializedObject, this);

        //    var pf = root.Query<PropertyField>().Where(f => f.bindingPath == "m_BagelTypeCollection").First();
        //    pf?.RegisterCallback<ChangeEvent<Object>>(e => bagelSelectionRoom.InitBagelCollection());

        //    return root;
        //}
    }
}
