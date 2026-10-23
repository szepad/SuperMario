using Gum.Forms.DefaultVisuals.V3;
using Gum.Themes.Editor;

namespace Mario2.Editor
{
    /// <summary>
    /// Every colour the editor uses ("Mario night": blue-slate panels with Mario red, Luigi green
    /// and coin gold accents). Nothing else in the editor should create colours.
    /// </summary>
    /// <remarks>
    /// To add another theme, turn these into instance values and pick one before <see cref="ConfigureGum"/>
    /// runs: Gum's visuals read their colours when they're created.
    /// </remarks>
    internal static class Theme
    {
        // Surfaces, darkest to lightest
        public static readonly Color Outside = Hex(0x0B0E1C);
        public static readonly Color Window = Hex(0x0F1324);
        public static readonly Color Recessed = Hex(0x10142A);
        public static readonly Color Panel = Hex(0x1A2038);
        public static readonly Color PanelRaised = Hex(0x252D4D);
        public static readonly Color Control = Hex(0x2B3560);
        public static readonly Color RowEven = Hex(0x1E2541);   // inspector rows, alternating
        public static readonly Color RowOdd = Hex(0x232B4A);
        public static readonly Color StatusBar = Hex(0x233F8C);
        public static readonly Color StatusBarMuted = Hex(0xB4C2EE);   // secondary text on the status bar

        public static readonly Color Border = Hex(0x2E3864);
        public static readonly Color BorderHover = Hex(0x7C8CD0);

        public static readonly Color Text = Hex(0xE8ECF8);
        public static readonly Color TextMuted = Hex(0x8890B5);

        // Meaning
        public static readonly Color Selection = Hex(0x2E6BD6);   // Mario blue: what's active or selected
        public static readonly Color Accent = Hex(0xF7B92E);      // coin gold: focus, level
        public static readonly Color Success = Hex(0x3DAE4A);     // Luigi green: play, brush
        public static readonly Color Danger = Hex(0xE04040);      // Mario red: delete, erase
        public static readonly Color DangerText = Hex(0xFF8A80);  // readable red on dark buttons
        public static readonly Color Info = Hex(0x4DA3FF);        // sky blue: selection outlines

        // Level view overlays
        public static readonly Color Hover = new(255, 255, 255, 140);
        public static readonly Color HoverFill = new(255, 255, 255, 30);
        public static readonly Color Grid = new(255, 255, 255, 38);
        public static readonly Color LevelBorder = new(255, 255, 255, 90);
        public static readonly Color ScreenGuide = Accent.Alpha(0.45f);
        public static readonly Color Spawn = Hex(0x78E678);
        public static readonly Color Collision = Danger.Alpha(0.3f);
        public static readonly Color EraseFill = Danger.Alpha(0.27f);
        public static readonly Color Dimmer = new(5, 7, 15, 170);
        public static readonly Color Transparent = new(0, 0, 0, 0);

        /// <summary>
        /// Gives Gum the theme. The editor theme's visuals read <see cref="EditorStyling"/> when they're
        /// created, so it's filled before <see cref="EditorTheme.Apply"/>. Apply copies only a few of
        /// those into Gum's base styling (used by toggle buttons, menus, tooltips), so the rest is set after.
        /// </summary>
        public static void ConfigureGum(Action applyEditorTheme)
        {
            EditorColors editor = EditorStyling.ActiveStyle.Colors;
            editor.TextPrimary = Text;
            editor.TextMuted = TextMuted;
            editor.Primary = Control;
            editor.Accent = Accent;
            editor.BorderHover = BorderHover;
            editor.BorderPushed = Text;
            editor.Selection = Selection;
            editor.PanelBackground = Panel;
            editor.RecessedBackground = Recessed;

            applyEditorTheme();

            Colors basic = Styling.ActiveStyle.Colors;
            basic.Black = Outside;
            basic.DarkGray = Panel;
            basic.Gray = BorderHover;
            basic.LightGray = TextMuted;
            basic.White = Text;
            basic.Primary = Control;
            basic.Success = Success;
            basic.Warning = Accent;
            basic.Danger = Danger;
            basic.Accent = Selection;
            basic.InputBackground = Recessed;
            basic.SurfaceVariant = PanelRaised;
            basic.IconDefault = Text;
            basic.TextPrimary = Text;
            basic.TextMuted = TextMuted;
        }

        private static Color Hex(int rgb) => new((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF, 255);
    }
}
