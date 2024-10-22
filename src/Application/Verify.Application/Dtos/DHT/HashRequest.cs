using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.DHT;
public record HashRequest
{
    public required string ValueToHash { get; init; }
}
