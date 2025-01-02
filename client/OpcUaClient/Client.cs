using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;

namespace OpcUaClient
{
    /// <summary>
    /// Represents an OPC UA client that connects to an OPC UA server, browses server nodes,
    /// and inserts data into a database.
    /// </summary>
    public class Client
    {
        private readonly Session _session;
        private readonly string _connectionString;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the Client class.
        /// </summary>
        /// <param name="session">The OPC UA session used to interact with the server</param>
        /// <param name="connectionString">The connection string to the SQL database</param>
        /// <param name="logger">The logger instance for logging messages</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Client(Session session, string connectionString, ILogger logger)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /* -------------------- Public Methods -------------------- */

        /// <summary>
        /// Browses the server nodes starting from the given node ID and retrieves child nodes.
        /// </summary>
        /// <param name="startNodeId">The starting node ID to begin the browse operation</param>
        /// <param name="verbose">Indicates whether verbose logging is enabled</param>
        /// <returns>A list of reference descriptions representing the child nodes</returns>
        public List<ReferenceDescription> GetServerChildNodes(NodeId startNodeId, bool verbose = false)
        {
            List<ReferenceDescription> nodes = new List<ReferenceDescription>();

            try
            {
                _session.Browse(
                    null,                                               // RequestHeader: default null
                    null,                                               // ViewDescription: default null
                    startNodeId,                                        // Starting node
                    0,                                                  // maxResultsToReturn
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HierarchicalReferences,            // Browsing references type
                    false,                                              // Whether to include subtypes
                    (uint)NodeClass.Object | (uint)NodeClass.Variable,  // NodeClassMask: Object and Variable nodes
                    out _,                                              // Continuation Point
                    out ReferenceDescriptionCollection references       // Browsed result
                );

                if (verbose)
                {
                    _logger.LogInformation("Browsed Nodes:");
                }

                foreach (var reference in references)
                {
                    if (reference.NodeId.NamespaceIndex == 0)
                    {
                        continue; // Skip nodes in the default namespace
                    }

                    if (verbose)
                    {
                        _logger.LogInformation("NodeId: {NodeId} BrowseName: {BrowseName} DisplayName: {DisplayName}",
                            reference.NodeId, reference.BrowseName, reference.DisplayName);
                    }

                    BrowseChildNodes((NodeId)reference.NodeId, nodes, verbose);
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation("Error during browsing: {ErrorMessage}", e.Message);
            }

            return nodes;
        }

        /// <summary>
        /// Inserts data into the database for a given display name and value.
        /// </summary>
        /// <param name="displayName">The display name of the data to insert</param>
        /// <param name="value">The value associated with the display name</param>
        public void InsertIntoDB(string displayName, string? value)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = @"INSERT 
                                INTO OpcUaData (DisplayName, Value, Timestamp) 
                                VALUES (@DisplayName, @Value, @Timestamp)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DisplayName", displayName);
                    command.Parameters.AddWithValue("@Value", value ?? "");
                    command.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);

                    command.ExecuteNonQuery();
                }

                _logger.LogInformation("Data inserted into DB: {DisplayName} = {Value}", displayName, value);
            }
            catch (Exception e)
            {
                _logger.LogError("Error inserting data into DB: {ErrorMessage}", e.Message);
            }
        }

        /* -------------------- Private Methods -------------------- */

        /// <summary>
        /// Browses the child nodes of a parent node and adds them to the list of nodes.
        /// </summary>
        /// <param name="parentNodeId">The parent node ID from which to browse child nodes</param>
        /// <param name="nodes">The list of nodes to add the child nodes to</param>
        /// <param name="verbose">Indicates whether verbose logging is enabled</param>
        private void BrowseChildNodes(NodeId parentNodeId, List<ReferenceDescription> nodes, bool verbose)
        {
            try
            {
                _session.Browse(
                    null,                                               // RequestHeader: default null
                    null,                                               // ViewDescription: default null
                    parentNodeId,                                       // Starting node: parent node
                    0,                                                  // maxResultsToReturn
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HierarchicalReferences,            // Browsing references type
                    true,                                               // Whether to include subtypes
                    (uint)NodeClass.Object | (uint)NodeClass.Variable,  // NodeClassMask: Object and Variable nodes
                    out _,                                              // Continuation Point
                    out ReferenceDescriptionCollection childReferences  // Browsed child nodes
                );

                foreach (var childReference in childReferences)
                {
                    if (verbose)
                    {
                        _logger.LogInformation("\tChildNodeId: {NodeId} BrowseName: {BrowseName} DisplayName: {DisplayName}",
                            childReference.NodeId, childReference.BrowseName, childReference.DisplayName);
                    }

                    nodes.Add(childReference);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error inserting data into DB: {ErrorMessage}", e.Message);
            }
        }
    }
}
