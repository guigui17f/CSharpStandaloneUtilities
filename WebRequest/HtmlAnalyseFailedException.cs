using System;

namespace GUIGUI17F
{
    public class HtmlAnalyseFailedException : Exception
    {
        public HtmlAnalyseFailedException(Exception e) : base("html analyse failed", e)
        {
        }
    }
}