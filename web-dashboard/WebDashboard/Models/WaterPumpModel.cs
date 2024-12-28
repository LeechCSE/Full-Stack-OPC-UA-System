namespace WebDashboard.Models
{
    /// <summary>
    /// Represents a model for water pump data, containing the pump's identifier, display name, value, and timestamp.
    /// </summary>
    public class WaterPumpModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the water pump data record.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the water pump, typically used for UI presentation.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the value associated with the water pump data (e.g., temperature, pressure, or setting).
        /// This value is stored as a string to handle various data types.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Gets or sets the timestamp indicating when the water pump data was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Attempts to parse and return the serialized value of the water pump data.
        /// If the value is a valid number (not NaN or Infinity), it returns the value as a string; otherwise, it returns null.
        /// </summary>
        /// <returns>
        /// The serialized value of the water pump data as a string, or null if the value is invalid.
        /// </returns>
        public string? GetSerializedValue()
        {
            if (double.TryParse(Value, out double numericValue))
            {
                if (double.IsNaN(numericValue) || double.IsInfinity(numericValue))
                {
                    return null;
                }
            }

            return Value;
        }
    }
}
