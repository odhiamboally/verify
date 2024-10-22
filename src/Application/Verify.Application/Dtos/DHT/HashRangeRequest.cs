using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.DHT;
public record HashRangeRequest
{
    public required string StartValue { get; init; }
    public required string EndValue { get; init; }
}
