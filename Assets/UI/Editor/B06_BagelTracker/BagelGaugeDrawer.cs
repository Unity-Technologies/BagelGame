using System;
using System.Collections.Generic;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // A custom UXML property drawer for BagelGauge, in UI Builder's and the UI Viewport's Inspector: the
    // usual attribute fields, then the three kinds of gauge as live previews to pick from. A click writes
    // kind through its SerializedProperty, so it undoes and saves to the UXML like any field edit.
    [CustomPropertyDrawer(typeof(BagelGauge.UxmlSerializedData))]
    public class BagelGaugeDrawer : UxmlSerializedDataPropertyDrawer
    {
        const string k_KindProperty = "kind";

        static readonly Color k_Tile = new(1f, 0.97f, 0.91f, 1f);
        static readonly Color k_Border = new(0f, 0f, 0f, 0.15f);
        static readonly Color k_Selected = new(0.87f, 0.28f, 0.17f, 1f);

        protected override void CreateChildPropertiesGUI(VisualElement container, SerializedProperty property)
        {
            base.CreateChildPropertiesGUI(container, property);

            var kindProperty = property.FindPropertyRelative(k_KindProperty);
            if (kindProperty == null)
                return;

            // The drawing loop recycles SerializedProperty instances: keep the object and the path instead.
            var serializedObject = property.serializedObject;
            var kindPath = kindProperty.propertyPath;

            var row = new VisualElement { name = "gauge-picker" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 4f;
            row.style.marginBottom = 4f;
            row.style.marginLeft = 16f;
            container.Add(row);

            var tiles = new List<VisualElement>();
            foreach (BagelGauge.Kind kind in Enum.GetValues(typeof(BagelGauge.Kind)))
            {
                var value = (int)kind;
                var tile = CreateTile(kind);
                tile.RegisterCallback<ClickEvent>(_ =>
                {
                    var p = serializedObject.FindProperty(kindPath);
                    if (p == null || p.enumValueIndex == value)
                        return;
                    p.enumValueIndex = value;
                    serializedObject.ApplyModifiedProperties();
                    Highlight(tiles, value);
                });
                tiles.Add(tile);
                row.Add(tile);
            }

            Highlight(tiles, kindProperty.enumValueIndex);

            // Follow edits made through the regular field, undo, or the UXML itself.
            row.TrackPropertyValue(kindProperty, p => Highlight(tiles, p.enumValueIndex));
        }

        // A tile with a live gauge of its kind, two-thirds full, and its name.
        static VisualElement CreateTile(BagelGauge.Kind kind)
        {
            var tile = new VisualElement { tooltip = ObjectNames.NicifyVariableName(kind.ToString()) };
            var s = tile.style;
            s.width = 64f;
            s.alignItems = Align.Center;
            s.marginRight = 4f;
            s.paddingTop = s.paddingBottom = 4f;
            s.backgroundColor = k_Tile;
            s.borderTopLeftRadius = s.borderTopRightRadius = s.borderBottomLeftRadius = s.borderBottomRightRadius = 6f;
            s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = 2f;

            var gauge = new BagelGauge { kind = kind, value = 0.66f, maxValue = 1f, pickingMode = PickingMode.Ignore };
            gauge.style.width = gauge.style.height = 44f;
            tile.Add(gauge);

            var label = new Label(ObjectNames.NicifyVariableName(kind.ToString())) { pickingMode = PickingMode.Ignore };
            label.style.color = new Color(0.23f, 0.12f, 0.08f);
            label.style.fontSize = 10f;
            tile.Add(label);
            return tile;
        }

        static void Highlight(List<VisualElement> tiles, int selected)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                var color = i == selected ? k_Selected : k_Border;
                var s = tiles[i].style;
                s.borderTopColor = s.borderBottomColor = s.borderLeftColor = s.borderRightColor = color;
            }
        }
    }
}
