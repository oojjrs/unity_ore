using System;

namespace oojjrs.ore
{
    public sealed class ReporterException : Exception
    {
        public bool IsNetworkError => StatusCode == 0;
        public string ResponseBody { get; }
        public long StatusCode { get; }

        public ReporterException(string message, long statusCode, string responseBody) : this(message, statusCode, responseBody, null)
        {
        }

        public ReporterException(string message, long statusCode, string responseBody, Exception innerException) : base(message, innerException)
        {
            ResponseBody = responseBody ?? string.Empty;
            StatusCode = statusCode;
        }
    }
}
