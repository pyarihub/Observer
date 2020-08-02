using Amazon.Geo.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DynamoDB_Geo.Interfaces
{
    public interface IGeoService
    {
        Task<PutPointResult> PutPointAsync(PutPointRequest putPointRequest, CancellationToken cancellationToken = default(CancellationToken));
        Task<QueryRectangleResult> QueryRectangleAsync(QueryRectangleRequest queryRectangleRequest, CancellationToken cancellationToken = default(CancellationToken));
    }
}
