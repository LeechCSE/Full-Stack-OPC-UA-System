using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;

namespace OpcUaClient
{
    public class Program
    {
        private const int DataRetrievalInterval = 1000; // in ms
        private const string EndpointUrl = "opc.tcp://127.0.0.1:4840/opcua/server";
        private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=OPCUA DB;Trusted_Connection=True;";

        private static readonly ILogger _logger = LoggerFactory
            .Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "[HH:mm:ss] ";
                });
            })
            .CreateLogger("GlobalLogger");

        /// <summary>
        /// Creates and establishes a session with the OPC UA server.
        /// </summary>
        /// <returns>A Task that represents the asynchronous operation, with a Session object upon completion.</returns>
        private static async Task<Session> CreateSession()
        {
            try
            {
                var applicationConfiguration = new ApplicationConfiguration
                {
                    ApplicationName = "MyOpcUaClient",
                    ApplicationType = ApplicationType.Client,
                    ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 /* ms */ }
                };
                var endpointDescription = CoreClientUtils.SelectEndpoint(EndpointUrl, useSecurity: false);
                var endpointConfiguration = EndpointConfiguration.Create(applicationConfiguration);
                var configuredEndpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
                var session = await Session.Create(
                    applicationConfiguration,
                    configuredEndpoint,
                    false,              // Do not update before connecting
                    "MyTestSession",    // Session name
                    60000,              // Timeout in ms
                    null,               // No specific user identity
                    null);              // No specific configuration for session

                _logger.LogInformation("Connected to {EndpointURL}", EndpointUrl);

                return session;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to create session: {ErrorMessage}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Main entry point for the application. It creates a session and continuously retrieves and logs data from the OPC UA server.
        /// </summary>
        private static async Task Main()
        {
            Session? session = null;

            try
            {
                session = await CreateSession();
                Client opcUaClient = new Client(session, ConnectionString, _logger);

                while (true)
                {
                    var references = opcUaClient.GetServerChildNodes(ObjectIds.ObjectsFolder, verbose: true);

                    foreach (ReferenceDescription reference in references)
                    {
                        opcUaClient.InsertIntoDB(
                            reference.DisplayName.ToString(),
                            session.ReadValue((NodeId)reference.NodeId).Value.ToString());
                    }

                    await Task.Delay(DataRetrievalInterval);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error: {ErrorMessage}", e.Message);
            }
            finally
            {
                if (session != null)
                {
                    session.Close();
                    _logger.LogInformation("Session is closed.");
                }
            }
        }

    }
}
