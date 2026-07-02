using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Exceptions
{
    public sealed class ConflictException:BaseBusinessException
    {
        public ConflictException(string message)
        : base(message)
        {
        }

        public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;

        public override string Title => "Conflict";
    }
}
