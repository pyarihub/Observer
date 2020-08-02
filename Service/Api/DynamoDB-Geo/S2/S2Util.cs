using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Geo.Model;
using Google.Common.Geometry;

namespace Amazon.Geo.S2
{
    internal static class S2Util
    {


        /// <summary>
        /// An utility method to get a bounding box of latitude and longitude from a given GeoQueryRequest.
        /// </summary>
        /// <param name="geoQueryRequest">It contains all of the necessary information to form a latitude and longitude box.</param>
        /// <returns></returns>
        public static S2LatLngRect GetBoundingLatLngRect(GeoQueryRequest geoQueryRequest)
        {
            if (geoQueryRequest is QueryRectangleRequest)
            {
                var queryRectangleRequest = (QueryRectangleRequest)geoQueryRequest;

                var minPoint = queryRectangleRequest.MinPoint;
                var maxPoint = queryRectangleRequest.MaxPoint;

                var latLngRect = default(S2LatLngRect);

                if (minPoint != null && maxPoint != null)
                {
                    var minLatLng = S2LatLng.FromDegrees(minPoint.lat, minPoint.lng);
                    var maxLatLng = S2LatLng.FromDegrees(maxPoint.lat, maxPoint.lng);

                    latLngRect = new S2LatLngRect(minLatLng, maxLatLng);
                }

                return latLngRect;
            }
            return S2LatLngRect.Empty;
        }
    }
}