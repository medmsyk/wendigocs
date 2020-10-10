using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Device.Event.WinAPI;
using static Device.Event.Structure;

namespace Device.Event{
    public static class EventSimulator {
        /// <summary>
        /// Simulate device event.
        /// </summary>
        /// <param name="inputs">Inputs.</param>
        public static void Simulate(Inputs inputs) {
            Input[] array = inputs.ToArray();
            SendInput((uint)array.Length, ref array[0], Marshal.SizeOf(array[0]));
        }
        
        /// <summary>
        /// Simulate key down.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="n">Number of inputs.</param>
        public static void KeyDown(Keys key, int n=1) {
            Simulate(new Inputs().KeyDown(key, n: n));
        }
        
        /// <summary>
        /// Simulate key up.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="n">Number of inputs.</param>
        public static void KeyUp(Keys key, int n=1) {
            Simulate(new Inputs().KeyUp(key, n: n));
        }
        
        /// <summary>
        /// Simulate key press.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="n">Number of inputs.</param>
        public static void KeyPress(Keys key, int n=1) {
            Simulate(new Inputs().KeyPress(key, n:n));
        }
        
        /// <summary>
        /// Type text.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="n">Number of inputs.</param>
        public static void TypeText(string text, int n=1) {
            Simulate(new Inputs().KeyPress(text, n: n));
        }

        /// <summary>
        /// Point a pixel.
        /// </summary>
        /// <param name="position">Position.</param>
        /// <param name="absolute">Use the position as an absolute one or not.</param>
        internal static void Point(Point position, bool absolute) {
            if (absolute)
                Cursor.Position = position;
            else
                Cursor.Position = new Point(Cursor.Position.X + position.X, Cursor.Position.Y + position.Y);
        }
        
        /// <summary>
        /// Point a pixel by a relative position.
        /// </summary>
        /// <param name="x">Position X.</param>
        /// <param name="y">Position Y.</param>
        public static void PointRelative(int x, int y) {
            Point(new System.Drawing.Point(x, y), false);
        }

        /// <summary>
        /// Point a pixel by an absolute position.
        /// </summary>
        /// <param name="x">Position X.</param>
        /// <param name="y">Position Y.</param>
        public static void PointAbsolute(int x, int y) {
            Point(new System.Drawing.Point(x, y), true);
        }
        
        /// <summary>
        /// Tilt.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="n">Number of inputs.</param>
        public static void Tilt(int value, int n=1) {
            Simulate(new Inputs().Tilt(value, n: n));
        }
        
        /// <summary>
        /// Wheel.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="n">Number of inputs.</param>
        public static void Wheel(int value, int n=1) {
            Simulate(new Inputs().Wheel(value, n: n));
        }
    }
}
