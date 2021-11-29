using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Newtonsoft.Json;

namespace Lambdas.DataModel
{
    [DynamoDBTable("DUMMY")]//This TableName attribute is overwritten on calling DbContext methods to use LAMBDA Environment Variables to set it dynamically
    public class Customer
    {
        [DynamoDBHashKey]
        public string CustomerId { get; set; } = "";

        [DynamoDBProperty(AttributeName = "MemberId")]
        public string MemberId { get; set; } = "";

        [DynamoDBProperty(AttributeName = "Email")]
        public string Email { get; set; } = "";

        [DynamoDBProperty(AttributeName = "Name")]
        public string Name { get; set; } = "";

        [DynamoDBProperty(AttributeName = "Romaji")]
        public string Romaji { get; set; } = "";

        [DynamoDBProperty(AttributeName = "City")]
        public string City { get; set; } = "";

        [DynamoDBProperty(AttributeName = "Phone")]
        public string Phone { get; set; } = "";

        [DynamoDBProperty(AttributeName = "Address")]
        public string Address { get; set; } = "";

        [DynamoDBProperty(AttributeName = "MembershipType")]
        public string MembershipType { get; set; } = "";

        [DynamoDBProperty(AttributeName = "LastVisit")]
        public string LastVisit { get; set; } = "";

        [DynamoDBProperty(AttributeName = "LastUpdated")]
        public string LastUpdated { get; set; } = "";

        [DynamoDBProperty(AttributeName = "Status")]
        public string Status { get; set; } = "Active";

        [JsonConstructor]
        public Customer()
        { }

    }
}
