using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Newtonsoft.Json;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Lambdas.DataModel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GetCustomersLambda
{
    public class GetFunction
    {
        public static AmazonDynamoDBClient dbClient = new AmazonDynamoDBClient();
        private readonly static string _TABLE_NAME_ = Environment.GetEnvironmentVariable("TABLE_NAME");
        private readonly static DynamoDBOperationConfig config = new DynamoDBOperationConfig() { OverrideTableName = _TABLE_NAME_  };
        private DynamoDBContext dbContext = new DynamoDBContext(dbClient, config);

        /// <summary>
        /// A simple function that reads customers from a DynamoDB Table. 
        /// 1) Reads the Query Parameters from API Gateway Lambda Proxy Integration.
        /// 2) Reads the condition (Equal,GreaterThan, etc... set to Equal by default) to perform the Query
        /// 3) Executes the Query on DynamoDB through the Object Persistence Model of .NET
        /// 
        /// </summary>
        /// <param name="request">APIGatewayProxyRequest</param>
        /// <param name="lambdaContext">Lambda execution context</param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
        {      
            IDictionary<string, string> queryParameters = request.QueryStringParameters;
            LambdaLogger.Log($"Query Parameters {JsonConvert.SerializeObject(queryParameters)} >>");
            List<Customer> customers = new List<Customer>();
            List<ScanCondition> conditions = new List<ScanCondition>();

            //Set a condition to retrieve only the items to display (Status = Active)
            ScanCondition statusCondition = new ScanCondition("Status", ScanOperator.Equal, "Active");
            conditions.Add(statusCondition);

            if (queryParameters != null && queryParameters.Count() > 0)
            {
                //Force display of NON ACTIVE with a very specific Query by clearing the ScanCondition on Status
                if (queryParameters.ContainsKey("Display") && queryParameters.TryGetValue("Display", out string displayValue) )
                {
                    if (displayValue == "Force") conditions.Remove(statusCondition);

                    //remove the Display key from the QueryString so it isn't searched for in the DB
                    queryParameters.Remove("Display");
                }


                ScanOperator scanOperator = new ScanOperator();

                if(queryParameters.ContainsKey("Condition") && queryParameters.TryGetValue("Condition", out string conditionValue))
                {
                    switch (conditionValue)
                    {
                        case "Greater": 
                            scanOperator = ScanOperator.GreaterThan;
                            break;
                        case "Less":
                            scanOperator = ScanOperator.LessThan;
                            break;
                        case "GreaterOrEqual":
                            scanOperator = ScanOperator.GreaterThanOrEqual;
                            break;
                        case "LessOrEqual":
                            scanOperator = ScanOperator.LessThanOrEqual;
                            break;
                        case "Contains":
                            scanOperator = ScanOperator.Contains;
                            break;
                        case "NotEqual":
                            scanOperator = ScanOperator.NotEqual;
                            break;
                        default:
                            scanOperator = ScanOperator.Equal;
                            break;
                    }//END switch
                    LambdaLogger.Log($"Scan Condition set to : {JsonConvert.SerializeObject(scanOperator)} ({conditionValue}) >>");
                }//end Condition check
                else
                {
                    scanOperator = ScanOperator.Equal;
                    LambdaLogger.Log($"Scan Condition set to : {JsonConvert.SerializeObject(scanOperator)} (Equal) >>");
                }            

                queryParameters.Remove("Condition");
                foreach (var pair in queryParameters)
                       conditions.Add(new ScanCondition(pair.Key, scanOperator, pair.Value));
            }//end queryParam null check

            LambdaLogger.Log($"Scan Conditions set to : {JsonConvert.SerializeObject(conditions)} .");

            //Try to Scan according to set Conditions
            try
            {
                AsyncSearch<Customer> searchResults = dbContext.ScanAsync<Customer>(conditions, config);
                customers = await searchResults.GetRemainingAsync();
                
            }//end try
            catch (AmazonDynamoDBException e) { LambdaLogger.Log("AmazonDynamoDB says : " + e.Message + " from " + e.TargetSite); customers.Clear(); }
            catch (AmazonServiceException e) { LambdaLogger.Log("AmazonService says: " + e.Message + " from " + e.TargetSite); customers.Clear(); }
            catch (Exception e) { LambdaLogger.Log("Other problem saying: " + e.Message + " from " + e.TargetSite); customers.Clear(); }

            LambdaLogger.Log($"Created Customer List with {customers.Count} items.");
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonConvert.SerializeObject(customers),
                Headers = new Dictionary<string, string>
                {
                    { "Access-Control-Allow-Headers", "Content-Type" },
                    { "Access-Control-Allow-Origin", "*" },
                    { "Access-Control-Allow-Methods", "OPTIONS,POST,GET" }
                }

            };//returned APIGatewayProxyResponse

        }//FunctionHandler

    }//Class

}//Namespace
