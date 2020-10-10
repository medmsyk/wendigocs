using System.Drawing;
using System.Windows.Forms;
using Application;

namespace Screen.Target {
    public static class TargetMarker {
        /// <summary>
        /// Initialize.
        /// </summary>
        static TargetMarker() {
            MainForm.TimerListeners.Add(UpdateTargets);
        }

        /// <summary>
        /// Update targets.
        /// This method must be called in the GUI thread.
        /// </summary>
        public static void UpdateTargets() {
            TargetMarkerByArea.UpdateTargets();
            TargetMarkerByDrag.UpdateTargets();
        }

        /// <summary>
        /// Mark an area.
        /// </summary>
        /// <param name="area">Area.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColor">Border color.</param>
        /// <returns>Target.</returns>
        public static TargetForm Mark(Rectangle area, int? borderWidth = null, Color? borderColor = null) {
            return TargetMarkerByArea.Mark(area, borderWidth: borderWidth, borderColor: borderColor);
        }

        /// <summary>
        /// Mark areas by drag.
        /// </summary>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="keys">Keys.</param>
        /// <param name="n">Number of targets.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColor">Border color.</param>
        public static void MarkByDrag(TargetMarkEventHandler eventHandler, Keys[] keys=null, int n=1, int? borderWidth=null, Color? borderColor=null){
            TargetMarkerByDrag.Mark(keys, n, eventHandler, borderWidth, borderColor);
        }
        
        /// <summary>
        /// Mark areas by drag with multiple border widths.
        /// </summary>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="keys">Keys.</param>
        /// <param name="n">Number of targets.</param>
        /// <param name="borderWidths">Border widths.</param>
        /// <param name="borderColor">Border color.</param>
        public static void MarkByDrag(TargetMarkEventHandler eventHandler, Keys[] keys=null, int n=1, int[] borderWidths=null, Color? borderColor=null){
            TargetMarkerByDrag.Mark(keys, n, eventHandler, borderWidths, borderColor);
        }
        
        /// <summary>
        /// Mark areas by drag with multiple border colors.
        /// </summary>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="keys">Keys.</param>
        /// <param name="n">Number of targets.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColors">Border colors.</param>
        public static void MarkByDrag(TargetMarkEventHandler eventHandler, Keys[] keys=null, int n=1, int? borderWidth=null, Color[] borderColors=null){
            TargetMarkerByDrag.Mark(keys, n, eventHandler, borderWidth, borderColors);
        }
        
        /// <summary>
        /// Mark areas by drag with multiple border widths and colors.
        /// </summary>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="keys">Keys.</param>
        /// <param name="n">Number of targets.</param>
        /// <param name="borderWidths">Border widths.</param>
        /// <param name="borderColors">Border colors.</param>
        public static void MarkByDrag(TargetMarkEventHandler eventHandler, Keys[] keys=null, int n=1, int[] borderWidths=null, Color[] borderColors=null){
            TargetMarkerByDrag.Mark(keys, n, eventHandler, borderWidths, borderColors);
        }
    }
}
