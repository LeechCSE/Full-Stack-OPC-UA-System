using Microsoft.AspNetCore.Mvc;
using WebDashboard.Services;

namespace WebDashboard.Controllers
{
    /// <summary>
    /// Controller for handling water pump data operations, including retrieving, processing, and presenting data.
    /// </summary>
    public class WaterPumpDataController : Controller
    {
        private const int DataFetchCount = 3600 * 1; // Number of one-second data points.
        private const int DownsamplingSize = 5; // Number of seconds to average.
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaterPumpDataController"/> class.
        /// </summary>
        /// <param name="_databaseService">The service used to interact with the database for retrieving water pump data.</param>
        public WaterPumpDataController(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /* -------------------- Public Methods -------------------- */

        /// <summary>
        /// Renders the main dashboard view with processed water pump data.
        /// </summary>
        /// <returns>A <see cref="Task{IActionResult}"/> representing the asynchronous operation, with the view containing processed data.</returns>
        public async Task<IActionResult> Index() => View(await GetProcessedDataModel(DataFetchCount));

        /// <summary>
        /// Retrieves the latest water pump data as a JSON response.
        /// </summary>
        /// <returns>A <see cref="Task{IActionResult}"/> representing the asynchronous operation, with the JSON result of the latest data.</returns>
        [HttpGet("api/opcua/water-pump/latest")]
        public async Task<IActionResult> GetLatestData() => Json(await GetProcessedDataModel(DataFetchCount));

        /* -------------------- Private Methods -------------------- */

        /// <summary>
        /// Retrieves and processes water pump data into a structured model.
        /// </summary>
        /// <param name="count">The number of data points to fetch from the database.</param>
        /// <returns>A <see cref="Task{object}"/> representing the asynchronous operation, with an object containing processed data for display.</returns>
        private async Task<object> GetProcessedDataModel(int count)
        {
            List<WebDashboard.Models.WaterPumpModel> data = await _databaseService.GetLatestData(count);

            var temperatureData = DownsampleData(ProcessData(data, "temperature"), DownsamplingSize);
            var pressureData = DownsampleData(ProcessData(data, "pressure"), DownsamplingSize);
            var pumpsettingData = DownsampleData(ProcessData(data, "pumpsetting", false), DownsamplingSize, "majority");

            return new
            {
                TemperatureData = temperatureData,
                PressureData = pressureData,
                PumpSettingData = pumpsettingData
            };
        }

        /// <summary>
        /// Processes raw water pump data based on the specified display name.
        /// </summary>
        /// <param name="data">The raw data to be processed.</param>
        /// <param name="displayName">The display name used to filter the data.</param>
        /// <param name="roundValues">A flag indicating whether the numeric values should be rounded to three decimal places. Defaults to true.</param>
        /// <returns>A list of processed water pump models.</returns>
        private static List<WebDashboard.Models.WaterPumpModel> ProcessData(List<WebDashboard.Models.WaterPumpModel> data, string displayName, bool roundValues = true)
        {
            return data
                .Where(d => d.DisplayName == displayName)
                .Select(d => new WebDashboard.Models.WaterPumpModel
                {
                    Id = d.Id,
                    Timestamp = d.Timestamp,
                    DisplayName = d.DisplayName,
                    Value = roundValues ? Math.Round(double.Parse(d.Value ?? "0.0"), 3).ToString() : d.Value
                })
                .ToList();
        }

        /// <summary>
        /// Downsamples the data by averaging or taking the majority value for each interval.
        /// </summary>
        /// <param name="data">The data to be downsampled.</param>
        /// <param name="interval">The interval (in number of data points) for downsampling.</param>
        /// <param name="averageOrMajority">Specifies whether to use "average" for numerical values or "majority" for string values.</param>
        /// <returns>A list of downsampled water pump models.</returns>
        /// <exception cref="ArgumentException">Thrown when an invalid value is passed for the 'averageOrMajority' parameter.</exception>
        private static List<WebDashboard.Models.WaterPumpModel> DownsampleData(List<WebDashboard.Models.WaterPumpModel> data, int interval, string averageOrMajority = "average")
        {
            List<WebDashboard.Models.WaterPumpModel> downsampled = new List<WebDashboard.Models.WaterPumpModel>();

            for (int i = 0; i < data.Count; i += interval)
            {
                var dataSlice = data.Skip(i).Take(interval).ToList();

                if (averageOrMajority == "average")
                {
                    var averageValue = dataSlice
                        .Where(d => double.TryParse(d.Value, out _))
                        .Average(d => double.Parse(d.Value ?? "0.0"));

                    downsampled.Add(new WebDashboard.Models.WaterPumpModel
                    {
                        Timestamp = dataSlice.Last().Timestamp,
                        DisplayName = dataSlice.First().DisplayName,
                        Value = Math.Round(averageValue, 3).ToString()
                    });
                }
                else if (averageOrMajority == "majority")
                {
                    var majorityValue = dataSlice
                        .GroupBy(d => d.Value)
                        .OrderByDescending(g => g.Count())
                        .First().Key;

                    downsampled.Add(new WebDashboard.Models.WaterPumpModel
                    {
                        Timestamp = dataSlice.Last().Timestamp,
                        DisplayName = dataSlice.First().DisplayName,
                        Value = majorityValue
                    });
                }
                else
                {
                    throw new ArgumentException("Invalid value for 'averageOrMajority'. Expected 'average' or 'majority'.");
                }
            }

            return downsampled;
        }
    }
}
