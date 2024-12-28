using Microsoft.AspNetCore.Mvc;
using WebDashboard.Services;

namespace WebDashboard.Controllers
{
    /// <summary>
    /// Handles water pump data operations, including data retrieval, processing, and insertion.
    /// </summary>
    public class WaterPumpDataController(DatabaseService databaseService) : Controller
    {
        private readonly DatabaseService _databaseService = databaseService;
        private const int DataFetchCount = 86400;

        /* -------------------- Public Methods -------------------- */

        /// <summary>
        /// Renders the main dashboard view with processed water pump data.
        /// </summary>
        /// <returns>ViewResult for the dashboard page.</returns>
        public async Task<IActionResult> Index() => View(await GetProcessedDataModel(DataFetchCount));

        /// <summary>
        /// Provides the latest water pump data as a JSON response (500 latest data points by default).
        /// </summary>
        /// <returns>JSON object containing processed water pump data.</returns>
        [HttpGet("api/opcua/water-pump/latest")]
        public async Task<IActionResult> GetLatestData() => Json(await GetProcessedDataModel(DataFetchCount));

        /* -------------------- Private Methods -------------------- */

        /// <summary>
        /// Retrieves and processes water pump data into a structured model.
        /// </summary>
        /// <param name="count">The number of data records to fetch from the database.</param>
        /// <returns>An object containing structured data for the dashboard.</returns>
        private async Task<object> GetProcessedDataModel(int count)
        {
            List<WebDashboard.Models.WaterPumpModel> data = await _databaseService.GetLatestData(count);

            var temperatureData = DownsampleData(ProcessData(data, "temperature", true), 30);
            var pressureData = DownsampleData(ProcessData(data, "pressure", true), 30);
            var pumpsettingData = DownsampleData(ProcessData(data, "pumpsetting", false), 30);

            return new
            {
                TemperatureData = temperatureData,
                PressureData = pressureData,
                PumpSettingData = pumpsettingData
            };
        }

        /// <summary>
        /// Filters and formats water pump data for a specific type (e.g. temperature, pressure).
        /// </summary>
        /// <param name="data">The raw water pump data.</param>
        /// <param name="displayName">The data type to filter (e.g. "temperature").</param>
        /// <param name="roundValues">Indicates whether to round numeric values to 3 decimal places.</param>
        /// <returns>A list of formatted data objects.</returns>
        private static List<object> ProcessData(List<WebDashboard.Models.WaterPumpModel> data, string displayName, bool roundValues)
        {
            return data
                .Where(d => d.DisplayName == displayName)
                .Select(d => (object)new
                {
                    d.Id,
                    Timestamp = d.Timestamp.ToString(),
                    Value = roundValues ? (object)Math.Round(double.Parse(d.Value ?? "0.0"), 3) : d.Value
                })
                .ToList();
        }

        /// <summary>
        /// Downsamples the data to a specified interval.
        /// </summary>
        /// <param name="data">The data to downsample.</param>
        /// <param name="interval">The interval for grouping data points.</param>
        /// <returns>Downsampled data list.</returns>
        private static List<object> DownsampleData(List<object> data, int interval)
        {
            var downsampled = new List<object>();
            for (int i = 0; i < data.Count; i += interval)
            {
                downsampled.Add(data[i]);
            }
            return downsampled;
        }
    }
}