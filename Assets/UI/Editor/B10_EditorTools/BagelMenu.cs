namespace Bagel
{
    // The Editor modes the Bagel menu shows in (MenuItem.editorModes): the default one, and the one the
    // Editor switches to while a UXML document is open on its own in the UI Viewport. That mode's menu bar
    // keeps a fixed list of menus (File, Edit, Assets, Window, Help...) and drops every other root menu,
    // unless its items name the mode.
    public static class BagelMenu
    {
        public const string defaultMode = "default";
        public const string uiToolkitAuthoringMode = "uitoolkitauthoring";
    }
}
