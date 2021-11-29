using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System.Net.WebSockets;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using AwsSignatureVersion4;
using Lambdas.DataModel;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace NotificationHubLambda
{
    public class NotificationFunction
    {


        //Information for the WebSocket Connections Table
        public static AmazonDynamoDBClient connectionsDbClient = new AmazonDynamoDBClient();
        private readonly static string _CONNECTIONS_TABLE_ = Environment.GetEnvironmentVariable("CONNECTIONS_TABLE");
        private readonly static DynamoDBOperationConfig connectionsDbConfig = new DynamoDBOperationConfig() { OverrideTableName = _CONNECTIONS_TABLE_ };
        private DynamoDBContext connectionDbContext = new DynamoDBContext(connectionsDbClient, connectionsDbConfig);

        //Information for the Customers Table
        public static AmazonDynamoDBClient customersDbClient = new AmazonDynamoDBClient();
        private readonly static string _CUSTOMERS_TABLE_ = Environment.GetEnvironmentVariable("CUSTOMERS_TABLE");
        private readonly static DynamoDBOperationConfig customersDbConfig = new DynamoDBOperationConfig() { OverrideTableName = _CUSTOMERS_TABLE_ };
        private DynamoDBContext customersdbContext = new DynamoDBContext(customersDbClient, customersDbConfig);


        private byte[] buffer = new byte[1204 * 4];
        private string timeNow = GetTokyoTime();
        private string message = "Invalid Information";
        private string customerId = "";
        private List<ConnectionInfo> connections = new List<ConnectionInfo>();
        private List<ScanCondition> conditions = new List<ScanCondition>();
        private ClientWebSocket webSocket = new ClientWebSocket();
        private HttpClient httpClient = new HttpClient();
        private ImmutableCredentials credentials = new ImmutableCredentials(Environment.GetEnvironmentVariable("ACCESS_KEY"), Environment.GetEnvironmentVariable("SECRET_KEY"), null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            
            //Get the message from the URL Query String
            if (request.QueryStringParameters != null)
            {
                request.QueryStringParameters.TryGetValue("customerid", out customerId);
            }

            try
            {
                //Retrieve Customer Information from the CUSTOMERS_TABLE with information received in the Query String
                Customer customerData = await customersdbContext.LoadAsync<Customer>(customerId, customersDbConfig);
                string customerName = customerData.Name == null ? "UNKNOWN" : customerData.Name;
                message = $"Customer {customerName} has arrived.";
            }
            catch (Exception e)
            {
                LambdaLogger.Log("AmazonDynamoDB says : " + e.Message + " from " + e.TargetSite);
                message = "UNKNOWN CUSTOMER has arrived.";
            }


            try
            {
                //Retrieve the list of CLIENTS (ConnectionIDs) from the DynamoDB
                AsyncSearch<ConnectionInfo> searchResults = connectionDbContext.ScanAsync<ConnectionInfo>(conditions, connectionsDbConfig);
                connections = await searchResults.GetRemainingAsync();
                
                //Broadcast message (HTTP POST) 
                foreach (ConnectionInfo connection in connections)
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage();
                    requestMessage.RequestUri = new Uri(connection.ConnectionURL);
                    requestMessage.Content = new StringContent(message);
                    requestMessage.Method = HttpMethod.Post;

                    await httpClient.SendAsync(requestMessage,
                                            regionName: "ap-northeast-1",
                                            serviceName: "execute-api",
                                            credentials);
                }


            }//end try
            catch (AmazonDynamoDBException e) { LambdaLogger.Log("AmazonDynamoDB says : " + e.Message + " from " + e.TargetSite); connections.Clear(); }
            catch (AmazonServiceException e) { LambdaLogger.Log("AmazonService says: " + e.Message + " from " + e.TargetSite); connections.Clear(); }
            catch (Exception e) { LambdaLogger.Log("Other problem saying: " + e.Message + " from " + e.TargetSite); connections.Clear(); }

            


            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                //BODY
                Headers = new Dictionary<string, string>
                {
                    { "Access-Control-Allow-Headers", "Content-Type" },
                    { "Access-Control-Allow-Origin", "*" },
                    { "Access-Control-Allow-Methods", "OPTIONS,POST,GET" }
                }

            };
        }//FunctionHandler

        private static string GetTokyoTime()
        {
            TimeSpan offset = new TimeSpan(09, 00, 00);
            TimeZoneInfo tokyoTZ = TimeZoneInfo.CreateCustomTimeZone("Tokyo Standard Time", offset, "Tokyo Time", "(UTC+09:00)Tokyo Standard Time");
            DateTime tokyoNow = TimeZoneInfo.ConvertTime(DateTime.Now, tokyoTZ);
            return tokyoNow.ToString("yyyy/MM/dd HH:mm");
        }

    }//Class NotificationFunction


    [DynamoDBTable("DUMMY")]
    public class ConnectionInfo
    {
        [DynamoDBHashKey]
        public string ConnectionId { get; set; }

        [DynamoDBProperty("ConnectionURL")]
        public string ConnectionURL { get; set; }

    }
}
