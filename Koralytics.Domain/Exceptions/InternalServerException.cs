using System.Net;

namespace Koralytics.Domain.Exceptions
{
    public sealed class InternalServerException : BaseBusinessException
    {
        public InternalServerException(string msg) : base(msg)
        { }

        public override HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;

        public override string Title => "Internal Server Error";
    }
}
