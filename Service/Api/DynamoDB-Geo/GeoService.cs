using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.Geo.DynamoDB;
using Amazon.Geo.Model;
using Amazon.Geo.S2;
using Amazon.Geo.Util;
using DynamoDB_Geo.Interfaces;
using Google.Common.Geometry;

namespace Amazon.Geo
{
    public sealed class GeoService : IGeoService
    {
        private readonly GeoDataManagerConfiguration _config;
        private readonly DynamoDBManager _dynamoDBManager;

        public GeoService(GeoDataManagerConfiguration config)
        {
            if (config == null) throw new ArgumentNullException("config");
            _config = config;

            _dynamoDBManager = new DynamoDBManager(_config);
        }

        public GeoDataManagerConfiguration GeoDataManagerConfiguration
        {
            get { return _config; }
        }
        public Task<PutPointResult> PutPointAsync(PutPointRequest putPointRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (putPointRequest == null) throw new ArgumentNullException("putPointRequest");
            return _dynamoDBManager.PutPointAsync(putPointRequest, cancellationToken);
        }

        public async Task<QueryRectangleResult> QueryRectangleAsync(QueryRectangleRequest queryRectangleRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (queryRectangleRequest == null) throw new ArgumentNullException("queryRectangleRequest");
            var latLngRect = S2Util.GetBoundingLatLngRect(queryRectangleRequest);

            var cellUnion = S2Manager.FindCellIds(latLngRect);
            var ranges = MergeCells(cellUnion);

            var result = await DispatchQueries(ranges, queryRectangleRequest, cancellationToken).ConfigureAwait(false);
            return new QueryRectangleResult(result);
        }

        /// <summary>
        ///     Merge continuous cells in cellUnion and return a list of merged GeohashRanges.
        /// </summary>
        /// <param name="cellUnion">Container for multiple cells.</param>
        /// <returns>A list of merged GeohashRanges.</returns>
        private static List<GeohashRange> MergeCells(S2CellUnion cellUnion)
        {
            var ranges = new List<GeohashRange>();
            foreach (var c in cellUnion.CellIds)
            {
                var range = new GeohashRange(c.RangeMin.Id, c.RangeMax.Id);

                var wasMerged = false;
                foreach (var r in ranges)
                {
                    if (r.TryMerge(range))
                    {
                        wasMerged = true;
                        break;
                    }
                }

                if (!wasMerged)
                {
                    ranges.Add(range);
                }
            }

            return ranges;
        }

        /// <summary>
        ///     Filter out any points outside of the queried area from the input list.
        /// </summary>
        /// <param name="list">List of items return by Amazon DynamoDB. It may contains points outside of the actual area queried.</param>
        /// <param name="geoQueryRequest">List of items within the queried area.</param>
        /// <returns></returns>
        private IEnumerable<IDictionary<string, AttributeValue>> Filter(IEnumerable<IDictionary<string, AttributeValue>> list,
                                                                        GeoQueryRequest geoQueryRequest)
        {
            var result = new List<IDictionary<String, AttributeValue>>();

            S2LatLngRect? latLngRect = null;
            S2LatLng? centerLatLng = null;
            double radiusInMeter = 0;
            if (geoQueryRequest is QueryRectangleRequest)
            {
                latLngRect = S2Util.GetBoundingLatLngRect(geoQueryRequest);
            }
            foreach (var item in list)
            {
                var geoJson = item[_config.GeoJsonAttributeName].S;
                var geoPoint = GeoJsonMapper.GeoPointFromString(geoJson);

                var latLng = S2LatLng.FromDegrees(geoPoint.lat, geoPoint.lng);
                if (latLngRect != null && latLngRect.Value.Contains(latLng))
                {
                    result.Add(item);
                }
                else if (centerLatLng != null && radiusInMeter > 0
                         && centerLatLng.Value.GetEarthDistance(latLng) <= radiusInMeter)
                {
                    result.Add(item);
                }
            }

            return result;
        }
        private async Task<GeoQueryResult> DispatchQueries(IEnumerable<GeohashRange> ranges, GeoQueryRequest geoQueryRequest, CancellationToken cancellationToken)
        {
            var geoQueryResult = new GeoQueryResult();
            var futureList = new List<Task>();
            var internalSource = new CancellationTokenSource();
            var internalToken = internalSource.Token;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, internalToken);
            
            foreach (var outerRange in ranges)
            {
                foreach (var range in outerRange.TrySplit(_config.HashKeyLength))
                {
                    var task = RunGeoQuery(geoQueryRequest, geoQueryResult, range, cts.Token);
                    futureList.Add(task);
                }
            }

            Exception inner = null;
            try
            {
                for (var i = 0; i < futureList.Count; i++)
                {
                    try
                    {
                        await futureList[i].ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        inner = e;
                        // cancel the others
                        internalSource.Cancel(true);
                    }
                }
            }
            catch (Exception ex)
            {
                inner = inner ?? ex;
                throw new ClientException("Querying Amazon DynamoDB failed.", inner);
            }
            return geoQueryResult;
        }

        private async Task RunGeoQuery(GeoQueryRequest request, GeoQueryResult geoQueryResult, GeohashRange range, CancellationToken cancellationToken)
        {
            var queryRequest = request.QueryRequest.CopyQueryRequest();
            var hashKey = S2Manager.GenerateHashKey(range.RangeMin, _config.HashKeyLength);

            var results = await _dynamoDBManager.QueryGeohashAsync(queryRequest, hashKey, range, cancellationToken).ConfigureAwait(false);

            foreach (var queryResult in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // This is a concurrent collection
                geoQueryResult.QueryResults.Add(queryResult);

                var filteredQueryResult = Filter(queryResult.Items, request);

                // this is a concurrent collection
                foreach (var r in filteredQueryResult)
                    geoQueryResult.Items.Add(r);
            }
        }
    }
}