using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Bagel
{
    [CustomEditor(typeof(LeaderboardData))]
    public class LeaderboardDataEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var row = new VisualElement();
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
            table.bindingPath = "Entries";

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

            table.columns["name"].makeCell = () => new TextField() { bindingPath = "playerName" };
            table.columns["toppings"].makeCell = () => new IntegerField() { bindingPath = "toppings" };

            table.itemsSource = (target as LeaderboardData).Entries;

            row.Add(table);
            return row;
        }
    }
}
