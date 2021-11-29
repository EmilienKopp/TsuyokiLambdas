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

namespace PostCustomerLambda
{
    
    public class PostFunction
    {
        public static AmazonDynamoDBClient dbClient = new AmazonDynamoDBClient();
        private readonly static string _TABLE_NAME_ = Environment.GetEnvironmentVariable("TABLE_NAME");
        private readonly static DynamoDBOperationConfig config = new DynamoDBOperationConfig() { OverrideTableName = _TABLE_NAME_ };
        private DynamoDBContext dbContext = new DynamoDBContext(dbClient, config);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
        {
            //Read the Body from the Request
            var customer = JsonConvert.DeserializeObject<Customer>(request.Body);
            LambdaLogger.Log($"Created the following object: {JsonConvert.SerializeObject(customer)}");

            //Return BadRequest if the Customer Object could not be deserialized (the request content had the wrong format)
            if (customer == null)
            {
                LambdaLogger.Log($"The Customer object was null or couldn't be deserialized.");
                return new APIGatewayProxyResponse { StatusCode = 400 };
            }

            //Check and assign Unique ID
            List<ScanCondition> conditions = new List<ScanCondition>();
            AsyncSearch<Customer> searchResults = dbContext.ScanAsync<Customer>(conditions, config);
            List<Customer> customers = await searchResults.GetRemainingAsync();
            int maxId = Int16.Parse(customers.AsQueryable().Select(c => c.CustomerId).AsQueryable().Max());
            customer.CustomerId = (maxId+1).ToString();
            LambdaLogger.Log($"Customer object to be saved with CustomerId = {customer.CustomerId}");

            //Set LastUpdated Date
            TimeSpan offset = new TimeSpan(09, 00, 00);
            TimeZoneInfo tokyoTZ = TimeZoneInfo.CreateCustomTimeZone("Tokyo Standard Time", offset, "Tokyo Time", "(UTC+09:00)Tokyo Standard Time");
            DateTime tokyoNow = TimeZoneInfo.ConvertTime(DateTime.Now, tokyoTZ);
            string dateTimeNow = tokyoNow.ToString("yyyy/MM/dd HH:mm");
            customer.LastUpdated = dateTimeNow;
            customer.Status = "Active";
            try
            {
                await dbContext.SaveAsync<Customer>(customer, config);
            }
            catch (AmazonDynamoDBException e) { LambdaLogger.Log("AmazonDynamoDB says : " + e.Message + " from " + e.TargetSite);  }
            catch (AmazonServiceException e) { LambdaLogger.Log("AmazonService says: " + e.Message + " from " + e.TargetSite); }
            catch (Exception e) { LambdaLogger.Log("Other problem saying: " + e.Message + " from " + e.TargetSite);  }

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonConvert.SerializeObject(customer),
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
