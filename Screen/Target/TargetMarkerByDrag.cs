using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Device.Event;

namespace Screen.Target {
    internal struct TargetRequestByDrag {
        public Rectangle? area;
        public int? borderWidth;
        public Color? borderColor;

        /// <summary>
        /// Make a request to create a target by drag.
        /// </summary>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColor">Border color.</param>
        public TargetRequestByDrag(int? borderWidth, Color? borderColor) {
            this.area = null;
            this.borderWidth = borderWidth;
            this.borderColor = borderColor;
        }

        /// <summary>
        /// Get a target.
        /// This method must be called in the GUI thread.
        /// </summary>
        /// <returns>Target.</returns>
        public TargetForm GetTarget() {
            if (area == null) throw new InvalidOperationException("Set area first");

            return new TargetForm((Rectangle)area, borderWidth: borderWidth, borderColor:borderColor);
        }
    }

    internal class TargetStateByDrag {
        private TargetRequestByDrag[] targetRequests;
        public TargetForm[] targetForms;
        public int index = 0;
        
        public Point? dragStart = null;

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="n">Number of targets.</param>
        /// <param name="getTargetRequest">Callback to get a target request.</param>
        private void Initialize(int n, Func<int, TargetRequestByDrag> getTargetRequest) {
            targetRequests = new TargetRequestByDrag[n];
            for (int i = 0; i < n; i++)
                targetRequests[i] = getTargetRequest(i);

            targetForms = new TargetForm[n];
        }

        /// <summary>
        /// Make a target state.
        /// </summary>
        /// <param name="n">Number of targets.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColor">Border color.</param>
        public TargetStateByDrag(int n, int? borderWidth, Color? borderColor) {
            Initialize(n, (i) => { return new TargetRequestByDrag(borderWidth, borderColor); });
        }
        
        /// <summary>
        /// Make a target state with multiple border widths.
        /// </summary>
        /// <param name="n">Number of targets.</param>
        /// <param name="borderWidths">Border widths.</param>
        /// <param name="borderColor">Border color.</param>
        public TargetStateByDrag(int n, int[] borderWidths, Color? borderColor) {
            if (borderWidths == null) borderWidths = new int[0];
            Initialize(n, (i) => { return new TargetRequestByDrag(i < borderWidths.Length ? (int?)borderWidths[i] : null, borderColor); });
        }
        
        /// <summary>
        /// Make a target state with multiple border colors.
        /// </summary>
        /// <param name="n">Number of targets.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColors">Border colors.</param>
        public TargetStateByDrag(int n, int? borderWidth, Color[] borderColors) {
            if (borderColors == null) borderColors = new Color[0];
            Initialize(n, (i) => { return new TargetRequestByDrag(borderWidth, i < borderColors.Length ? (Color?)borderColors[i] : null); });
        }
        
        /// <summary>
        /// Make a target state with multiple border widths and colors.
        /// </summary>
        /// <param name="n">Number of targets.</param>
        /// <param name="borderWidths">Border widths.</param>
        /// <param name="borderColors">Border colors.</param>
        public TargetStateByDrag(int n, int[] borderWidths, Color[] borderColors) {
            if (borderWidths == null) borderWidths = new int[0];
            if (borderColors == null) borderColors = new Color[0];
            Initialize(n, (i) => { return new TargetRequestByDrag(i < borderWidths.Length ? (int?)borderWidths[i] : null, i < borderColors.Length ? (Color?)borderColors[i] : null); });
        }

        /// <summary>
        /// Update area.
        /// </summary>
        /// <param name="mousePosition">Mouse position.</param>
        public void UpdateArea(Point mousePosition) {
            if (dragStart == null) dragStart = mousePosition;

            Point start = (Point)dragStart;
            Point end = mousePosition;
                    
            targetRequests[index].area = new Rectangle(
                start.X <= end.X ? start.X : end.X, start.Y <= end.Y ? start.Y : end.Y,
                Math.Abs(start.X - end.X), Math.Abs(start.Y - end.Y)
            );
        }
        
        /// <summary>
        /// Update targets.
        /// This method must be called in the GUI thread.
        /// </summary>
        public void UpdateTargets() {
            for (int i=0; i<targetRequests.Length; i++){
                if (targetRequests[i].area is null) continue;    // Not created yet.

                Rectangle area = (Rectangle)targetRequests[i].area;

                if (targetForms[i] is null)
                    targetForms[i] = targetRequests[i].GetTarget();
                else
                    targetForms[i].Area = area;
            }
        }

        /// <summary>
        /// Get targets.
        /// </summary>
        /// <returns>Targets.</returns>
        public List<TargetForm> GetTargets() {
            return new List<TargetForm>(targetForms);
        }

        /// <summary>
        /// Move to the next target.
        /// </summary>
        /// <returns>Out of range or not.</returns>
        public bool Next() {
            index += 1;
            dragStart = null;
            return index < targetRequests.Length;
        }
    }

    internal static class TargetMarkerByDrag {
        private static Keys[] defaultKeys = new Keys[]{ Keys.LButton };

        private static TargetStateByDrag state = null;
        private static object stateLocker = new object();
        
        /// <summary>
        /// Update targets.
        /// This method must be called in the GUI thread.
        /// </summary>
        public static void UpdateTargets() {
            if (state != null) state.UpdateTargets();
        }

        /// <summary>
        /// Mark areas by drag.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <param name="n">Number of targets.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="getState">Callback to get a target state.</param>
        private static void Mark(Keys[] keys, int n, TargetMarkEventHandler eventHandler, Func<TargetStateByDrag> getState) {
            if (n <= 0) throw new ArgumentException($"Invalid n: {n}");

            lock (stateLocker) {
                if (state != null) throw new InvalidOperationException("Already marking a target by drag");
                state = getState();
            }

            string eventName = $"{nameof(TargetMarkerByDrag)}.drag";

            if (keys == null) keys = defaultKeys;
            
            EventDispatcher.KeyDown(
                $"{eventName}.start", keys, (e) => {
                    state.UpdateArea(e.State.Mouse.Position);
                }, forSystem:true
            );
            EventDispatcher.KeyUp(
                $"{eventName}.end", keys, (e) => {
                    if (!state.Next()){
                        EventDispatcher.Unlisten(eventName, forSystem:true);
                            
                        TargetMarkEventArgs eventArgs = new TargetMarkEventArgs(state.GetTargets());
                        state = null;

                        eventHandler(eventArgs);
                    }
                }, forSystem:true
            );
        }

        /// <summary>
        /// Mark areas by drag.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <param name="n">Number of targets.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColor">Border color.</param>
        public static void Mark(Keys[] keys, int n, TargetMarkEventHandler eventHandler, int? borderWidth, Color? borderColor) {
            Mark(keys, n, eventHandler, () => { return new TargetStateByDrag(n, borderWidth, borderColor); });
        }
        
        /// <summary>
        /// Mark areas by drag with multiple border widths.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <param name="n">Number of targets.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="borderWidths">Border widths.</param>
        /// <param name="borderColor">Border color.</param>
        public static void Mark(Keys[] keys, int n, TargetMarkEventHandler eventHandler, int[] borderWidths, Color? borderColor) {
            Mark(keys, n, eventHandler, () => { return new TargetStateByDrag(n, borderWidths, borderColor); });
        }
        
        /// <summary>
        /// Mark areas by drag with multiple border colors.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <param name="n">Number of targets.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColors">Border colors.</param>
        public static void Mark(Keys[] keys, int n, TargetMarkEventHandler eventHandler, int? borderWidth, Color[] borderColors) {
            Mark(keys, n, eventHandler, () => { return new TargetStateByDrag(n, borderWidth, borderColors); });
        }
        
        /// <summary>
        /// Mark areas by drag with multiple border widths and colors.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <param name="n">Number of targets.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="borderWidths">Border widths.</param>
        /// <param name="borderColors">Border colors.</param>
        public static void Mark(Keys[] keys, int n, TargetMarkEventHandler eventHandler, int[] borderWidths, Color[] borderColors) {
            Mark(keys, n, eventHandler, () => { return new TargetStateByDrag(n, borderWidths, borderColors); });
        }
    }
}
