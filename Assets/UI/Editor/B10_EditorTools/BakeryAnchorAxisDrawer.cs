using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Bagel
{
    // One of a bakery anchor's axes on one row, lined up with the fields round it: the part of the room it keeps its
    // distance from, then which side of it.
    [CustomPropertyDrawer(typeof(BakeryAnchor.Axis))]
    public class BakeryAnchorAxisDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var part = new EnumField(property.displayName, default(Bakery.Part)) { tooltip = property.tooltip };
            part.AddToClassList(BaseField<int>.alignedFieldUssClassName);
            part.BindProperty(property.FindPropertyRelative(nameof(BakeryAnchor.Axis.part)));

            var side = new EnumField(default(BakeryAnchor.Side)) { tooltip = property.tooltip };
            side.BindProperty(property.FindPropertyRelative(nameof(BakeryAnchor.Axis.side)));
            side.style.flexGrow = 1f;
            side.style.flexBasis = 0f;
            side.style.marginRight = 0f;

            // The side's beside the part, sharing the row.
            var input = part.Q(className: BaseField<int>.inputUssClassName);
            input.style.flexBasis = 0f;
            part.Add(side);
            return part;
        }
    }
}
