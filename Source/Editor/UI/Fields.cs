using Gum.Forms.Controls;
using Gum.Wireframe;
using NativeFileDialogSharp;

namespace Mario2.Editor
{
    /// <summary>
    /// An inspector control bound to a getter/setter pair. Values are pulled every frame (unless
    /// the user is editing the control) so undo/redo and viewport edits show up immediately.
    /// </summary>
    internal abstract class Field
    {
        public abstract FrameworkElement Control { get; }
        public Func<bool>? IsEnabled { get; init; }

        public virtual void Update() { }

        public void Refresh()
        {
            bool enabled = IsEnabled?.Invoke() ?? true;
            if (Control.IsEnabled != enabled)
                Control.IsEnabled = enabled;

            RefreshValue();
        }

        protected abstract void RefreshValue();
    }

    /// <summary>Commits on Enter or when focus leaves; Escape reverts. Invalid input snaps back.</summary>
    internal sealed class TextField : Field
    {
        private readonly TextBox _box = new();
        private readonly Func<string> _get;
        private readonly Func<string, bool> _set;

        public override FrameworkElement Control => _box;
        public TextBox Box => _box;

        public TextField(Func<string> get, Func<string, bool> set)
        {
            _get = get;
            _set = set;
            _box.Text = get();
            _box.LostFocus += (_, _) => Commit();
        }

        public override void Update()
        {
            if (!_box.IsFocused)
                return;

            if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
            {
                Commit();
                _box.IsFocused = false;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                _box.Text = _get();
                _box.IsFocused = false;
            }
        }

        private void Commit()
        {
            if (_box.Text == _get())
                return;

            if (!_set(_box.Text ?? string.Empty))
                _box.Text = _get();
        }

        protected override void RefreshValue()
        {
            if (_box.IsFocused)
                return;

            string value = _get();
            if (_box.Text != value)
                _box.Text = value;
        }
    }

    internal sealed class CheckField : Field
    {
        private readonly CheckBox _box = new() { Text = string.Empty };
        private readonly Func<bool> _get;
        private bool _updating;

        public override FrameworkElement Control => _box;

        public CheckField(Func<bool> get, Action<bool> set)
        {
            _get = get;
            _box.IsChecked = get();
            _box.Checked += (_, _) => Changed(set, true);
            _box.Unchecked += (_, _) => Changed(set, false);
        }

        private void Changed(Action<bool> set, bool value)
        {
            if (_updating)
                return;

            InteractiveGue.CurrentInputReceiver = null;
            if (value != _get())
                set(value);
        }

        protected override void RefreshValue()
        {
            bool value = _get();
            if (_box.IsChecked == value)
                return;

            _updating = true;
            _box.IsChecked = value;
            _updating = false;
        }
    }

    internal sealed class ChoiceField : Field
    {
        private readonly ComboBox _combo = new();
        private readonly IReadOnlyList<string> _choices;
        private readonly Func<string> _get;
        private bool _updating;

        public override FrameworkElement Control => _combo;

        public ChoiceField(IReadOnlyList<string> choices, Func<string> get, Action<string> set)
        {
            _choices = choices;
            _get = get;

            foreach (string choice in choices)
                _combo.Items!.Add(choice);

            RefreshValue();
            _combo.SelectionChanged += (_, _) =>
            {
                if (_updating || _combo.SelectedIndex < 0)
                    return;

                InteractiveGue.CurrentInputReceiver = null;
                string value = _choices[_combo.SelectedIndex];
                if (value != _get())
                    set(value);
            };
        }

        protected override void RefreshValue()
        {
            if (_combo.IsDropDownOpen)
                return;

            string value = _get();
            int index = -1;
            for (int i = 0; i < _choices.Count; i++)
            {
                if (string.Equals(_choices[i], value, StringComparison.OrdinalIgnoreCase))
                    index = i;
            }

            if (_combo.SelectedIndex == index)
                return;

            _updating = true;
            _combo.SelectedIndex = index;
            _updating = false;
        }
    }

    /// <summary>A text field with a browse button that stores paths relative to the game folder.</summary>
    internal sealed class PathField : Field
    {
        private readonly StackPanel _row = UiKit.Row(4f);
        private readonly TextField _text;

        public override FrameworkElement Control => _row;

        public PathField(Func<string> get, Func<string, bool> set)
        {
            _text = new TextField(get, set);
            UiKit.Grow(_text.Box.Visual, horizontal: true);
            _row.AddChild(_text.Box);
            _row.AddChild(UiKit.Button("...", () =>
            {
                DialogResult result = Dialog.FileOpen("lvl", AppPaths.DefaultLevelsFolder);
                if (result.IsOk)
                    set(ToGamePath(result.Path));
            }, width: 32));
        }

        public override void Update() => _text.Update();
        protected override void RefreshValue() => _text.Refresh();

        private static string ToGamePath(string path)
        {
            // Levels are loaded relative to the game folder, so store them that way when possible
            string relative = Path.GetRelativePath(AppContext.BaseDirectory, path);
            return relative.StartsWith("..") || Path.IsPathRooted(relative) ? path : relative.Replace('\\', '/');
        }
    }

    internal sealed class InfoField : Field
    {
        private readonly Label _label = new();
        private readonly Func<string> _get;

        public override FrameworkElement Control => _label;

        public InfoField(Func<string> get)
        {
            _get = get;
            _label.Text = get();
        }

        protected override void RefreshValue() => UiKit.SetText(_label, _get());
    }
}
