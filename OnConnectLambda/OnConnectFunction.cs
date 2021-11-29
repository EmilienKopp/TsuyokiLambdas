using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OnConnectLambda
{
    public class OnConnectFunction
    {
        public AmazonDynamoDBClient dbClient = new AmazonDynamoDBClient();
        private readonly static string _TABLE_NAME_ = Environment.GetEnvironmentVariable("TABLE_NAME");


        /// <summary>
        /// Inserts the Connection ID and API ID into DynamoDB to communicate with WebSocket Clients
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            //Read the Body from the Request
            var connectionID = request.RequestContext.ConnectionId;
            var stage = request.RequestContext.Stage;
            var domain = request.RequestContext.DomainName;

            string connectionURL = $"https://{domain}/{stage}/@connections/{connectionID}";

            PutItemRequest putRequest = new PutItemRequest
            {
                TableName = _TABLE_NAME_,
                Item = new Dictionary<string, AttributeValue>()
                  {
                    { "ConnectionId", new AttributeValue {S = connectionID } },
                    { "ConnectionURL", new AttributeValue { S = connectionURL} }
                  }
            };

            try
            {
                await dbClient.PutItemAsync(putRequest);
            }
            catch (AmazonDynamoDBException e) { LambdaLogger.Log("AmazonDynamoDB says : " + e.Message + " from " + e.TargetSite); }
            catch (AmazonServiceException e) { LambdaLogger.Log("AmazonService says: " + e.Message + " from " + e.TargetSite); }
            catch (Exception e) { LambdaLogger.Log("Other problem saying: " + e.Message + " from " + e.TargetSite); }

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = $"Successfully saved {connectionURL} in the database",
                Headers = new Dictionary<string, string>
                {
                    { "Access-Control-Allow-Headers", "Content-Type" },
                    { "Access-Control-Allow-Origin", "*" },
                    { "Access-Control-Allow-Methods", "OPTIONS,POST,GET" }
                }

            };
        }
    }
}
