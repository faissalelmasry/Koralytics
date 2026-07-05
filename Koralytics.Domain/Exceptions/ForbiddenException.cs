using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Exceptions
{
    public sealed class ForbiddenException:BaseBusinessException
    {
        public ForbiddenException(string message)
        : base(message)
        {
        }

        public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;

        public override string Title => "Forbidden";
    }
}
