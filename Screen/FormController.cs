using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static Screen.WinAPI;
using static Screen.Structure;

namespace Screen {
    public static class FormController {
        /// <summary>
        /// Get form information.
        /// </summary>
        /// <returns>List of form class and title.</returns>
        public static List<string[]> GetFormInfo() {
            List<string[]> names = new List<string[]>();

            foreach (Process p in Process.GetProcesses().Where(
                p => p.MainWindowHandle != IntPtr.Zero && p.MainWindowTitle.Length > 0)) {
                StringBuilder sb = new StringBuilder(256);
                GetClassName(p.MainWindowHandle, sb, sb.Capacity);
                names.Add(new string[] { sb.ToString(), p.MainWindowTitle });
            }

            return names;
        }

        /// <summary>
        /// Get a form handle by a class or title.
        /// </summary>
        /// <param name="formClass">Form class.</param>
        /// <param name="formTitle">Form title.</param>
        /// <returns>Form handle.</returns>
        private static IntPtr GetFormHandle(string formClass=null, string formTitle=null) {
            if (formClass is null && formTitle is null) throw new ArgumentException("Specify form class or form title");

            return FindWindow(formClass, formTitle);
        }

        /// <summary>
        /// Get a form area.
        /// </summary>
        /// <param name="handle">Form handle.</param>
        /// <param name="clientOnly">Only the client area or not.</param>
        /// <returns>Area.</returns>
        private static Rectangle? GetArea(IntPtr handle, bool clientOnly) {
            if (handle != IntPtr.Zero) {
                bool aeroEnabled = false;
                DwmIsCompositionEnabled(out aeroEnabled);

                if (aeroEnabled && !clientOnly) {
                    Rect rect = new Rect();
                    DwmGetWindowAttribute(handle, DwmWindowAttributes.ExtendedFrameBounds, out rect, Marshal.SizeOf(rect));
                    return new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
                } else {
                    var wi = new WindowInfo();
                    wi.cbSize = Marshal.SizeOf(wi);
                    GetWindowInfo(handle, ref wi);
                    Rect rect = clientOnly ? wi.rcClient : wi.rcWindow;
                    return new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
                }
            }

            return null;
        }

        /// <summary>
        /// Get an area by a form class or title.
        /// </summary>
        /// <param name="formClass">Form class.</param>
        /// <param name="formTitle">Form title.</param>
        /// <param name="clientOnly">Only the client area or not.</param>
        /// <returns>Area.</returns>
        public static Rectangle? GetArea(string formClass=null, string formTitle=null, bool clientOnly=false) {
            IntPtr handle = GetFormHandle(formClass: formClass, formTitle: formTitle);
            return GetArea(handle, clientOnly);
        }
        
        /// <summary>
        /// Activate a form.
        /// This method restore a form and bring it to the front.
        /// </summary>
        /// <param name="handle">Form handle.</param>
        private static void ActivateForm(IntPtr handle) {
            RestoreForm(handle);
            SetForegroundWindow(handle);
        }

        /// <summary>
        /// Activate a form by a class or title.
        /// This method restore a form and bring it to the front.
        /// </summary>
        /// <param name="formClass">Form class.</param>
        /// <param name="formTitle">Form title.</param>
        public static void ActivateForm(string formClass=null, string formTitle=null) {
            IntPtr handle = GetFormHandle(formClass: formClass, formTitle: formTitle);
            ActivateForm(handle);
        }
        
        /// <summary>
        /// Maximize a form.
        /// </summary>
        /// <param name="handle">Form handle.</param>
        private static void MaximizeForm(IntPtr handle) {
            ShowWindow(handle, 3);
        }

        /// <summary>
        /// Maximize a form by a class or title.
        /// </summary>
        /// <param name="formClass">Form class.</param>
        /// <param name="formTitle">Form title.</param>
        public static void MaximizeForm(string formClass=null, string formTitle=null) {
            IntPtr handle = GetFormHandle(formClass: formClass, formTitle: formTitle);
            MaximizeForm(handle);
        }
        
        /// <summary>
        /// Minimize a form.
        /// </summary>
        /// <param name="handle">Form handle.</param>
        private static void MinimizeForm(IntPtr handle) {
            ShowWindow(handle, 6);
        }
        
        /// <summary>
        /// Minimize a form by a class or title.
        /// </summary>
        /// <param name="formClass">Form class.</param>
        /// <param name="formTitle">Form title.</param>
        public static void MinimizeForm(string formClass=null, string formTitle=null) {
            IntPtr handle = GetFormHandle(formClass: formClass, formTitle: formTitle);
            MinimizeForm(handle);
        }

        /// <summary>
        /// Restore a form.
        /// </summary>
        /// <param name="handle">Form handle.</param>
        private static void RestoreForm(IntPtr handle) {
            ShowWindow(handle, 9);
        }

        /// <summary>
        /// Restore a form by a class or title.
        /// </summary>
        /// <param name="formClass">Form class.</param>
        /// <param name="formTitle">Form title.</param>
        public static void RestoreForm(string formClass=null, string formTitle=null) {
            IntPtr handle = GetFormHandle(formClass: formClass, formTitle: formTitle);
            RestoreForm(handle);
        }

        /// <summary>
        /// Get color of a pixel.
        /// </summary>
        /// <param name="x">Position X.</param>
        /// <param name="y">Position Y.</param>
        /// <returns>Color.</returns>
        public static Color GetColor(int x, int y) {
            Bitmap bitmap = new Bitmap(1, 1);

            using (Graphics g = Graphics.FromImage(bitmap))
                g.CopyFromScreen(new Point(x, y), Point.Empty, new Size(1, 1));
            
            return bitmap.GetPixel(0, 0);
        }
    }
}
