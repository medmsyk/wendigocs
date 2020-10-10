using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using FormApplication = System.Windows.Forms.Application;
using Application.Properties;

namespace Application {
    public static class Wendigo {
        // Tasktray icon
        private static TasktrayIcon tasktrayIcon;

        /// <summary>
        /// Initialize.
        /// </summary>
        static Wendigo() {
            tasktrayIcon = new TasktrayIcon();
        }
        
        /// <summary>
        /// Show a baloon tip.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="timeout"></param>
        public static void Notify(string title, string text, int timeout=1) {
            // Use ToolTipIcon.None not to show a process name.
            tasktrayIcon.ShowBalloonTip(title, text, timeout, ToolTipIcon.None);
        }

        /// <summary>
        /// Run.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="iconPath">Icon file path.</param>
        /// <param name="exitCaption">Caption for the tasktray item to exit.</param>
        public static void Run(string name="Wendigo", string iconPath=null, string exitCaption="Exit") {
            tasktrayIcon.Text = name;
            tasktrayIcon.Icon = iconPath is null ? Resources.app : new Icon(iconPath);

            // Disable Ctrl+C not to raise exception in the caller process (e.g. KeyboardInterrupt).
            Console.TreatControlCAsInput = true;

            // Dispose the tasktray icon if whatever happens.
            FormApplication.ApplicationExit += Dispose;
            FormApplication.ThreadException += Dispose;
            Thread.GetDomain().UnhandledException += Dispose;
            
            // Add a tasktray item to exit.
            tasktrayIcon.AddMenuItem(exitCaption, Exit);

            Notify("Wendigo", "Activated Wendigo.");
            FormApplication.Run(new MainForm());
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event args.</param>
        private static void Dispose(object sender, EventArgs e) {
            tasktrayIcon.Dispose();
        }

        /// <summary>
        /// Exit.
        /// </summary>
        public static void Exit() {
            Notify("Wendigo", "Deactivated Wendigo.");
            FormApplication.Exit();
        }

        /// <summary>
        /// Exit.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event args.</param>
        public static void Exit(object sender, EventArgs e) {
            Exit();
        }
    }
}
