using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace Bagel
{
    // What the UI asked to hear, raised for every sound (also when muted or when nothing is listening).
    public readonly struct SoundEvent
    {
        public readonly string patch;
        public readonly string cue;
        public readonly float semitones;
        public readonly VisualElement element;

        public SoundEvent(string patch, string cue, float semitones, VisualElement element)
        {
            this.patch = patch;
            this.cue = cue;
            this.semitones = semitones;
            this.element = element;
        }

        public override string ToString() => cue == null ? patch : $"{cue}: {patch}";
    }

    // Sound effects, chosen in USS. The code only says what happened to an element; its styles say what
    // that sounds like (see BagelSounds.uss), so any sound can be retuned without code:
    //   --b-sound: pop;              pressing the element, by pointer, gamepad or keyboard (none: silent)
    //   --b-sound-<cue>: confirm;    an event, e.g. --b-sound-confirm when a long press goes all the way
    //   --b-sound-pitch: 4;          semitones up
    //   --b-sound-tone: wood;        the melodic sounds' instrument (SoundTone); each theme sets one
    // Custom properties aren't inherited, so each lookup walks up from the element: :root holds the
    // defaults. The patches are code (BagelSoundLibrary), played through BagelAudio.
    [NoAutoStaticsCleanup] // ResetStatics starts every Play session with no stale listeners
    public static class BagelSound
    {
        public const string pressProperty = "--b-sound";
        public const string pitchProperty = "--b-sound-pitch";
        public const string toneProperty = "--b-sound-tone";

        // On every screen BagelSound listens to: its styles give it the theme's tone (see BagelSounds.uss).
        public static readonly UniqueStyleString screenUssClassName = new UniqueStyleString("b-sound-screen");

        static readonly CustomStyleProperty<string> s_Press = new(pressProperty);
        static readonly CustomStyleProperty<float> s_Pitch = new(pitchProperty);
        static readonly CustomStyleProperty<string> s_Tone = new(toneProperty);
        static readonly CustomStyleProperty<string> s_ScreenTone = new("--b-sound-screen-tone");
        static readonly Dictionary<string, CustomStyleProperty<string>> s_CueProperties = new(StringComparer.Ordinal);
        static readonly EventCallback<ClickEvent> s_OnClick = OnClick;
        static readonly EventCallback<NavigationSubmitEvent> s_OnSubmit = OnSubmit;
        static readonly EventCallback<NavigationMoveEvent> s_OnNavigationMove = OnNavigationMove;
        static readonly EventCallback<PointerDownEvent> s_OnPointerDown = OnPointerDown;
        static readonly EventCallback<FocusInEvent> s_OnFocusIn = OnFocusIn;
        static readonly EventCallback<CustomStyleResolvedEvent> s_OnStylesResolved = OnStylesResolved;

        static bool s_Navigating;

        public static event Action<SoundEvent> played;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            played = null;
            s_Navigating = false;
        }

        // Plays the sound element's styles give cue (null: a press). Returns the patch, or null when the
        // styles name none.
        public static string Play(VisualElement element, string cue = null, float semitones = 0f, float pan = 0f)
        {
            if (element == null)
                return null;
            var patch = FindString(element, cue == null ? s_Press : CueProperty(cue));
            if (patch == null || patch == BagelSoundLibrary.none)
                return null;

            if (BagelSoundLibrary.TryParseTone(FindString(element, s_Tone), out var tone))
                BagelAudio.tone = tone;
            if (FindFloat(element, s_Pitch, out var pitch))
                semitones += pitch;

            played?.Invoke(new SoundEvent(patch, cue, semitones, element));
            BagelAudio.Play(patch, semitones, pan);
            return patch;
        }

        // Plays the press sound of whatever is pressed inside root (disabled elements: the disabled cue),
        // and the focus cue when a gamepad or keyboard moves the focus. The root's theme picks the tone
        // for every sound, the game's too.
        public static void Listen(VisualElement root)
        {
            root.UnregisterCallback(s_OnClick, TrickleDown.TrickleDown);
            root.RegisterCallback(s_OnClick, TrickleDown.TrickleDown);
            root.UnregisterCallback(s_OnSubmit, TrickleDown.TrickleDown);
            root.RegisterCallback(s_OnSubmit, TrickleDown.TrickleDown);
            root.UnregisterCallback(s_OnNavigationMove, TrickleDown.TrickleDown);
            root.RegisterCallback(s_OnNavigationMove, TrickleDown.TrickleDown);
            root.UnregisterCallback(s_OnPointerDown, TrickleDown.TrickleDown);
            root.RegisterCallback(s_OnPointerDown, TrickleDown.TrickleDown);
            root.UnregisterCallback(s_OnFocusIn, TrickleDown.TrickleDown);
            root.RegisterCallback(s_OnFocusIn, TrickleDown.TrickleDown);
            root.UnregisterCallback(s_OnStylesResolved);
            root.RegisterCallback(s_OnStylesResolved);
            root.AddToClassList(screenUssClassName);
        }

        static void OnClick(ClickEvent evt) => Press(evt.target as VisualElement);

        static void OnSubmit(NavigationSubmitEvent evt) => Press(evt.target as VisualElement);

        static void Press(VisualElement element)
        {
            if (element != null)
                Play(element, element.enabledInHierarchy ? null : "disabled");
        }

        static void OnNavigationMove(NavigationMoveEvent evt) => s_Navigating = true;

        static void OnPointerDown(PointerDownEvent evt) => s_Navigating = false;

        static void OnFocusIn(FocusInEvent evt)
        {
            if (!s_Navigating)
                return;
            s_Navigating = false;
            Play(evt.target as VisualElement, "focus");
        }

        // A theme sets the tone on :root, which only the root's own styles see; a screen copies it into
        // its own, so it hears about every theme change.
        static void OnStylesResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_ScreenTone, out var value) && BagelSoundLibrary.TryParseTone(value, out var tone))
                BagelAudio.tone = tone;
        }

        static CustomStyleProperty<string> CueProperty(string cue)
        {
            if (!s_CueProperties.TryGetValue(cue, out var property))
            {
                property = new CustomStyleProperty<string>(pressProperty + "-" + cue);
                s_CueProperties.Add(cue, property);
            }
            return property;
        }

        static string FindString(VisualElement element, CustomStyleProperty<string> property)
        {
            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                if (e.customStyle.TryGetValue(property, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value.Trim().Trim('"', '\'').ToLowerInvariant();
            }
            return null;
        }

        static bool FindFloat(VisualElement element, CustomStyleProperty<float> property, out float value)
        {
            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                if (e.customStyle.TryGetValue(property, out value))
                    return true;
            }
            value = 0f;
            return false;
        }
    }
}
