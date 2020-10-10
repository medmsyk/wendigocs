using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Device.Event.WinAPI;
using static Device.Event.Structure;

namespace Device.Event {
    public static class EventDispatcher {
        // Device event for Windows API
        private static class Event {
            public const uint KeyboardKeyDown       = 0x100;
            public const uint KeyboardKeyUp         = 0x101;
            public const uint KeyboardSystemKeyDown = 0x104;
            public const uint KeyboardSystemKeyUp   = 0x105;

            public const uint MouseMove        = 0x0200;
            public const uint MouseLButtonDown = 0x0201;
            public const uint MouseLButtonUp   = 0x0202;
            public const uint MouseRButtonDown = 0x0204;
            public const uint MouseRButtonUp   = 0x0205;
            public const uint MouseMButtonDown = 0x0207;
            public const uint MouseMButtonUp   = 0x0208;
            public const uint MouseWheel       = 0x020A;
            public const uint MouseXButtonDown = 0x020B;
            public const uint MouseXButtonUp   = 0x020C;
            public const uint MouseTilt        = 0x020E;
        }
        
        // Handles for Windows API
        private static class Handles {
            public const int KeyboardLowLevel = 13;
            public const int MouseLowLevel    = 14;
        }
        
        private static IntPtr keyEventHandle;
        private static KeyHook.KeyboardEventHandler keyEventHandler;

        private static IntPtr mouseEventHandle;
        private static MouseHook.MouseEventHandler mouseEventHandler;
        
        private static Dictionary<string, EventListener> listeners = new Dictionary<string, EventListener>();
        private static object listenerLocker = new object();
        
        private static DeviceState oldState = new DeviceState();
        private static object stateLocker = new object();

        /// <summary>
        /// Initialize.
        /// </summary>
        static EventDispatcher() {
            IntPtr h = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
            
            keyEventHandler = HandleKeyEvent;    // Prevent from being released by garbage collector
            keyEventHandle = KeyHook.SetWindowsHookEx(Handles.KeyboardLowLevel, keyEventHandler, h, IntPtr.Zero);
            if (keyEventHandle == IntPtr.Zero) throw new Win32Exception("Failed to set key event handler");
            
            mouseEventHandler = HandleMouseEvent;    // Prevent from being released by garbage collector
            mouseEventHandle = MouseHook.SetWindowsHookEx(Handles.MouseLowLevel, mouseEventHandler, h, IntPtr.Zero);
            if (mouseEventHandle == IntPtr.Zero) throw new Win32Exception("Failed to set mouse event handler");
        }

        /// <summary>
        /// Check if the dispatcher is listening for an event or not.
        /// </summary>
        /// <param name="name">Name.</param>
        private static void NotListening(string name) {
            if (listeners.Keys.ToList().Contains(name)) throw new InvalidOperationException($"Already listening to {name}");
        }

        /// <summary>
        /// Get an event name.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="forSystem">The event is for system or not.</param>
        /// <returns>Name.</returns>
        private static string GetName(string name, bool forSystem) {
            return $"{(forSystem ? "system" : "user")}:{name}";
        }

        /// <summary>
        /// Listen for key event.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="keys">Keys.</param>
        /// <param name="deviceEvent">Device event.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="forSystem">The event is for system or not.</param>
        private static void Listen(string name, Keys[] keys, DeviceEvent deviceEvent, DeviceEventHandler eventHandler, bool forSystem) {
            if (keys.Length == 0) throw new ArgumentException("Keys are not specified");

            name = GetName(name, forSystem);
            lock(listenerLocker){
                NotListening(name);
                listeners.Add(name, new EventListener(keys, deviceEvent, eventHandler));
            }
        }

        /// <summary>
        /// Listen for key down.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="keys">Keys.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="forSystem">The event is for system or not.</param>
        public static void KeyDown(string name, Keys[] keys, DeviceEventHandler eventHandler, bool forSystem=false) {
            Listen(name, keys, DeviceEvent.KeyDown, eventHandler, forSystem);
        }
        
        /// <summary>
        /// Listen for key up.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="keys">Keys.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="forSystem">The event is for system or not.</param>
        public static void KeyUp(string name, Keys[] keys, DeviceEventHandler eventHandler, bool forSystem=false) {
            Listen(name, keys, DeviceEvent.KeyUp, eventHandler, forSystem);
        }
        
        /// <summary>
        /// Listen for key press.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="keys">Keys.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="forSystem">The event is for system or not.</param>
        public static void KeyPress(string name, Keys[] keys, DeviceEventHandler eventHandler, bool forSystem=false) {
            Listen(name, keys, DeviceEvent.KeyPress, eventHandler, forSystem);
        }
        
        /// <summary>
        /// Listen for mouse event.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="deviceEvent">Device event.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="forSystem">The event is for system or not.</param>
        private static void Listen(string name, DeviceEvent deviceEvent, DeviceEventHandler eventHandler, bool forSystem) {
            name = GetName(name, forSystem);
            lock(listenerLocker){
                NotListening(name);
                listeners.Add(name, new EventListener(deviceEvent, eventHandler));
            }
        }
        
        /// <summary>
        /// Listen for mouse move.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="forSystem">The event is for system or not.</param>
        public static void MouseMove(string name, DeviceEventHandler eventHandler, bool forSystem=false) {
            Listen(name, DeviceEvent.MouseMove, eventHandler, forSystem);
        }
        
        /// <summary>
        /// Listen for mouse wheel.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="forSystem">The event is for system or not.</param>
        public static void MouseWheel(string name, DeviceEventHandler eventHandler, bool forSystem=false) {
            Listen(name, DeviceEvent.MouseWheel, eventHandler, forSystem);
        }
        
        /// <summary>
        /// Listen for mouse tilt.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="forSystem">The event is for system or not.</param>
        public static void MouseTilt(string name, DeviceEventHandler eventHandler, bool forSystem=false) {
            Listen(name, DeviceEvent.MouseTilt, eventHandler, forSystem);
        }

        /// <summary>
        /// Unlisten for key or mouse event.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="forSystem">The event is for system or not.</param>
        public static void Unlisten(string name, bool forSystem=false) {
            name = GetName(name, forSystem);
            lock(listenerLocker){
                foreach (string n in new List<string>(listeners.Keys)) {
                    if (n == name || n.StartsWith($"{name}.")) listeners.Remove(n);
                }
            }
        }
        
        /// <summary>
        /// Handle device event.
        /// </summary>
        /// <param name="state">Device state.</param>
        /// <param name="oldState">Old device state.</param>
        private static void HandleDeviceEvent(DeviceState state, DeviceState oldState) {
            EventImitator.RecordToFile(state);

            Parallel.ForEach(new List<string>(listeners.Keys), (name) => {
                listeners[name].Dispatch(state, oldState);
            });
        }
        
        /// <summary>
        /// Handle key event.
        /// </summary>
        /// <param name="code">Code.</param>
        /// <param name="msg">Message.</param>
        /// <param name="keyboard">Keyboard.</param>
        /// <returns>Value which depends on the hook type.</returns>
        private static IntPtr HandleKeyEvent(int code, uint msg, Keyboard keyboard) {
            if (code >= 0) {
                lock (stateLocker) {
                    DeviceState state = GetDeviceState(msg, keyboard);
                    HandleDeviceEvent(state, oldState);
                    oldState = state;
                }
            }

            return KeyHook.CallNextHookEx(keyEventHandle, code, msg, keyboard);
        }

        /// <summary>
        /// Get key state.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <param name="k">Keyboard.</param>
        /// <returns>Key state.</returns>
        private static DeviceState GetDeviceState(uint msg, Keyboard k) {
            DeviceState state = new DeviceState();
            Keys key = (Keys)k.VirtualKey;

            switch (msg) {
                case Event.KeyboardKeyDown:       state.SetKeyTarget(DeviceEvent.KeyDown, key); break;
                case Event.KeyboardKeyUp:         state.SetKeyTarget(DeviceEvent.KeyUp, key);   break;
                case Event.KeyboardSystemKeyDown: state.SetKeyTarget(DeviceEvent.KeyDown, key); break;
                case Event.KeyboardSystemKeyUp:   state.SetKeyTarget(DeviceEvent.KeyUp, key);   break;
            }
            
            return state;
        }
        
        /// <summary>
        /// Handle mouse event.
        /// </summary>
        /// <param name="code">Code.</param>
        /// <param name="msg">Message.</param>
        /// <param name="mouse">Mouse.</param>
        /// <returns>Value which depends on the hook type.</returns>
        private static IntPtr HandleMouseEvent(int code, uint msg, Mouse mouse) {
            if (code >= 0) {
                lock (stateLocker) {
                    DeviceState state = GetDeviceState(msg, mouse);
                    HandleDeviceEvent(state, oldState);
                    oldState = state;
                }
            }

            return MouseHook.CallNextHookEx(mouseEventHandle, code, msg, mouse);
        }

        /// <summary>
        /// Get mouse state.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <param name="m">Mouse.</param>
        /// <returns>Mouse state.</returns>
        private static DeviceState GetDeviceState(uint msg, Mouse m) {
            DeviceState state = new DeviceState();
            Keys key;

            switch (msg) {
                case Event.MouseMove:        state.SetMouseTarget(DeviceEvent.MouseMove,  Cursor.Position); break;
                case Event.MouseWheel:       state.SetMouseTarget(DeviceEvent.MouseWheel, new Point(0, GetMousePoint(m.Data))); break;
                case Event.MouseTilt:        state.SetMouseTarget(DeviceEvent.MouseTilt,  new Point(GetMousePoint(m.Data), 0)); break;
                case Event.MouseLButtonDown: state.SetKeyTarget(DeviceEvent.KeyDown, Keys.LButton); break;
                case Event.MouseLButtonUp:   state.SetKeyTarget(DeviceEvent.KeyUp,   Keys.LButton); break;
                case Event.MouseRButtonDown: state.SetKeyTarget(DeviceEvent.KeyDown, Keys.RButton); break;
                case Event.MouseRButtonUp:   state.SetKeyTarget(DeviceEvent.KeyUp,   Keys.RButton); break;
                case Event.MouseMButtonDown: state.SetKeyTarget(DeviceEvent.KeyDown, Keys.MButton); break;
                case Event.MouseMButtonUp:   state.SetKeyTarget(DeviceEvent.KeyUp,   Keys.MButton); break;
                case Event.MouseXButtonDown: key = GetMouseXButton(m.Data); if (key != Keys.None) state.SetKeyTarget(DeviceEvent.KeyDown, key); break;
                case Event.MouseXButtonUp:   key = GetMouseXButton(m.Data); if (key != Keys.None) state.SetKeyTarget(DeviceEvent.KeyUp, key);   break;
            }
            
            return state;
        }

        /// <summary>
        /// Get mouse point for wheel or tilt.
        /// </summary>
        /// <param name="mouseData">Mouse data.</param>
        /// <returns>Mouse point.</returns>
        private static int GetMousePoint(uint mouseData) {
            return (int)mouseData >> 16;
        }

        /// <summary>
        /// Get mouse X button.
        /// </summary>
        /// <param name="mouseData">Mouse data.</param>
        /// <returns>X button 1 or X button 2.</returns>
        private static Keys GetMouseXButton(uint mouseData) {
            switch (mouseData >> 16) {
                case 1:  return Keys.XButton1;
                case 2:  return Keys.XButton2;
                default: return Keys.None;
            }
        }
    }
}
