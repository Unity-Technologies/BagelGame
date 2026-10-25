using System;
using UnityEngine.UIElements;

namespace Bagel
{
    // For a ToggleButtonGroup that picks one of its buttons: its value for an index, and the index back.
    public static class Choice
    {
        public static ToggleButtonGroupState One(int index, int count) =>
            new(index >= 0 && index < count ? 1UL << index : 0UL, count);

        // The picked button's index, or -1.
        public static int Selected(ToggleButtonGroupState state)
        {
            Span<int> active = stackalloc int[Math.Max(1, state.length)];
            var options = state.GetActiveOptions(active);
            return options.Length > 0 ? options[0] : -1;
        }
    }
}
