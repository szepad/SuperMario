using System.Runtime.InteropServices;

namespace Mario2.Editor
{
    /// <summary>
    /// Reads trackpad scrolling and pinch gestures straight from macOS. raylib's GLFW passes
    /// scrolling on without saying whether it came from a trackpad, and drops pinch gestures
    /// entirely, so this patches GLFW's content view through the Objective-C runtime.
    /// </summary>
    /// <remarks>
    /// The callbacks run inside raylib's event polling (in <c>EndDrawing</c>) on the main thread,
    /// so the totals are simply collected until the next <see cref="Drain"/>.
    /// </remarks>
    internal static unsafe class MacTrackpad
    {
        private const string ObjC = "/usr/lib/libobjc.A.dylib";
        private const string ViewEventTypes = "v@:@";

        public static bool IsInstalled { get; private set; }

        private static bool _attempted;
        private static delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> _originalScrollWheel;

        private static IntPtr _hasPreciseDeltas;
        private static IntPtr _deltaX;
        private static IntPtr _deltaY;
        private static IntPtr _magnification;

        private static Vector2 _points;
        private static Vector2 _lines;
        private static float _magnify = 1f;
        private static bool _smartMagnify;

        /// <summary>Installs the hooks once. Returns false off macOS or if GLFW looks different than expected.</summary>
        public static bool Install()
        {
            if (_attempted)
                return IsInstalled;

            _attempted = true;
            if (!OperatingSystem.IsMacOS())
                return false;

            try
            {
                IntPtr view = objc_getClass("GLFWContentView");
                IntPtr scrollWheel = view == IntPtr.Zero ? IntPtr.Zero : class_getInstanceMethod(view, sel_registerName("scrollWheel:"));
                if (scrollWheel == IntPtr.Zero)
                    return false;

                _hasPreciseDeltas = sel_registerName("hasPreciseScrollingDeltas");
                _deltaX = sel_registerName("scrollingDeltaX");
                _deltaY = sel_registerName("scrollingDeltaY");
                _magnification = sel_registerName("magnification");

                // GLFW doesn't implement these, so they're added rather than replaced
                class_addMethod(view, sel_registerName("magnifyWithEvent:"),
                    (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)&OnMagnify, ViewEventTypes);
                class_addMethod(view, sel_registerName("smartMagnifyWithEvent:"),
                    (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)&OnSmartMagnify, ViewEventTypes);

                _originalScrollWheel = (delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)method_setImplementation(scrollWheel,
                    (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)&OnScrollWheel);

                IsInstalled = true;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                IsInstalled = false;
            }

            return IsInstalled;
        }

        /// <summary>Everything that arrived since the last call.</summary>
        public static ScrollInput Drain()
        {
            ScrollInput input = new(_points, _lines, _magnify, _smartMagnify);
            Discard();
            return input;
        }

        /// <summary>Forgets gestures made while the editor wasn't listening (e.g. during a playtest).</summary>
        public static void Discard()
        {
            _points = Vector2.Zero;
            _lines = Vector2.Zero;
            _magnify = 1f;
            _smartMagnify = false;
        }

        [UnmanagedCallersOnly]
        private static void OnScrollWheel(IntPtr self, IntPtr selector, IntPtr nsEvent)
        {
            // Exceptions must not escape into native code
            try
            {
                // Both are already in the direction the content should move (natural scrolling applied)
                Vector2 delta = new((float)SendDouble(nsEvent, _deltaX), (float)SendDouble(nsEvent, _deltaY));
                if (SendByte(nsEvent, _hasPreciseDeltas) != 0)
                    _points += delta;
                else
                    _lines += delta;
            }
            catch
            {
            }

            // raylib (and through it Gum) still gets every scroll event
            _originalScrollWheel(self, selector, nsEvent);
        }

        [UnmanagedCallersOnly]
        private static void OnMagnify(IntPtr self, IntPtr selector, IntPtr nsEvent)
        {
            try
            {
                _magnify *= 1f + (float)SendDouble(nsEvent, _magnification);
            }
            catch
            {
            }
        }

        [UnmanagedCallersOnly]
        private static void OnSmartMagnify(IntPtr self, IntPtr selector, IntPtr nsEvent)
        {
            _smartMagnify = true;
        }

        [DllImport(ObjC)]
        private static extern IntPtr objc_getClass(string name);

        [DllImport(ObjC)]
        private static extern IntPtr sel_registerName(string name);

        [DllImport(ObjC)]
        private static extern IntPtr class_getInstanceMethod(IntPtr cls, IntPtr selector);

        [DllImport(ObjC)]
        private static extern IntPtr method_setImplementation(IntPtr method, IntPtr implementation);

        [DllImport(ObjC)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool class_addMethod(IntPtr cls, IntPtr selector, IntPtr implementation, string types);

        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        private static extern double SendDouble(IntPtr receiver, IntPtr selector);

        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        private static extern byte SendByte(IntPtr receiver, IntPtr selector);
    }
}
