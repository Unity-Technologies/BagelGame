using UnityEngine.UIElements;

namespace Bagel
{
    // A scroll view's scroll bars take the focus in UI Toolkit, so Tab and the arrows stop on them, unlabeled, between
    // what's in the view. The game's views scroll by themselves as the focus goes through what's in them (or, a list
    // without a selection, with the arrows), so their bars stay out of the focus: they're still there to drag.
    public static class ScrollBars
    {
        public static void KeepOutOfTheFocus(VisualElement view)
        {
            view?.Query<Scroller>().ForEach(scroller => scroller.Query<VisualElement>().ForEach(element => element.focusable = false));
        }
    }
}
