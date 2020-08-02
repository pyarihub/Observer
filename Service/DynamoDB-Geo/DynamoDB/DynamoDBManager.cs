using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Geo.Model;
using Amazon.Geo.S2;
using Amazon.Geo.Util;

namespace Amazon.Geo.DynamoDB
{
    internal sealed class DynamoDBManager
    {
        private readonly GeoDataManagerConfiguration _config;

        public DynamoDBManager(GeoDataManagerConfiguration config)
        {
            _config = config;
        }
        public async Task<GetPointResult> GetPointAsync(GetPointRequest getPointRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (getPointRequest == null) throw new ArgumentNullException("getPointRequest");
            var geohash = S2Manager.GenerateGeohash(getPointRequest.GeoPoint);
            var hashKey = S2Manager.GenerateHashKey(geohash, _config.HashKeyLength);

            var getItemRequest = getPointRequest.GetItemRequest;
            getItemRequest.TableName = _config.TableName;

            var hashKeyValue = new AttributeValue
            {
                N = hashKey.ToString(CultureInfo.InvariantCulture)
            };
            getItemRequest.Key[_config.HashKeyAttributeName] = hashKeyValue;
            getItemRequest.Key[_config.RangeKeyAttributeName] = getPointRequest.RangeKeyValue;

            GetItemResponse getItemResult = await _config.DynamoDBClient.GetItemAsync(getItemRequest, cancellationToken).ConfigureAwait(false);
            var getPointResult = new GetPointResult(getItemResult);

            return getPointResult;
        }

        public async Task<PutPointResult> PutPointAsync(PutPointRequest putPointRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (putPointRequest == null) throw new ArgumentNullException("putPointRequest");

            var geohash = S2Manager.GenerateGeohash(putPointRequest.FromGeoPoint);
            var rangeHash = S2Manager.GenerateGeohash(putPointRequest.ToGeoPoint);
            var hashKey = S2Manager.GenerateHashKey(geohash, _config.HashKeyLength);
            var geoJson = GeoJsonMapper.StringFromGeoObject(putPointRequest.FromGeoPoint);

            var putItemRequest = putPointRequest.PutItemRequest;
            putItemRequest.TableName = _config.TableName;

            var hashKeyValue = new AttributeValue
            {
                N = hashKey.ToString(CultureInfo.InvariantCulture)
            };
            putItemRequest.Item[_config.HashKeyAttributeName] = hashKeyValue;
            putItemRequest.Item[_config.RangeKeyAttributeName] = new AttributeValue
            {
                S = rangeHash.ToString(CultureInfo.InvariantCulture)
            };


            var geohashValue = new AttributeValue
            {
                N = geohash.ToString(CultureInfo.InvariantCulture)
            };

            putItemRequest.Item[_config.GeohashAttributeName] = geohashValue;

            var geoJsonValue = new AttributeValue
            {
                S = geoJson
            };

            putItemRequest.Item[_config.GeoJsonAttributeName] = geoJsonValue;

            PutItemResponse putItemResult = await _config.DynamoDBClient.PutItemAsync(putItemRequest, cancellationToken).ConfigureAwait(false);
            var putPointResult = new PutPointResult(putItemResult);

            return putPointResult;
        }

        /// <summary>
        ///     Query Amazon DynamoDB
        /// </summary>
        /// <param name="queryRequest"></param>
        /// <param name="hashKey">Hash key for the query request.</param>
        /// <param name="range">The range of geohashs to query.</param>
        /// <returns>The query result.</returns>
        public async Task<IReadOnlyList<QueryResponse>> QueryGeohashAsync(QueryRequest queryRequest, ulong hashKey, GeohashRange range, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (queryRequest == null) throw new ArgumentNullException("queryRequest");
            if (range == null) throw new ArgumentNullException("range");

            var queryResults = new List<QueryResponse>();
            IDictionary<String, AttributeValue> lastEvaluatedKey = null;
            do
            {
                var keyConditions = new Dictionary<String, Condition>();

                var hashKeyCondition = new Condition
                {
                    ComparisonOperator = ComparisonOperator.EQ,
                    AttributeValueList = new List<AttributeValue>
                    {
                        new AttributeValue
                        {
                            N = hashKey.ToString(CultureInfo.InvariantCulture)
                        }
                    }
                };

                keyConditions.Add(_config.HashKeyAttributeName, hashKeyCondition);

                var minRange = new AttributeValue
                {
                    N = range.RangeMin.ToString(CultureInfo.InvariantCulture)
                };
                var maxRange = new AttributeValue
                {
                    N = range.RangeMax.ToString(CultureInfo.InvariantCulture)
                };

                var geohashCondition = new Condition
                {
                    ComparisonOperator = ComparisonOperator.BETWEEN,
                    AttributeValueList = new List<AttributeValue>
                    {
                        minRange,
                        maxRange
                    }
                };

                keyConditions.Add(_config.GeohashAttributeName, geohashCondition);

                queryRequest.TableName = _config.TableName;
                queryRequest.KeyConditions = keyConditions;
                queryRequest.IndexName = _config.GeohashIndexName;
                queryRequest.ConsistentRead = true;
                queryRequest.ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL;

                if (lastEvaluatedKey != null && lastEvaluatedKey.Count > 0)
                {
                    queryRequest.ExclusiveStartKey[_config.HashKeyAttributeName] = lastEvaluatedKey[_config.HashKeyAttributeName];
                    queryRequest.ExclusiveStartKey[_config.RangeKeyAttributeName] = lastEvaluatedKey[_config.RangeKeyAttributeName];
                    queryRequest.ExclusiveStartKey[_config.GeohashAttributeName] = lastEvaluatedKey[_config.GeohashAttributeName];
                }

                QueryResponse queryResult = await _config.DynamoDBClient.QueryAsync(queryRequest, cancellationToken).ConfigureAwait(false);
                queryResults.Add(queryResult);

                lastEvaluatedKey = queryResult.LastEvaluatedKey;
            } while (lastEvaluatedKey != null && lastEvaluatedKey.Count > 0);

            return queryResults;
        }
    }
}