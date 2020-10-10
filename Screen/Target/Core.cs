using System;
using System.Collections.Generic;

namespace Screen.Target {
    /// <summary>
    /// Target mark event args which contains targets.
    /// </summary>
    public class TargetMarkEventArgs : EventArgs {
        public List<TargetForm> Targets { get; set; }
        public TargetMarkEventArgs(List<TargetForm> targets) { Targets = targets; }
    }

    /// <summary>
    /// Target mark event handler.
    /// </summary>
    /// <param name="e">Target mark event args.</param>
    public delegate void TargetMarkEventHandler(TargetMarkEventArgs e);
}
