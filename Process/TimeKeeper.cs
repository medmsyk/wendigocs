using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Process {
    public static class TimeKeeper {
        // Timer threads
        private static Dictionary<string, Thread> threads = new Dictionary<string, Thread>();
        private static object threadLock = new object();

        /// <summary>
        /// Listen for timer event.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="interval">Interval in milliseconds.</param>
        public static void Listen(string name, Action eventHandler, int interval) {
            lock (threadLock) {
                if (threads.ContainsKey(name)) {
                    throw new InvalidOperationException($"Already listening to {name}");
                }

                Action wrapper = () => {
                    // Run synchronously.
                    while (true) {
                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        eventHandler();

                        sw.Stop();

                        int diff = interval - (int)sw.ElapsedMilliseconds;
                        if (diff > 0) Thread.Sleep(diff);
                    }
                };

                Thread thread = new Thread(new ThreadStart(wrapper));
                thread.Start();
                threads.Add(name, thread);
            }
        }

        /// <summary>
        /// Unlisten for timer event.
        /// </summary>
        /// <param name="name">Name.</param>
        public static void Unlisten(string name) {
            lock (threadLock) {
                if (threads.ContainsKey(name)) {
                    threads[name].Abort();
                    threads.Remove(name);
                }
            }
        }
    }
}
