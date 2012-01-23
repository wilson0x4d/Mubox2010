using System;
using System.Runtime.Serialization;

namespace Mubox.Model.Input
{
    /// <summary>
    /// State Information for a Button (e.g. a Mouse Button)
    /// </summary>
    [DataContract]
    public class ButtonState
    {
        /// <summary>
        /// True if the button is currently down.
        /// </summary>
        public bool IsDown { get { return LastDownTimestamp.Ticks <= LastUpTimestamp.Ticks; } }

        /// <summary>
        /// Timestamp of the last time IsDown transitioned into a True state from a False state.
        /// </summary>
        public DateTime LastDownTimestamp { get; set; }

        /// <summary>
        /// Timestamp of the last time IsDown transitioned into a False state from a True state.
        /// </summary>
        public DateTime LastUpTimestamp { get; set; }

        /// <summary>
        /// True if the button transitioned from a Down state to an Up state, as an 'atomic' "Click" gesture.
        /// </summary>
        public bool IsClick { get { return LastUpTimestamp.Ticks <= LastDownTimestamp.AddMilliseconds(Mubox.Configuration.MuboxConfigSection.Default.MouseBufferMilliseconds).Ticks; } }

        /// <summary>
        /// True if the button is Down due to a Multicast (e.g. Mouse Clone, Key Clone).
        /// </summary>
        public bool IsMulticast { get; set; }

        /// <summary>
        /// True if IsClick was True when the current gesture began, necessary for implementing an 'atomic' "Double Click" gesture.
        /// </summary>
        public bool LastGestureWasClick { get; set; }
    }
}