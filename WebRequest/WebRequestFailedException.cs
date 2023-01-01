using System.Net;

namespace GUIGUI17F
{
    public class WebRequestFailedException : WebException
    {
        public WebRequestFailedException(string message) : base(message)
        {
        }
    }
}