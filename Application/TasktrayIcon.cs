using System;
using System.Drawing;
using System.Windows.Forms;

namespace Application {
    public class TasktrayIcon {
        // Notify icon
        private NotifyIcon notifyIcon = new NotifyIcon();

        /// <summary>
        /// Text.
        /// </summary>
        public string Text {
            get { return notifyIcon.Text; }
            set { notifyIcon.Text = value; }
        }

        /// <summary>
        /// Icon.
        /// </summary>
        public Icon Icon {
            get { return notifyIcon.Icon; }
            set { notifyIcon.Icon = value; }
        }
        
        /// <summary>
        /// Craete a tasktray icon.
        /// </summary>
        public TasktrayIcon() {
            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon.Visible = true;
        }

        /// <summary>
        /// Add a menu item.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="eventHandler">Click event handler.</param>
        public void AddMenuItem(string text, EventHandler eventHandler) {
            ToolStripMenuItem item = new ToolStripMenuItem();
            item.Text = text;
            item.Click += eventHandler;
            notifyIcon.ContextMenuStrip.Items.Add(item);
        }

        /// <summary>
        /// Show a baloon tip.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="text">Text.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="icon">Tool tip icon.</param>
        public void ShowBalloonTip(string title, string text, int timeout, ToolTipIcon icon) {
            Console.WriteLine($"{nameof(icon)} - {title}: {text}");
            notifyIcon.ShowBalloonTip(timeout * 1000, title, text, icon);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose() {
            notifyIcon.Dispose();
        }
    }
}
