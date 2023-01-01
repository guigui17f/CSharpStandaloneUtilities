using System.Net;

namespace GUIGUI17F
{
    public struct PageResponseData
    {
        public HttpStatusCode StatusCode { get; set; }
        public string FoundLocation { get; set; }
        public string Text { get; set; }
    }
}