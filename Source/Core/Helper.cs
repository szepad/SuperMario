using Gui = ImGuiNET.ImGui;

namespace Mario2
{
    public static class Helper
    {
        public static float Clamp(float val, float min, float max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        public static bool GuiDisabledButton(string label, bool disabled)
        {
            if (disabled) Gui.BeginDisabled();
            bool result = Gui.Button(label);
            if (disabled) Gui.EndDisabled();
            return result;
        }
    }
}
