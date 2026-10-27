using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Bagel
{
    // The Editor's theme, in a window that shows the game's own UI: taken off the window's root, so none
    // of the Editor's looks leak into the game's screens, and given to the parts of the window that are
    // Editor UI (a toolbar, a list, an inspector). Newer editors keep their theme on an ancestor of the
    // root: it's borrowed while the window is attached, and given back when it detaches (closed, docked
    // elsewhere). The Bagel All UI, Bagel Levels and Bagel Features windows each have one.
    public sealed class BagelEditorTheme
    {
        readonly VisualElement m_Root;
        readonly List<StyleSheet> m_Sheets = new();
        readonly List<(VisualElement owner, StyleSheet sheet)> m_Borrowed = new();
        readonly List<VisualElement> m_Parts = new();

        public BagelEditorTheme(VisualElement root)
        {
            m_Root = root;
            for (int i = 0; i < root.styleSheets.count; i++)
                m_Sheets.Add(root.styleSheets[i]);
            root.styleSheets.Clear();
            root.RegisterCallback<AttachToPanelEvent>(_ => Lift());
            root.RegisterCallback<DetachFromPanelEvent>(_ => Restore());
        }

        // Editor UI: the Editor's theme is on it.
        public void Share(VisualElement part)
        {
            if (part == null)
                return;
            if (!m_Parts.Contains(part))
                m_Parts.Add(part);
            if (m_Root.panel != null)
                Lift();
            else
                AddSheets(part);
        }

        // No longer Editor UI: the game's theme can have it.
        public void Unshare(VisualElement part)
        {
            if (part == null || !m_Parts.Remove(part))
                return;
            foreach (var sheet in m_Sheets)
                part.styleSheets.Remove(sheet);
        }

        // The ancestors' sheets back where they were: when the window closes or docks elsewhere.
        public void Restore()
        {
            foreach (var (owner, sheet) in m_Borrowed)
            {
                if (owner != null && !owner.styleSheets.Contains(sheet))
                    owner.styleSheets.Add(sheet);
            }
            m_Borrowed.Clear();
        }

        void Lift()
        {
            if (m_Borrowed.Count == 0)
            {
                for (var ancestor = m_Root.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
                {
                    for (int i = 0; i < ancestor.styleSheets.count; i++)
                    {
                        var sheet = ancestor.styleSheets[i];
                        m_Borrowed.Add((ancestor, sheet));
                        if (!m_Sheets.Contains(sheet))
                            m_Sheets.Add(sheet);
                    }
                    ancestor.styleSheets.Clear();
                }
            }
            foreach (var part in m_Parts)
                AddSheets(part);
        }

        void AddSheets(VisualElement part)
        {
            foreach (var sheet in m_Sheets)
            {
                if (!part.styleSheets.Contains(sheet))
                    part.styleSheets.Add(sheet);
            }
        }
    }
}
