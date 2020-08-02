using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Geo.Model;
using DynamoDB_Geo.Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SampleServer;
using SampleServer.Models;

namespace MapService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MapController : ControllerBase
    {
        private readonly IGeoService geoService;

        public MapController(IGeoService geoService)
        {
            this.geoService = geoService;
        }


        [HttpPost("Search")]
        public async Task<ActionResult> QueryRectangle(MapBounds bounds)
        {

            //await Utilities.Instance.StartLoadData(this.geoService);

            var min = new GeoPoint(bounds.MinLat, bounds.MinLng);
            var max = new GeoPoint(bounds.MaxLat, bounds.MaxLng);

            var attributesToGet = new List<string>
            {
                "geoJson",
                "rangeKey"
            };

            var radReq = new QueryRectangleRequest(min, max);
            radReq.QueryRequest.AttributesToGet = attributesToGet;
            
            var result = await geoService.QueryRectangleAsync(radReq);
            var dtos = GetResultsFromQuery(result);

            return Ok(dtos);
        }
        private IEnumerable<Point> GetResultsFromQuery(GeoQueryResult result)
        {
            return from item in result.Items
                       let geoJsonString = item["geoJson"].S
                       let point = JsonConvert.DeserializeObject<GeoPoint>(geoJsonString)
                       select new Point
                       {
                           lat = point.lat,
                           lng = point.lng
                       };
        }
    }
}
