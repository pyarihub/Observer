using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Geo;
using Amazon.Geo.Model;
using Amazon.Geo.Util;
using Amazon.Runtime;
using DynamoDB_Geo.Interfaces;

namespace SampleServer
{
    public class Utilities
    {
        private Utilities()
        {
            Status = Status.NotStarted;
        }

        public static readonly Utilities Instance = new Utilities();
        public Status Status { get; private set; }
        public async Task StartLoadData(IGeoService manager)
        {
            Status = Status.CreatingTable;
            //var config = manager.GeoDataManagerConfiguration;
            //var ctr = GeoTableUtil.GetCreateTableRequest(config);
            //await config.DynamoDBClient.CreateTableAsync(ctr);
            //await WaitForTableToBeReady(manager);
            await InsertData(manager);
        }
        private async Task InsertData(IGeoService dm)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data\\point_list.txt");

            foreach (var line in File.ReadLines(path))
            {
                var columns = line.Split('\t');
                var vendorid = columns[0];

                var st = columns[1];
                var et = columns[2];
                var fromLatitude = double.Parse(columns[4], CultureInfo.InvariantCulture);
                var fromLongitude = double.Parse(columns[3], CultureInfo.InvariantCulture);

                var toLatitude = double.Parse(columns[6], CultureInfo.InvariantCulture);
                var toLongitude = double.Parse(columns[5], CultureInfo.InvariantCulture);

                var fromPoint = new GeoPoint(fromLatitude, fromLongitude);
                var toPoint = new GeoPoint(toLatitude, toLongitude);
                var rangeKeyVal = new AttributeValue {S = vendorid};
                var req = new PutPointRequest(fromPoint, toPoint);
                req.PutItemRequest.Item["startTime"] = new AttributeValue { S = st };
                req.PutItemRequest.Item["endTime"] = new AttributeValue { S = et }; 
                await dm.PutPointAsync(req);
            }

            Status = Status.Ready;
        }
        private async Task WaitForTableToBeReady(GeoService dm)
        {
            var config = dm.GeoDataManagerConfiguration;
            var dtr = new DescribeTableRequest {TableName = config.TableName};
            var result = await config.DynamoDBClient.DescribeTableAsync(dtr);

            while (result.Table.TableStatus != TableStatus.ACTIVE)
            {
                await Task.Delay(2000);
                result = await config.DynamoDBClient.DescribeTableAsync(dtr);
            }
        }
    }

    public enum Status { NotStarted, CreatingTable, InsertingDataToTable, Ready}
}