using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace Amazon.Geo.Model
{
    /// <summary>
 /// Put point request. The request must specify a geo point and a range key value. You can modify PutItemRequest to
 /// customize the underlining Amazon DynamoDB put item request, but the table name, hash key, geohash, and geoJson
 /// attribute will be overwritten by GeoDataManagerConfiguration.
    /// </summary>
    public sealed class PutPointRequest : GeoDataRequest
    {
        public GeoPoint FromGeoPoint { get; private set; }
        public GeoPoint ToGeoPoint { get; private set; }
        public PutItemRequest PutItemRequest { get; private set; }

        public PutPointRequest(GeoPoint fromGeoPoint, GeoPoint toGeoPoint)
        {
            if (fromGeoPoint == null) throw new ArgumentNullException("from geoPoint");
            if (toGeoPoint == null) throw new ArgumentNullException("to geoPoint");

            PutItemRequest = new PutItemRequest();
            FromGeoPoint = fromGeoPoint;
            ToGeoPoint = toGeoPoint;
        }
    }
}
