using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Exceptions
{
    public abstract class BaseBusinessException: Exception
    {
        protected BaseBusinessException(string message) : base(message)
        {
        }
        public abstract HttpStatusCode StatusCode { get; }

        public abstract string Title { get; }
    }
}
