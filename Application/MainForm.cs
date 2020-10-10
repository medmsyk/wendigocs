using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Application {
    public partial class MainForm : Form {
        // GUI Timer
        private static Timer timer = new Timer();

        /// <summary>
        /// Listeners of the GUI timer.
        /// </summary>
        public static List<Action> TimerListeners = new List<Action>();

        /// <summary>
        /// Create a form which manages the GUI thread.
        /// To listen for GUI timer events, add Action to MainForm.TimerListeners.
        /// </summary>
        /// <param name="interval">Interval of the GUI timer.</param>
        public MainForm(int interval=10) {
            InitializeComponent();

            timer.Interval = interval;
            timer.Tick += new EventHandler(DispatchTimerEvents);
            timer.Start();
        }

        /// <summary>
        /// Dispatch GUI timer events.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event args.</param>
        private void DispatchTimerEvents(object sender, EventArgs e) {
            foreach (Action listener in TimerListeners) {
                listener();
            }
        }
    }
}
