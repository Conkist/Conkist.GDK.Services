using System;
using System.Collections.Generic;

namespace Conkist.GDK.Services
{
    /// <summary>
    /// Represents a single telemetry event to be sent to the Conkist API.
    /// Mirrors the TelemetryEventDto contract from the Conkist API.
    /// </summary>
    [Serializable]
    public class TelemetryEvent
    {
        /// <summary>
        /// The name/type of the event (e.g., "game_started", "quiz_answered").
        /// Must match an approved event name in the project's governance dictionary.
        /// </summary>
        public string eventName;

        /// <summary>
        /// ISO 8601 UTC timestamp of when the event occurred.
        /// </summary>
        public string timestamp;

        /// <summary>
        /// Arbitrary key-value properties associated with the event.
        /// </summary>
        public Dictionary<string, object> properties;

        public TelemetryEvent(string eventName, Dictionary<string, object> properties = null)
        {
            this.eventName = eventName;
            this.timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            this.properties = properties ?? new Dictionary<string, object>();
        }
    }
}
