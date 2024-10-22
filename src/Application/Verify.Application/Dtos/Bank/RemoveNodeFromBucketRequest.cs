using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore.Internal;

namespace Verify.Application.Dtos.Bank;
public record RemoveNodeFromBucketRequest
{
    public required string NodeBIC { get; init; }
    public required Uri NodeUri { get; init; }
    public required byte[] NodeHash { get; init; }
    public required byte[] NodeRangeHash { get; init; }
    public string? NodeEndPoint { get; init; }
    public List<Uri>? KnownPeers { get; init; }


}
