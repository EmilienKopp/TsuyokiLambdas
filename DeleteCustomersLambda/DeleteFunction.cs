using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Lambdas.DataModel;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DeleteCustomersLambda
{
    public class DeleteFunction
    {
        public static AmazonDynamoDBClient dbClient = new AmazonDynamoDBClient();
        private readonly static string _TABLE_NAME_ = Environment.GetEnvironmentVariable("TABLE_NAME");
        private readonly static DynamoDBOperationConfig config = new DynamoDBOperationConfig() { OverrideTableName = _TABLE_NAME_ };
        private DynamoDBContext dbContext = new DynamoDBContext(dbClient, config);
        /// <summary>
        /// A lambda function to SOFT DELETE (flag for no-display) one or several item from the DB
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
        {
            //Read the Body from the Request - Typically a List<Customer>
            var customers = JsonConvert.DeserializeObject<List<Customer>>(request.Body);
            LambdaLogger.Log($"Created the following object: {JsonConvert.SerializeObject(customers)}");

            //Return BadRequest if the Customer Object could not be deserialized (the request content had the wrong format)
            if (customers == null)
            {
                LambdaLogger.Log($"The Customer object was null or couldn't be deserialized.");
                return new APIGatewayProxyResponse { StatusCode = 400 };
            }
            else
            {
                //Set the LastUpdated date
                TimeSpan offset = new TimeSpan(09, 00, 00);
                TimeZoneInfo tokyoTZ = TimeZoneInfo.CreateCustomTimeZone("Tokyo Standard Time", offset, "Tokyo Time", "(UTC+09:00)Tokyo Standard Time");
                DateTime tokyoNow = TimeZoneInfo.ConvertTime(DateTime.Now, tokyoTZ);
                string dateTimeNow = tokyoNow.ToString("yyyy/MM/dd HH:mm");
                //DateTime tokyoNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Tokyo Standard Time");
                foreach (Customer c in customers)
                {
                    c.LastUpdated = dateTimeNow;
                    c.Status = "SoftDeleted";
                }
            }

            //Create a BatchWrite object
            var customerBatch = dbContext.CreateBatchWrite<Customer>(config);
            customerBatch.AddPutItems(customers);
            LambdaLogger.Log($"Created the following BatchWrite Object: {customerBatch}");



            try
            {
                await customerBatch.ExecuteAsync();
            }
            catch (AmazonDynamoDBException e) { LambdaLogger.Log("AmazonDynamoDB says : " + e.Message + " from " + e.TargetSite); }
            catch (AmazonServiceException e) { LambdaLogger.Log("AmazonService says: " + e.Message + " from " + e.TargetSite); }
            catch (Exception e) { LambdaLogger.Log("Other problem saying: " + e.Message + " from " + e.TargetSite); }

            return new APIGatewayProxyResponse
            {
                StatusCode = 204,
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
