using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Device.Event {
    /// <summary>
    /// Device.
    /// </summary>
    internal enum Device {
        Nothing  = -1,
        Mouse    =  0,
        Keyboard =  1,
        Hardware =  2,
    }

    /// <summary>
    /// Device event.
    /// </summary>
    public enum DeviceEvent {
        Nothing    = -1,
        KeyDown    =  0,
        KeyPress   =  1,
        KeyUp      =  2,
        MouseMove  =  3,
        MouseWheel =  4,
        MouseTilt  =  5,
    }

    /// <summary>
    /// Device event args which contains a device state.
    /// </summary>
    public class DeviceEventArgs : EventArgs {
        public DeviceState State { get; set; }
        public DeviceEventArgs(DeviceState state) { State = state; }
    }

    /// <summary>
    /// Device event handler.
    /// </summary>
    /// <param name="e">Device event args.</param>
    public delegate void DeviceEventHandler(DeviceEventArgs e);
    
    internal static class WinAPI {
        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);
        
        [DllImport("user32.dll")]
        public static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        public static extern int SendInput(uint length, ref Structure.Input input, int size);
        
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys key);

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW")]
        public static extern IntPtr GetModuleHandle(string moduelName);
        
        public static class KeyHook {
            public delegate IntPtr KeyboardEventHandler(int nCode, uint msg, Structure.Keyboard kbdllhookstruct);
            
            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowsHookEx(int id, KeyboardEventHandler eventHandler, IntPtr moduleName, IntPtr threadId);
            
            [DllImport("user32.dll")]
            public static extern IntPtr CallNextHookEx(IntPtr handle, int code, uint msg, Structure.Keyboard keyboard);
        }

        public static class MouseHook {
            public delegate IntPtr MouseEventHandler(int code, uint msg, Structure.Mouse mouse);

            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowsHookEx(int id, MouseEventHandler eventHandler, IntPtr moduleName, IntPtr threadId);
            
            [DllImport("user32.dll")]
            public static extern IntPtr CallNextHookEx(IntPtr handle, int code, uint msg, Structure.Mouse mouse);
        }
    }
    
    public static class Structure {
        private static IntPtr extraInfo = WinAPI.GetMessageExtraInfo();
        private static IntPtr keyboardLayout = WinAPI.GetKeyboardLayout(0);
        
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct Keyboard {
            public ushort VirtualKey;
            public ushort ScanCode;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;

            /// <summary>
            /// Define a keyboard input.
            /// </summary>
            /// <param name="virtualKey">Virtual key.</param>
            /// <param name="scanCode">Scan code.</param>
            /// <param name="flags">Flags.</param>
            public Keyboard(ushort virtualKey, ushort scanCode, uint flags) {
                VirtualKey = virtualKey;
                ScanCode = scanCode;
                Flags = flags;
                Time = 0;
                ExtraInfo = extraInfo;
            }
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct Mouse {
            public int X;
            public int Y;
            public uint Data;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;

            /// <summary>
            /// Define a mouse input.
            /// </summary>
            /// <param name="x">X.</param>
            /// <param name="y">Y.</param>
            /// <param name="data">Data.</param>
            /// <param name="flags">Flags.</param>
            public Mouse(int x, int y, uint data, uint flags) {
                X = x;
                Y = y;
                Data = data;
                Flags = flags;
                Time = 0;
                ExtraInfo = extraInfo;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct Hardware {
            public int Message;
            public short ParamLow;
            public short ParamHigh;
        }
        
        [StructLayout(LayoutKind.Explicit)]
        public struct Input {
            [FieldOffset(0)] public uint Type;
            [FieldOffset(8)] public Mouse Mouse;
            [FieldOffset(8)] public Keyboard Keyboard;
            [FieldOffset(8)] public Hardware Hardware;

            /// <summary>
            /// Define a keyboard input.
            /// </summary>
            /// <param name="key">Key.</param>
            /// <param name="deviceEvent">Device event.</param>
            public Input(Keys key, DeviceEvent deviceEvent) {
                ushort virtualKey = (ushort)key;
                uint flags = deviceEvent.GetFlags(key);
                
                Hardware = new Hardware();

                if (key.IsMouseKey()) {
                    // Mouse
                    Type = (uint)Device.Mouse;
                    Keyboard = new Keyboard();
                    Mouse = new Mouse(x: 0, y: 0, data: key.GetMouseData(), flags: flags);
                } else {
                    // Keyboard
                    Type = (uint)Device.Keyboard;
                    Mouse = new Mouse();
                    Keyboard = new Keyboard(virtualKey, (ushort)WinAPI.MapVirtualKeyEx(virtualKey, 0, keyboardLayout), flags);
                }
            }

            /// <summary>
            /// Define a keyboard input by a character.
            /// </summary>
            /// <param name="deviceEvent">Device event.</param>
            /// <param name="c">Character.</param>
            public Input(DeviceEvent deviceEvent, char c) {
                Type = (uint)Device.Keyboard;
                Mouse = new Mouse();
                Hardware = new Hardware();
                Keyboard = new Keyboard(0, c, deviceEvent.GetFlags(c));
            }
            
            /// <summary>
            /// Define a mouse input.
            /// </summary>
            /// <param name="deviceEvent">Device event.</param>
            /// <param name="value">Value for wheel or tilt.</param>
            public Input(DeviceEvent deviceEvent, int value) {
                Type = (uint)Device.Mouse;
                Keyboard = new Keyboard();
                Hardware = new Hardware();
                Mouse = new Mouse(x: 0, y: 0, data: (uint)(value * 120), flags: deviceEvent.GetFlags());
            }
        }

        public class Inputs {
            // Device inputs
            private List<Input> inputs = new List<Input>();

            /// <summary>
            /// Cast inputs to an array.
            /// </summary>
            /// <returns>Inputs</returns>
            public Input[] ToArray() {
                return inputs.ToArray();
            }
            
            /// <summary>
            /// Define inputs for key down.
            /// </summary>
            /// <param name="key">Key.</param>
            /// <param name="n">Number of inputs.</param>
            /// <returns>Inputs.</returns>
            public Inputs KeyDown(Keys key, int n=1) {
                for (int i = 0; i < n; i++) {
                    inputs.Add(new Input(key, DeviceEvent.KeyDown));
                }
                return this;
            }
            
            /// <summary>
            /// Define multiple inputs for key down.
            /// </summary>
            /// <param name="keys">Keys.</param>
            /// <param name="n">Number of inputs.</param>
            /// <returns>Inputs.</returns>
            public Inputs KeyDown(Keys[] keys, int n=1) {
                for (int i = 0; i < n; i++) {
                    foreach (Keys key in keys) KeyDown(key, n: 1);
                }
                return this;
            }
            
            /// <summary>
            /// Define inputs for key up.
            /// </summary>
            /// <param name="key">Key.</param>
            /// <param name="n">Number of inputs.</param>
            /// <returns>Inputs.</returns>
            public Inputs KeyUp(Keys key, int n=1) {
                for (int i = 0; i < n; i++) {
                    inputs.Add(new Input(key, DeviceEvent.KeyUp));
                }
                return this;
            }
            
            /// <summary>
            /// Define multiple inputs for key up.
            /// </summary>
            /// <param name="keys">Keys.</param>
            /// <param name="n">Number of inputs.</param>
            /// <returns>Inputs.</returns>
            public Inputs KeyUp(Keys[] keys, int n=1) {
                for (int i = 0; i < n; i++) {
                    foreach (Keys key in keys) KeyUp(key, n: 1);
                }
                return this;
            }
            
            /// <summary>
            /// Define inputs for key press.
            /// </summary>
            /// <param name="key">Key.</param>
            /// <param name="n">Number of inputs.</param>
            /// <returns>Inputs.</returns>
            public Inputs KeyPress(Keys key, int n=1) {
                for (int i = 0; i < n; i++) {
                    inputs.Add(new Input(key, DeviceEvent.KeyDown));
                    inputs.Add(new Input(key, DeviceEvent.KeyUp));
                }
                return this;
            }
            
            /// <summary>
            /// Define multiple inputs for key press.
            /// </summary>
            /// <param name="keys">Keys.</param>
            /// <param name="n">Number of inputs.</param>
            /// <returns>Inputs.</returns>
            public Inputs KeyPress(Keys[] keys, int n=1) {
                for (int i = 0; i < n; i++) {
                    foreach (Keys key in keys) KeyPress(key, n: 1);
                }
                return this;
            }
            
            /// <summary>
            /// Define inputs for key press by a text.
            /// </summary>
            /// <param name="text">Text.</param>
            /// <param name="n">Number of inputs.</param>
            /// <returns>Inputs.</returns>
            public Inputs KeyPress(string text, int n=1) {
                for (int i = 0; i < n; i++) {
                    foreach (char c in text) {
                        inputs.Add(new Input(DeviceEvent.KeyDown, c));
                        inputs.Add(new Input(DeviceEvent.KeyUp, c));
                    }
                }
                return this;
            }
            
            /// <summary>
            /// Define multiple inputs for key press by texts.
            /// </summary>
            /// <param name="texts">Texts.</param>
            /// <param name="n">Number of inputs.</param>
            /// <returns>Inputs.</returns>
            public Inputs KeyPress(string[] texts, int n=1) {
                for (int i = 0; i < n; i++) {
                    foreach (string text in texts) KeyPress(text, n: 1);
                }
                return this;
            }
            
            /// <summary>
            /// Define inputs for mouse tilt.
            /// </summary>
            /// <param name="value">Value.</param>
            /// <param name="n">Number of inputs.</param>
            /// <returns>Inputs</returns>
            public Inputs Tilt(int value, int n=1) {
                for (int i = 0; i < n; i++) {
                    inputs.Add(new Input(DeviceEvent.MouseTilt, value));
                }
                return this;
            }
            
            /// <summary>
            /// Define inputs for mouse wheel.
            /// </summary>
            /// <param name="value">Value.</param>
            /// <param name="n">Number of inputs.</param>
            /// <returns>Inputs.</returns>
            public Inputs Wheel(int value, int n=1) {
                for (int i = 0; i < n; i++) {
                    inputs.Add(new Input(DeviceEvent.MouseWheel, value));
                }
                return this;
            }
        }
    }
    
    public class KeyState {
        public Keys Target { get; set; }
        public Dictionary<Keys, bool> Keys { get; set; }

        /// <summary>
        /// Define a key state.
        /// </summary>
        public KeyState() {
            Target = System.Windows.Forms.Keys.None;

            // Check which keys are pressed.
            Keys = new Dictionary<Keys, bool>();
            foreach (Keys key in ExtendKeys.Unique) Keys.Add(key, key.IsPressed());
        }
    }

    public class MouseState {
        public Point Target { get; set; }
        public Point Position { get; set; }
        public Point Scroll { get; set; }

        /// <summary>
        /// Define a mouse state.
        /// </summary>
        public MouseState() {
            Target = new Point(0, 0);

            Position = System.Windows.Forms.Cursor.Position;
            Scroll = new Point(0, 0);
        }
    }

    public class DeviceState {
        public DeviceEvent DeviceEvent { get; set; }
        public KeyState Key { get; set; }
        public MouseState Mouse { get; set; }

        /// <summary>
        /// Define device states.
        /// </summary>
        /// <param name="key">Key state.</param>
        /// <param name="mouse">Mouse state.</param>
        public DeviceState(KeyState key=null, MouseState mouse=null) {
            DeviceEvent = DeviceEvent.Nothing;
            Key = key is null ? new KeyState() : key;
            Mouse = mouse is null ? new MouseState() : mouse;
        }

        /// <summary>
        /// Set the target for key event.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <param name="target">Target key.</param>
        public void SetKeyTarget(DeviceEvent deviceEvent, Keys target) {
            DeviceEvent = deviceEvent;
            Key.Target = target;
            bool isPressed = deviceEvent == DeviceEvent.KeyDown;
            Key.Keys[target] = isPressed;
            
            switch (target) {
                case Keys.LControlKey: Key.Keys[Keys.ControlKey] = isPressed || Keys.RControlKey.IsPressed(); break;
                case Keys.RControlKey: Key.Keys[Keys.ControlKey] = isPressed || Keys.LControlKey.IsPressed(); break;
                case Keys.LMenu: Key.Keys[Keys.Menu] = isPressed || Keys.RMenu.IsPressed(); break;
                case Keys.RMenu: Key.Keys[Keys.Menu] = isPressed || Keys.LMenu.IsPressed(); break;
                case Keys.LShiftKey: Key.Keys[Keys.ShiftKey] = isPressed || Keys.RShiftKey.IsPressed(); break;
                case Keys.RShiftKey: Key.Keys[Keys.ShiftKey] = isPressed || Keys.LShiftKey.IsPressed(); break;
            }
        }

        /// <summary>
        /// Set the target for mouse event.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <param name="target">Target point.</param>
        public void SetMouseTarget(DeviceEvent deviceEvent, Point target) {
            DeviceEvent = deviceEvent;
            Mouse.Target = target;
            if (deviceEvent == DeviceEvent.MouseMove)
                Mouse.Position = target;
            else if (deviceEvent == DeviceEvent.MouseWheel || deviceEvent == DeviceEvent.MouseTilt)
                Mouse.Scroll = target;
        }
    }

    public static class EventBuilder {
        /// <summary>
        /// Get a name for device event.
        /// </summary>
        /// <param name="namespaces">Namespaces.</param>
        /// <returns>Name.</returns>
        public static string GetName(string[] namespaces) {
            for (int i = 0; i < namespaces.Length; i++) {
                // To use periods as delimiters, replace namespaces which has periods to hashes.
                if (namespaces[i].Contains("."))
                    namespaces[i] = String.Join("", new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(namespaces[i])).Select(x => $"{x:x2}"));
            }

            return String.Join(".", namespaces);
        }
    }

    public static partial class ExtendKeys {
        // Keys which need extended key events.
        private static List<Keys> extendedKeys = new List<Keys> { 
            Keys.Menu, Keys.RMenu, Keys.ControlKey, Keys.RControlKey,
            Keys.Insert, Keys.Delete, Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown,
            Keys.Up, Keys.Left, Keys.Down, Keys.Right,
            Keys.NumLock, Keys.Cancel, Keys.PrintScreen, Keys.Divide,
            // Numpad enter key does not exist in Keys
        };

        /// <summary>
        /// Check if a key is extended or not.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Extended or not.</returns>
        public static bool IsExtendedKey(this Keys key) {
            return extendedKeys.Contains(key);
        }

        // Mouse keys
        public static List<Keys> Mouse = new List<Keys> {
            Keys.LButton, Keys.MButton, Keys.RButton, Keys.XButton1, Keys.XButton2
        };

        // Unique keys (removed duplicated values from Keys)
        public static List<Keys> Unique = new List<Keys> {
            Keys.Modifiers, Keys.None, Keys.Cancel, Keys.Back, Keys.Tab, Keys.LineFeed, Keys.Clear, Keys.Enter, Keys.ShiftKey, Keys.ControlKey, Keys.Menu, Keys.Pause, Keys.CapsLock, Keys.KanaMode, Keys.JunjaMode, Keys.FinalMode, Keys.KanjiMode, Keys.Escape, Keys.IMEConvert, Keys.IMENonconvert, Keys.IMEAccept, Keys.IMEModeChange, Keys.Space, Keys.PageUp, Keys.PageDown, Keys.End, Keys.Home, Keys.Left, Keys.Up, Keys.Right, Keys.Down, Keys.Select, Keys.Print, Keys.Execute, Keys.PrintScreen, Keys.Insert, Keys.Delete, Keys.Help, Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G, Keys.H, Keys.I, Keys.J, Keys.K, Keys.L, Keys.M, Keys.N, Keys.O, Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T, Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z, Keys.LWin, Keys.RWin, Keys.Apps, Keys.Sleep, Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9, Keys.Multiply, Keys.Add, Keys.Separator, Keys.Subtract, Keys.Decimal, Keys.Divide, Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12, Keys.F13, Keys.F14, Keys.F15, Keys.F16, Keys.F17, Keys.F18, Keys.F19, Keys.F20, Keys.F21, Keys.F22, Keys.F23, Keys.F24, Keys.NumLock, Keys.Scroll, Keys.LShiftKey, Keys.RShiftKey, Keys.LControlKey, Keys.RControlKey, Keys.LMenu, Keys.RMenu, Keys.BrowserBack, Keys.BrowserForward, Keys.BrowserRefresh, Keys.BrowserStop, Keys.BrowserSearch, Keys.BrowserFavorites, Keys.BrowserHome, Keys.VolumeMute, Keys.VolumeDown, Keys.VolumeUp, Keys.MediaNextTrack, Keys.MediaPreviousTrack, Keys.MediaStop, Keys.MediaPlayPause, Keys.LaunchMail, Keys.SelectMedia, Keys.LaunchApplication1, Keys.LaunchApplication2, Keys.OemSemicolon, Keys.Oemplus, Keys.Oemcomma, Keys.OemMinus, Keys.OemPeriod, Keys.OemQuestion, Keys.Oemtilde, Keys.OemOpenBrackets, Keys.OemPipe, Keys.OemCloseBrackets, Keys.OemQuotes, Keys.Oem8, Keys.OemBackslash, Keys.ProcessKey, Keys.Packet, Keys.Attn, Keys.Crsel, Keys.Exsel, Keys.EraseEof, Keys.Play, Keys.Zoom, Keys.NoName, Keys.Pa1, Keys.OemClear, Keys.Shift, Keys.Control, Keys.Alt,
            Keys.LButton, Keys.MButton, Keys.RButton, Keys.XButton1, Keys.XButton2
        };

        /// <summary>
        /// Check if a key belongs to mouse or not.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Belongs to mouse or not.</returns>
        public static bool IsMouseKey(this Keys key) {
            return Mouse.Contains(key);
        }

        /// <summary>
        /// Get mouse data.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Mouse data.</returns>
        public static uint GetMouseData(this Keys key) {
            switch (key) {
                case Keys.XButton1: return 0x0001;
                case Keys.XButton2: return 0x0002;
                default: return 0;
            }
        }
        
        /// <summary>
        /// Check if a key is pressed or not.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Pressed or not.</returns>
        public static bool IsPressed(this Keys key) {
            return WinAPI.GetAsyncKeyState(key) < 0;
        }
    }

    public static partial class ExtendDeviceEvent {
        // Device event for Windows API.
        private static class Event {
            public const uint KeyboardKeyDown     = 0x0;
            public const uint KeyboardKeyUp       = 0x2;
            public const uint KeyboardKeyExtended = 0x1;
            public const uint KeyboardKeyDownExtended = KeyboardKeyExtended | KeyboardKeyDown;
            public const uint KeyboardKeyUpExtended   = KeyboardKeyExtended | KeyboardKeyUp;
            public const uint KeyboardUnicodeDown = 0x4;
            public const uint KeyboardUnicodeUp   = KeyboardUnicodeDown | KeyboardKeyUp;

            public const uint MouseMove        = 0x0001;
            public const uint MouseLButtonDown = 0x0002;
            public const uint MouseLButtonUp   = 0x0004;
            public const uint MouseRButtonDown = 0x0008;
            public const uint MouseRButtonUp   = 0x0010;
            public const uint MouseMButtonDown = 0x0020;
            public const uint MouseMButtonUp   = 0x0040;
            public const uint MouseXButtonDown = 0x0100;
            public const uint MouseXButtonUp   = 0x0200;
            public const uint MouseWheel       = 0x0800;
            public const uint MouseTilt        = 0x1000;
            public const uint MouseAbsolute    = 0x8000;
        }

        private static List<DeviceEvent> keyEvents = new List<DeviceEvent> { DeviceEvent.KeyDown, DeviceEvent.KeyPress, DeviceEvent.KeyUp };
        private static List<DeviceEvent> mouseEvents = new List<DeviceEvent> { DeviceEvent.MouseMove, DeviceEvent.MouseTilt, DeviceEvent.MouseWheel };

        /// <summary>
        /// Get device.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <returns>device.</returns>
        internal static Device GetDevice(this DeviceEvent deviceEvent) {
            if (keyEvents.Contains(deviceEvent)) return Device.Keyboard;
            else if (mouseEvents.Contains(deviceEvent)) return Device.Mouse;
            else return Device.Nothing;
        }

        /// <summary>
        /// Check if an event is key event or not.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <returns>Key event or not.</returns>
        public static bool IsKeyEvent(this DeviceEvent deviceEvent) {
            return deviceEvent.GetDevice() == Device.Keyboard;
        }
        
        /// <summary>
        /// Check if an event is mouse event or not.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <returns>Mouse event or not.</returns>
        public static bool IsMouseEvent(this DeviceEvent deviceEvent) {
            return deviceEvent.GetDevice() == Device.Mouse;
        }

        /// <summary>
        /// Get flags for key event.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <param name="key">Key.</param>
        /// <returns>Flags.</returns>
        public static uint GetFlags(this DeviceEvent deviceEvent, Keys key) {
            switch (deviceEvent) {
                case DeviceEvent.KeyDown:
                    switch (key) {
                        case Keys.LButton: return Event.MouseLButtonDown;
                        case Keys.MButton: return Event.MouseMButtonDown;
                        case Keys.RButton: return Event.MouseRButtonDown;
                        case Keys.XButton1: return Event.MouseXButtonDown;
                        case Keys.XButton2: return Event.MouseXButtonDown;
                        default: return key.IsExtendedKey() ? Event.KeyboardKeyDownExtended : Event.KeyboardKeyDown;
                    }
                case DeviceEvent.KeyUp:
                    switch (key) {
                        case Keys.LButton: return Event.MouseLButtonUp;
                        case Keys.MButton: return Event.MouseMButtonUp;
                        case Keys.RButton: return Event.MouseRButtonUp;
                        case Keys.XButton1: return Event.MouseXButtonUp;
                        case Keys.XButton2: return Event.MouseXButtonUp;
                        default: return key.IsExtendedKey() ? Event.KeyboardKeyUpExtended : Event.KeyboardKeyUp;
                    }
            }

            throw new ArgumentException($"No flags for {deviceEvent}");
        }

        /// <summary>
        /// Get flags for key event by a character.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <param name="c">Character.</param>
        /// <returns>Flags.</returns>
        public static uint GetFlags(this DeviceEvent deviceEvent, char c) {
            switch (deviceEvent) {
                case DeviceEvent.KeyDown: return Event.KeyboardUnicodeDown;
                case DeviceEvent.KeyUp:   return Event.KeyboardUnicodeUp;
            }

            throw new ArgumentException($"No flags for {deviceEvent}");
        }

        /// <summary>
        /// Get flags for mouse event.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <returns>Flags.</returns>
        public static uint GetFlags(this DeviceEvent deviceEvent) {
            switch (deviceEvent) {
                case DeviceEvent.MouseTilt:  return Event.MouseTilt;
                case DeviceEvent.MouseWheel: return Event.MouseWheel;
            }

            throw new ArgumentException($"No flags for {deviceEvent}");
        }
    }
}