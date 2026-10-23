using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Bagel
{
    // For UI that's made again rather than shown again (an editor window's copy of a screen, a
    // rebuilt list): out of the panel now, then RecursiveReleaseResources gives its layout nodes,
    // styles and text back right away instead of when the GC finds them. Never for UI that's kept
    // to be shown again: a released element can't be used. The release waits for the panel's next
    // update, because until then the panel can still send a pointer leave to what was under the
    // pointer, and a released element throws.
    public static class Discard
    {
        // Removes the parent's children for good.
        public static void Children(VisualElement parent)
        {
            if (parent == null || parent.childCount == 0)
                return;

            var children = new List<VisualElement>(parent.Children());
            var panel = parent.panel;
            parent.Clear();
            if (panel == null)
                children.ForEach(Release);
            else
                panel.visualTree.schedule.Execute(() => children.ForEach(Release));
        }

        // Removes the element from its parent for good, and everything under it.
        public static void Element(VisualElement element)
        {
            if (element == null || element.resourcesReleased)
                return;

            var panel = element.panel;
            element.RemoveFromHierarchy();
            if (panel == null)
                Release(element);
            else
                panel.visualTree.schedule.Execute(() => Release(element));
        }

        static void Release(VisualElement element)
        {
            // Released already, or put back somewhere in the meantime: leave it.
            if (element.resourcesReleased || element.hierarchy.parent != null)
                return;

            element.hierarchy.Clear(VisualElementClearOptions.RecursiveReleaseResources);
            element.ReleaseResources();
        }
    }
}
