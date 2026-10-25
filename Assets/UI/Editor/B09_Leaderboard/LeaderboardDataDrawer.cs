using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Bagel
{
    [CustomPropertyDrawer(typeof(LeaderboardData))]
    public class LeaderboardDataDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new VisualElement();
            row.Add(new Label(property.displayName));

            var table = new MultiColumnListView
            {
                name = "leaderboard-table",
                showAddRemoveFooter = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                selectionType = SelectionType.None,
                showBorder = true,
                style =
                {
                    flexGrow = 1,
                    height = 300
                }
            };
            table.bindingPath = "m_Entries";

            table.columns.Add(new Column
            {
                name = "name",
                title = "Player Name",
                bindingPath = "playerName",
                stretchable = true
            });
            table.columns.Add(new Column
            {
                name = "toppings",
                title = "Toppings",
                bindingPath = "toppings",
                stretchable = true
            });
            table.columns.Add(new Column
            {
                name = "time",
                title = "Time (s)",
                bindingPath = "time",
                stretchable = true
            });

            table.columns["name"].makeCell = () => new TextField() { bindingPath = "playerName" };
            table.columns["toppings"].makeCell = () => new IntegerField() { bindingPath = "toppings" };
            table.columns["time"].makeCell = () => new FloatField() { bindingPath = "time" };

            table.BindProperty(property.FindPropertyRelative("m_Entries"));

            row.Add(table);
            return row;
        }
    }
}
