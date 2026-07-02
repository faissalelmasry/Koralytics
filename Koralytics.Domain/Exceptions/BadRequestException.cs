using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Exceptions
{
    public sealed class BadRequestException : BaseBusinessException
    {
        public BadRequestException(string msg) : base(msg)
        { }

        public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

        public override string Title => "Validation Error";

    }
}



