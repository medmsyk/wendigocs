using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Device.Event {
    public class EventListener {
        private Keys[] keys;
        private bool keysEnabled;

        private DeviceEvent deviceEvent;
        private event DeviceEventHandler eventHandler;

        /// <summary>
        /// Initialize for key event.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <param name="deviceEvent">Device event.</param>
        /// <param name="eventHandler">Event handler.</param>
        public EventListener(Keys[] keys, DeviceEvent deviceEvent, DeviceEventHandler eventHandler) {
            this.keys = keys;
            this.deviceEvent = deviceEvent;
            this.eventHandler += eventHandler;
            this.keysEnabled = deviceEvent == DeviceEvent.KeyPress;
        }

        /// <summary>
        /// Initialize for mouse event.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <param name="eventHandler">Event handler.</param>
        public EventListener(DeviceEvent deviceEvent, DeviceEventHandler eventHandler) {
            this.deviceEvent = deviceEvent;
            this.eventHandler += eventHandler;
        }

        /// <summary>
        /// Listen for device event.
        /// </summary>
        /// <param name="state">Device state.</param>
        /// <param name="oldState">Old device state.</param>
        private void Listen(DeviceState state, DeviceState oldState) {
            if (deviceEvent.IsKeyEvent())
                ListenKeyEvent(deviceEvent, state, oldState);
            else if (deviceEvent.IsMouseEvent())
                ListenMouseEvent(deviceEvent, state, oldState);
        }

        /// <summary>
        /// Listen for key event.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <param name="state">Device state.</param>
        /// <param name="oldState">Old device state.</param>
        private void ListenKeyEvent(DeviceEvent deviceEvent, DeviceState state, DeviceState oldState) {
            if (CheckKeys(keys, state.Key.Keys, deviceEvent)) {
                state.DeviceEvent = deviceEvent;
                InvokeEvent(state);
            }
        }

        /// <summary>
        /// Listen for mouse event.
        /// </summary>
        /// <param name="deviceEvent">Device event.</param>
        /// <param name="state">Device state.</param>
        /// <param name="oldState">Old device state.</param>
        private void ListenMouseEvent(DeviceEvent deviceEvent, DeviceState state, DeviceState oldState) {
            if (state.DeviceEvent == deviceEvent)
                InvokeEvent(state);
        }

        /// <summary>
        /// Check if keys are used or not.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <param name="state">Device state.</param>
        /// <param name="deviceEvent">Device event.</param>
        /// <returns>Keys are used or not.</returns>
        private bool CheckKeys(Keys[] keys, Dictionary<Keys, bool> state, DeviceEvent deviceEvent) {
            int isPressed = 0;

            foreach (Keys key in keys) {
                if (state.ContainsKey(key) && state[key]) isPressed++;
            }
            
            switch (deviceEvent) {
                case DeviceEvent.KeyDown:
                    if (isPressed == keys.Length) {
                        return true;
                    }
                    break;
                case DeviceEvent.KeyPress:
                    if (isPressed == keys.Length) {
                        if (keysEnabled) {
                            keysEnabled = false;
                            return true;
                        }
                    } else if (isPressed == 0) {
                        keysEnabled = true;
                    }
                    break;
                case DeviceEvent.KeyUp:
                    if (isPressed == keys.Length) {
                        keysEnabled = true;
                    } else if (isPressed == 0) {
                        if (keysEnabled) {
                            keysEnabled = false;
                            return true;
                        }
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Dispatch device events.
        /// </summary>
        /// <param name="state">Device state.</param>
        /// <param name="oldState">Old device state.</param>
        public async void Dispatch(DeviceState state, DeviceState oldState) {
            await Task.Run(() => Listen(state, oldState));
        }

        /// <summary>
        /// Invoke device event.
        /// </summary>
        /// <param name="state">Device state.</param>
        private void InvokeEvent(DeviceState state) {
            eventHandler(new DeviceEventArgs(state));
        }
    }
}
