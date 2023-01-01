using System.Drawing;
using System.Net;

namespace GUIGUI17F
{
    public struct ImageResponseData
    {
        public HttpStatusCode StatusCode { get; set; }
        public Image Image { get; set; }
    }
}