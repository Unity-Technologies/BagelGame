using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace Bagel
{
    // How the blackboard changes what it shows: what's there is wiped off with an eraser, then the new chalk is
    // written in, a line at a time. The ChalkWipe custom filter draws both; USS transitions on the b-wipe classes
    // animate it (see the board's styles), and this only sets the classes, in order. It's quick, so it never holds
    // anyone up, and instant where motion is reduced or the filters are off.
    [NoAutoStaticsCleanup] // pending steps belong to their elements, which the table doesn't keep alive
    public static class ChalkWipe
    {
        // Wiped by the eraser: shown on top of whatever comes next while the stroke crosses it.
        public static readonly UniqueStyleString erasingUssClassName = new("b-wipe--erasing");

        // Nothing written yet: what's written in starts from here.
        public static readonly UniqueStyleString blankUssClassName = new("b-wipe--blank");

        // How long the eraser takes (the b-wipe--erasing transition), before the writing starts.
        public const long eraseMs = 150;

        static readonly ConditionalWeakTable<VisualElement, IVisualElementScheduledItem> s_Pending = new();

        // Wipes element off the board. It's blank afterwards.
        public static void Erase(VisualElement element, bool instant = false)
        {
            if (element == null)
                return;
            Cancel(element);
            if (instant)
            {
                element.RemoveFromClassList(erasingUssClassName);
                element.AddToClassList(blankUssClassName);
                return;
            }
            element.RemoveFromClassList(blankUssClassName);
            element.AddToClassList(erasingUssClassName);
            Later(element, eraseMs + 30, () =>
            {
                element.AddToClassList(blankUssClassName);
                element.RemoveFromClassList(erasingUssClassName);
            });
        }

        // Writes element in, once the eraser is done with what was there before.
        public static void Write(VisualElement element, bool instant = false)
        {
            if (element == null)
                return;
            Cancel(element);
            element.RemoveFromClassList(erasingUssClassName);
            if (instant)
            {
                element.RemoveFromClassList(blankUssClassName);
                return;
            }
            element.AddToClassList(blankUssClassName);
            Later(element, eraseMs, () => element.RemoveFromClassList(blankUssClassName));
        }

        // Shown or blank right away, as the board starts.
        public static void Set(VisualElement element, bool written)
        {
            if (element == null)
                return;
            Cancel(element);
            element.RemoveFromClassList(erasingUssClassName);
            element.EnableInClassList(blankUssClassName, !written);
        }

        static void Later(VisualElement element, long delayMs, System.Action action)
        {
            var item = element.schedule.Execute(() =>
            {
                s_Pending.Remove(element);
                action();
            });
            item.ExecuteLater(delayMs);
            s_Pending.AddOrUpdate(element, item);
        }

        static void Cancel(VisualElement element)
        {
            if (s_Pending.TryGetValue(element, out var item))
            {
                item.Pause();
                s_Pending.Remove(element);
            }
        }
    }
}
