using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;

namespace Mario2.Editor
{
    /// <summary>
    /// Simple modal message boxes. While one is open the rest of the editor ignores input.
    /// Enter presses the first button, Escape the last one.
    /// </summary>
    public sealed class Dialogs
    {
        private const float Width = 460f;
        private const float Padding = 16f;

        public bool IsOpen => _overlay is not null;

        private ContainerRuntime? _overlay;
        private (string Label, Action? Action)[] _buttons = [];
        private bool _openedThisFrame;

        public void Show(string title, string message, params (string Label, Action? Action)[] buttons)
        {
            Close();
            InteractiveGue.CurrentInputReceiver = null;
            _buttons = buttons;
            _openedThisFrame = true;

            // A full-window dimmer that swallows clicks
            _overlay = new ContainerRuntime { HasEvents = true };
            _overlay.Dock(Dock.Fill);
            UiKit.Background(_overlay, Theme.Dimmer);

            ContainerRuntime panel = UiKit.Container();
            panel.Width = Width;
            panel.HeightUnits = DimensionUnitType.RelativeToChildren;
            panel.Height = Padding * 2;
            panel.Anchor(Anchor.Center);
            UiKit.Background(panel, Theme.Panel);
            RectangleRuntime stripe = UiKit.Filled(Theme.Selection);
            stripe.Anchor(Anchor.Top);
            UiKit.FillWidth(stripe);
            stripe.Height = 3;
            panel.AddChild(stripe);
            _overlay.AddChild(panel);

            ContainerRuntime content = UiKit.Container(ChildrenLayout.TopToBottomStack);
            content.X = Padding;
            content.Y = Padding;
            content.StackSpacing = 12;
            UiKit.FillWidth(content, Padding * 2);
            content.HeightUnits = DimensionUnitType.RelativeToChildren;
            content.Height = 0;
            panel.AddChild(content);

            content.AddChild(UiKit.Label(title, Theme.Text, 18));

            Label body = UiKit.Label(message, Theme.Text);
            UiKit.FillWidth(body);
            content.AddChild(body);

            StackPanel row = UiKit.Row(8f);
            for (int i = 0; i < buttons.Length; i++)
            {
                Action? action = buttons[i].Action;
                Button button = UiKit.Button(buttons[i].Label, () => Choose(action));

                // Enter presses the first button, so show it as the default
                if (i == 0)
                    UiKit.MakePrimary(button);

                row.AddChild(button);
            }

            content.AddChild(row);

            GumService.Default.ModalRoot.AddChild(_overlay);
        }

        public void Update()
        {
            if (_overlay is null)
                return;

            // The key that opened the dialog shouldn't also answer it
            if (_openedThisFrame)
            {
                _openedThisFrame = false;
                return;
            }

            if (_buttons.Length == 0)
                return;

            if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
                Choose(_buttons[0].Action);
            else if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                Choose(_buttons[^1].Action);
        }

        public void Close()
        {
            if (_overlay is null)
                return;

            GumService.Default.ModalRoot.RemoveChild(_overlay);
            _overlay = null;
            _buttons = [];
        }

        private void Choose(Action? action)
        {
            Close();
            action?.Invoke();
        }
    }
}
