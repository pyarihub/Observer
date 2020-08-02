using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using MapService;
using Microsoft.Extensions.Options;

namespace Amazon.Geo
{
    public sealed class GeoDataManagerConfiguration
    {
        // Public constants
        public const long MergeThreshold = 2;

        // Default values
        private const string DefaultHashkeyAttributeName = "hashKey";
        private const string DefaultRangekeyAttributeName = "rangeKey";
        private const string DefaultGeohashAttributeName = "geohash";
        private const string DefaultGeojsonAttributeName = "geoJson";
        private const string DefaultGeohashIndexAttributeName = "geohash-index";

        private const int DefaultHashkeyLength = 6;


        public GeoDataManagerConfiguration(IOptionsMonitor<AmazonSettings> amazonSettings)
        {
            HashKeyAttributeName = DefaultHashkeyAttributeName;
            RangeKeyAttributeName = DefaultRangekeyAttributeName;
            GeohashAttributeName = DefaultGeohashAttributeName;
            GeoJsonAttributeName = DefaultGeojsonAttributeName;
            GeohashIndexName = DefaultGeohashIndexAttributeName;
            HashKeyLength = DefaultHashkeyLength;

            var settings = amazonSettings.CurrentValue;
            var region = RegionEndpoint.GetBySystemName(settings.regionName);
            var config = new AmazonDynamoDBConfig { MaxErrorRetry = 20, RegionEndpoint = region };
            var creds = new BasicAWSCredentials(settings.accessKey, settings.secretKey);

            DynamoDBClient = new AmazonDynamoDBClient(creds, config); 
            TableName = settings.tableName;
        }

        public string TableName { get; set; }
        public string HashKeyAttributeName { get; set; }
        public string RangeKeyAttributeName { get; set; }
        public string GeohashAttributeName { get; set; }
        public string GeoJsonAttributeName { get; set; }
        public string GeohashIndexName { get; set; }
        public int HashKeyLength { get; set; }
        public AmazonDynamoDBClient DynamoDBClient { get; set; }
    }
}