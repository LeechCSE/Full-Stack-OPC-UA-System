using Microsoft.Data.SqlClient;
using WebDashboard.Models;

namespace WebDashboard.Services
{
    /// <summary>
    /// Provides services for interacting with the database, including retrieving water pump data.
    /// </summary>
    public class DatabaseService
    {
        /// <summary>
        /// The connection string used to connect to the database.
        /// </summary>
        private readonly string? _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseService"/> class.
        /// Retrieves the connection string from the application's configuration.
        /// Throws an exception if the connection string is missing or invalid.
        /// </summary>
        /// <param name="configuration">The configuration object containing application settings.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the connection string "OpcUaDbConnection" is missing from appsettings.json.
        /// </exception>
        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OpcUaDbConnection");
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ArgumentException("Connection string 'OpcUaDbConnection' is missing from appsettings.json.");
            }
        }

        /// <summary>
        /// Retrieves the latest water pump data from the database.
        /// The data is ordered by timestamp in descending order.
        /// </summary>
        /// <param name="count">The maximum number of records to retrieve. Defaults to the maximum integer value.</param>
        /// <returns>A list of <see cref="WaterPumpModel"/> containing the latest water pump data.</returns>
        public async Task<List<WaterPumpModel>> GetLatestData(int count = int.MaxValue)
        {
            List<WaterPumpModel> result = new List<WaterPumpModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT TOP (@Count) Id, DisplayName, Value, Timestamp
                    FROM OpcUaData
                    ORDER BY Timestamp DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Count", count);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var model = new WaterPumpModel
                            {
                                Id = reader.GetInt64(0),
                                DisplayName = reader.GetString(1),
                                Value = reader.GetString(2),
                                Timestamp = reader.GetDateTime(3)
                            };

                            model.Value = model.GetSerializedValue() ?? "0.0";
                            result.Add(model);
                        }
                    }
                }
            }

            return result;
        }
    }
}
