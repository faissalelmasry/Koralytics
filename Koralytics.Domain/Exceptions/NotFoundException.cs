using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Exceptions
{
    public sealed class NotFoundException: BaseBusinessException
    {
        public NotFoundException(string message) : base(message)
        {
        }
        public override HttpStatusCode StatusCode=> HttpStatusCode.NotFound;

        public override string Title => "Resource Not Found";
    }
}
