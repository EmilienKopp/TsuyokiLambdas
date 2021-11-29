using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OnDisconnectLambda
{
    public class OnDisconnectFunction
    {

        public AmazonDynamoDBClient dbClient = new AmazonDynamoDBClient();
        private readonly static string _TABLE_NAME_ = Environment.GetEnvironmentVariable("TABLE_NAME");

        /// <summary>
        /// A simple function that deletes WebSocket ConnectionIDs from the DB
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            //Read the Body from the Request
            var connectionID = request.RequestContext.ConnectionId;


            DeleteItemRequest deleteRequest = new DeleteItemRequest
            {
                TableName = _TABLE_NAME_,
                Key = new Dictionary<string, AttributeValue>()
                  {
                      { "ConnectionId", new AttributeValue { S = connectionID} }
                  }
            };

            try
            {
                await dbClient.DeleteItemAsync(deleteRequest);
            }
            catch (AmazonDynamoDBException e) { LambdaLogger.Log("AmazonDynamoDB says : " + e.Message + " from " + e.TargetSite); }
            catch (AmazonServiceException e) { LambdaLogger.Log("AmazonService says: " + e.Message + " from " + e.TargetSite); }
            catch (Exception e) { LambdaLogger.Log("Other problem saying: " + e.Message + " from " + e.TargetSite); }

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = $"Deleted {connectionID} from the database",
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

