using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Application;

namespace Device.Event {
    internal class Recorder {
        public List<Keys> keys;
        public long timestamp;

        /// <summary>
        /// Define settings for a recorder.
        /// </summary>
        /// <param name="startKeys">Keys to start recording.</param>
        /// <param name="stopKeys">Keys to stop recording.</param>
        /// <param name="timestamp">Start time.</param>
        public Recorder(Keys[] startKeys, Keys[] stopKeys, long timestamp) {
            keys = new List<Keys>(startKeys);
            keys.AddRange(stopKeys);
            this.timestamp = timestamp;
        }
    }

    public static class EventImitator {
        // Event recorders
        private static Dictionary<string, Recorder> recorders = new Dictionary<string, Recorder>();
        private static object recorderLocker = new object();

        // Event players
        private static List<string> players = new List<string>();
        private static object playerLocker = new object();
        
        /// <summary>
        /// Show a baloon tip.
        /// </summary>
        /// <param name="text"></param>
        private static void Notify(string text) {
            Wendigo.Notify(nameof(EventImitator), text);
        }

        /// <summary>
        /// Record device events.
        /// Key events for startKeys and stopKeys are ignored.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="startKeys">Keys to start recording.</param>
        /// <param name="stopKeys">Keys to stop recording.</param>
        public static void Record(string path, Keys[] startKeys, Keys[] stopKeys) {
            string eventName = EventBuilder.GetName(new string[]{ nameof(EventImitator), "record", "{0}", path });
            
            EventDispatcher.KeyPress(
                String.Format(eventName, "start"), startKeys, (e) => {
                    // Do not raise exceptions in this callback.
                    lock (recorderLocker) {
                        if (recorders.ContainsKey(path)) {
                            Notify($"Already recording to {path}");
                            return;
                        }

                        if (File.Exists(path)) File.Delete(path);

                        Notify($"Started recording to {path}");
                        recorders.Add(path, new Recorder(startKeys, stopKeys, DateTimeOffset.Now.ToUnixTimeMilliseconds()));
                    }
                }, forSystem:true
            );
            EventDispatcher.KeyPress(
                String.Format(eventName, "stop"), stopKeys, (e) => {
                    // Do not raise exceptions in this callback.
                    lock (recorderLocker) {
                        if (!recorders.ContainsKey(path)) return;

                        Notify($"Stopped recording to {path}");
                        recorders.Remove(path);
                    }
                }, forSystem:true
            );
        }

        /// <summary>
        /// Build a line to record.
        /// </summary>
        /// <param name="state">Device state.</param>
        /// <param name="recorder">Recorder.</param>
        /// <returns></returns>
        private static string BuildLine(DeviceState state, Recorder recorder) {
            long elapsed = DateTimeOffset.Now.ToUnixTimeMilliseconds() - recorder.timestamp;
            string record = $"{elapsed},{state.DeviceEvent}";

            // Ignore key events for startKeys and stopKeys.
            if (!recorder.keys.Contains(state.Key.Target))
                return state.DeviceEvent.IsKeyEvent() ? $"{record},{state.Key.Target}" : $"{record},{state.Mouse.Target.X},{state.Mouse.Target.Y}";
            else
                return "";
        }

        /// <summary>
        /// Record a device event to a file.
        /// </summary>
        /// <param name="state">Device state.</param>
        public static void RecordToFile(DeviceState state) {
            foreach (string path in recorders.Keys) {
                using (StreamWriter writer = new StreamWriter(path, true, Encoding.Default)) {
                    string line = BuildLine(state, recorders[path]);
                    if (line != "") writer.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Play device events.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="startKeys">Keys to start playing.</param>
        /// <param name="stopKeys">Keys to stop playing.</param>
        public static void Play(string path, Keys[] startKeys, Keys[] stopKeys) {
            string eventName = EventBuilder.GetName(new string[]{ nameof(EventImitator), "play", "{0}", path });
            
            EventDispatcher.KeyPress(
                String.Format(eventName, "start"), startKeys, (e) => {
                    // Do not raise exceptions in this callback.
                    lock (playerLocker) {
                        if (!File.Exists(path)) {
                            Notify($"Not found {path}");
                            return;
                        }
                        if (recorders.ContainsKey(path)) {
                            Notify($"Now recording to {path}");
                            return;
                        }
                        if (players.Contains(path)) {
                            Notify($"Already playing {path}");
                            return;
                        }
                        
                        Notify($"Started playing {path}");
                        players.Add(path);
                    }

                    long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    List<Keys> downKeys = new List<Keys>();
                    int lineNo = 1;

                    using (StreamReader reader = new StreamReader(path)) {
                        while (reader.Peek() != -1) {
                            if (!players.Contains(path)) break;    // Stopped

                            string line = reader.ReadLine();
                            if (line == "") continue;

                            try {
                                string[] values = line.Split(',');
                                long elapsed = long.Parse(values[0]);

                                int diff = (int)(elapsed - (DateTimeOffset.Now.ToUnixTimeMilliseconds() - timestamp));
                                if (diff > 0) Thread.Sleep(diff);

                                DeviceEvent deviceEvent = (DeviceEvent)Enum.Parse(typeof(DeviceEvent), values[1]);
                    
                                if (deviceEvent.IsKeyEvent()) {
                                    Keys key = (Keys)Enum.Parse(typeof(Keys), values[2]);
                        
                                    if (deviceEvent == DeviceEvent.KeyDown) {
                                        if (!downKeys.Contains(key)) downKeys.Add(key);
                                        EventSimulator.KeyDown(key);
                                    } else if (deviceEvent == DeviceEvent.KeyUp) {
                                        if (downKeys.Contains(key)) downKeys.Remove(key);
                                        EventSimulator.KeyUp(key);
                                    } else if (deviceEvent == DeviceEvent.KeyPress) {
                                        if (downKeys.Contains(key)) downKeys.Remove(key);
                                        EventSimulator.KeyPress(key);
                                    }
                                } else if (deviceEvent.IsMouseEvent()) {
                                    int x = int.Parse(values[2]);
                                    int y = int.Parse(values[3]);
                        
                                    if (deviceEvent == DeviceEvent.MouseMove)
                                        EventSimulator.Point(new Point(x, y), true);
                                    else if (deviceEvent == DeviceEvent.MouseWheel)
                                        EventSimulator.Wheel(y);
                                    else if (deviceEvent == DeviceEvent.MouseTilt)
                                        EventSimulator.Tilt(x);
                                }
                            } catch {
                                Notify($"Failed to parse line {lineNo}.\n{path}: {line}");
                                break;
                            }

                            lineNo += 1;
                        }
                    }
            
                    // Release all pressed keys.
                    foreach (Keys key in downKeys) EventSimulator.KeyUp(key);

                    lock (playerLocker) {
                        if (players.Contains(path)) {
                            Notify($"Finished playing {path}");
                            players.Remove(path);
                        }
                    }
                }, forSystem:true
            );
            EventDispatcher.KeyPress(
                String.Format(eventName, "stop"), stopKeys, (e) => {
                    // Do not raise exceptions in this callback.
                    lock (playerLocker) {
                        if (players.Contains(path)) {
                            Notify($"Stopped playing {path}");
                            players.Remove(path);
                        }
                    }
                }, forSystem:true
            );
        }
    }
}
