using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Exceptions
{
    public sealed class UnauthorizedException:BaseBusinessException
    {
        public UnauthorizedException(string message)
        : base(message)
        {
        }

        public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;

        public override string Title => "Unauthorized";
    }
}
