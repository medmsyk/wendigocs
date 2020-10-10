using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace Screen.Target {
    internal struct TargetRequest {
        public Rectangle area;
        public int? borderWidth;
        public Color? borderColor;

        /// <summary>
        /// Make a request to create a target.
        /// </summary>
        /// <param name="area">Area.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColor">Border color.</param>
        public TargetRequest(Rectangle area, int? borderWidth, Color? borderColor) {
            this.area = area;
            this.borderWidth = borderWidth;
            this.borderColor = borderColor;
        }

        /// <summary>
        /// Get a target.
        /// This method must be called in the GUI thread.
        /// </summary>
        /// <returns>Target.</returns>
        public TargetForm GetTarget() {
            return new TargetForm(area, borderWidth:borderWidth, borderColor:borderColor);
        }
    }

    public static class TargetMarkerByArea {
        private const int interval = 10;

        private static Dictionary<string, TargetRequest> targetRequests = new Dictionary<string, TargetRequest>();
        private static Dictionary<string, TargetForm> targetForms = new Dictionary<string, TargetForm>();
        private static object targetLocker = new object();

        /// <summary>
        /// Update targets.
        /// This method must be called in the GUI thread.
        /// </summary>
        public static void UpdateTargets() {
            lock (targetLocker) {
                foreach (string key in new List<string>(targetRequests.Keys)) {
                    targetForms.Add(key, targetRequests[key].GetTarget());
                    targetRequests.Remove(key);
                }
            }
        }

        /// <summary>
        /// Mark an area.
        /// </summary>
        /// <param name="area">Area.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColor">Border color.</param>
        /// <returns>Target.</returns>
        public static TargetForm Mark(Rectangle area, int? borderWidth=null, Color? borderColor=null) {
            string key = $"{Process.GetCurrentProcess().Id} {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            // Make a target request to create a target in the GUI thread.
            // Do not use Invoke since it's too slow.
            lock (targetLocker) targetRequests.Add(key, new TargetRequest(area, borderWidth, borderColor));

            // Wait until a target is created.
            for (int i = 0; i < 10; i++) {
                if (targetForms.ContainsKey(key)) {
                    lock (targetLocker) {
                        TargetForm target = targetForms[key];
                        targetForms.Remove(key);
                        return target;
                    }
                }

                System.Threading.Thread.Sleep(interval);
            }
            
            throw new TimeoutException("Could not create a target form");
        }
    }
}
