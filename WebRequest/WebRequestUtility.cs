using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GUIGUI17F
{
    public class WebRequestUtility
    {
        /// <summary>
        /// request a page until success or reach the max retry count
        /// </summary>
        /// <param name="urlGetter">delegate to get the request url</param>
        /// <param name="method">the request method (POST, GET and so on)</param>
        /// <param name="encoding">encoding of the requesting page</param>
        /// <param name="cookie">cookie used for request the page and store the response information</param>
        /// <param name="responseChecker">delegate to check whether the request has success based on the response data</param>
        /// <param name="maxRetry">max retry count for this request</param>
        /// <param name="customHeaders">custom headers for this request</param>
        /// <param name="postDataGetter">delegate to get the request body of the POST request</param>
        /// <param name="contentType">ContentType of the request body</param>
        /// <param name="proxy">the web proxy used for this request</param>
        /// <param name="handleCookieManually">whether to handle the response data manually</param>
        /// <param name="allowAutoRedirect">whether allow auto redirect</param>
        /// <param name="timeout">timeout milliseconds for this request</param>
        /// <param name="validationCallback">validation callback for the https request</param>
        /// <param name="protocolVersion">HTTP protocol version for this request</param>
        public static PageResponseData AutoRequestPage(
            Func<string> urlGetter,
            string method,
            Encoding encoding,
            CookieContainer cookie,
            Func<PageResponseData, bool> responseChecker,
            int maxRetry = 3,
            Dictionary<string, string> customHeaders = null,
            Func<byte[]> postDataGetter = null,
            string contentType = null,
            IWebProxy proxy = null,
            bool handleCookieManually = false,
            bool allowAutoRedirect = false,
            int timeout = 6400,
            RemoteCertificateValidationCallback validationCallback = null,
            Version protocolVersion = null)
        {
            if (urlGetter == null || responseChecker == null || maxRetry < 1)
            {
                throw new ArgumentException();
            }
            bool success = false;
            int failCount = 0;
            PageResponseData data = default;
            do
            {
                try
                {
                    PageResponseData response = RequestPage(
                        urlGetter(),
                        method,
                        encoding,
                        cookie,
                        customHeaders,
                        postDataGetter?.Invoke(),
                        contentType,
                        proxy,
                        handleCookieManually,
                        allowAutoRedirect,
                        timeout,
                        validationCallback,
                        protocolVersion);
                    if (responseChecker(response))
                    {
                        data = response;
                        success = true;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (WebException e)
                {
                    if (e.Status == WebExceptionStatus.Timeout)
                    {
                        failCount++;
                    }
                    else
                    {
                        throw e;
                    }
                }
            } while (!success && failCount < maxRetry);
            if (!success)
            {
                throw new WebRequestFailedException($"request {urlGetter()} failed");
            }
            return data;
        }

        /// <summary>
        /// request a page
        /// </summary>
        /// <param name="url">the request url</param>
        /// <param name="method">the request method (POST, GET and so on)</param>
        /// <param name="encoding">encoding of the requesting page</param>
        /// <param name="cookie">cookie used for request the page and store the response information</param>
        /// <param name="customHeaders">custom headers for this request</param>
        /// <param name="postData">the request body of the POST request</param>
        /// <param name="contentType">ContentType of the request body</param>
        /// <param name="proxy">the web proxy used for this request</param>
        /// <param name="handleCookieManually">whether to handle the response data manually</param>
        /// <param name="allowAutoRedirect">whether allow auto redirect</param>
        /// <param name="timeout">timeout milliseconds for this request</param>
        /// <param name="validationCallback">validation callback for the https request</param>
        /// <param name="protocolVersion">HTTP protocol version for this request</param>
        public static PageResponseData RequestPage(
            string url,
            string method,
            Encoding encoding,
            CookieContainer cookie,
            Dictionary<string, string> customHeaders = null,
            byte[] postData = null,
            string contentType = null,
            IWebProxy proxy = null,
            bool handleCookieManually = false,
            bool allowAutoRedirect = false,
            int timeout = 6400,
            RemoteCertificateValidationCallback validationCallback = null,
            Version protocolVersion = null)
        {
            if (url.StartsWith("https", StringComparison.CurrentCultureIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = validationCallback ?? DefaultValidationCallback;
            }
            PageResponseData responseData = new PageResponseData();
            //fill request data
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.59 Safari/537.36 OPR/41.0.2353.46";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            if (cookie != null)
            {
                request.CookieContainer = cookie;
            }
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            if (proxy != null)
            {
                request.Proxy = proxy;
            }
            if (contentType != null)
            {
                request.ContentType = contentType;
            }
            if (customHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in customHeaders)
                {
                    switch (header.Key.ToUpper())
                    {
                        case "HOST":
                            request.Host = header.Value;
                            break;
                        case "CONNECTION":
                            if (header.Value.ToUpper() == "KEEP-ALIVE")
                            {
                                request.KeepAlive = true;
                            }
                            else
                            {
                                request.Connection = header.Value;
                            }
                            break;
                        case "REFERER":
                            request.Referer = header.Value;
                            break;
                        case "ACCEPT":
                            request.Accept = header.Value;
                            break;
                        default:
                            request.Headers.Add(header.Key, header.Value);
                            break;
                    }
                }
            }
            request.Timeout = timeout;
            request.Method = method;
            request.AllowAutoRedirect = allowAutoRedirect;
            if (method.ToUpper() == "POST")
            {
                if (contentType == null)
                {
                    request.ContentType = "application/x-www-form-urlencoded";
                }
                request.ContentLength = postData.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postData, 0, postData.Length);
                }
            }
            if (protocolVersion != null)
            {
                request.ProtocolVersion = protocolVersion;
            }
            //request the page
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                //handle response data
                if (cookie != null)
                {
                    if (handleCookieManually)
                    {
                        SaveResponseCookies(url, cookie, response.Headers.GetValues("Set-Cookie"));
                    }
                    else
                    {
                        cookie.Add(response.Cookies);
                    }
                }
                responseData.StatusCode = response.StatusCode;
                if (responseData.StatusCode == HttpStatusCode.Found)
                {
                    responseData.FoundLocation = response.GetResponseHeader("Location");
                }
                using (StreamReader responseReader = new StreamReader(response.GetResponseStream(), encoding))
                {
                    responseData.Text = responseReader.ReadToEnd();
                }
            }
            return responseData;
        }

        /// <summary>
        /// request a image until success or reach the max retry count
        /// </summary>
        /// <param name="urlGetter">delegate to get the request url</param>
        /// <param name="cookie">cookie used for request the page and store the response information</param>
        /// <param name="maxRetry">max retry count for this request</param>
        /// <param name="proxy">the web proxy used for this request</param>
        /// <param name="handleCookieManually">whether to handle the response data manually</param>
        /// <param name="timeout">timeout milliseconds for this request</param>
        /// <param name="validationCallback">validation callback for the https request</param>
        public static ImageResponseData AutoRequestImage(
            Func<string> urlGetter,
            CookieContainer cookie,
            int maxRetry = 3,
            IWebProxy proxy = null,
            bool handleCookieManually = false,
            int timeout = 6400,
            RemoteCertificateValidationCallback validationCallback = null)
        {
            if (urlGetter == null || maxRetry < 1)
            {
                throw new ArgumentException();
            }
            bool success = false;
            int failCount = 0;
            ImageResponseData data = default;
            do
            {
                try
                {
                    ImageResponseData response = RequestImage(urlGetter(), cookie, proxy, handleCookieManually, timeout, validationCallback);
                    if (response.Image != null)
                    {
                        data = response;
                        success = true;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (WebException e)
                {
                    if (e.Status == WebExceptionStatus.Timeout)
                    {
                        failCount++;
                    }
                    else
                    {
                        throw e;
                    }
                }
                catch (ArgumentException)
                {
                    failCount++;
                }
            } while (!success && failCount < maxRetry);
            if (!success)
            {
                throw new WebRequestFailedException($"request {urlGetter()} failed");
            }
            return data;
        }

        /// <summary>
        /// request a image
        /// </summary>
        /// <param name="url">the request url</param>
        /// <param name="cookie">cookie used for request the page and store the response information</param>
        /// <param name="proxy">the web proxy used for this request</param>
        /// <param name="handleCookieManually">whether to handle the response data manually</param>
        /// <param name="timeout">timeout milliseconds for this request</param>
        /// <param name="validationCallback">validation callback for the https request</param>
        public static ImageResponseData RequestImage(
            string url,
            CookieContainer cookie,
            IWebProxy proxy = null,
            bool handleCookieManually = false,
            int timeout = 6400,
            RemoteCertificateValidationCallback validationCallback = null)
        {
            if (url.StartsWith("https", StringComparison.CurrentCultureIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = validationCallback ?? DefaultValidationCallback;
            }
            ImageResponseData responseData = new ImageResponseData();
            //fill request data
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.59 Safari/537.36 OPR/41.0.2353.46";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.CookieContainer = cookie;
            if (proxy != null)
            {
                request.Proxy = proxy;
            }
            request.Timeout = timeout;
            //request the page
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                //handle response data
                if (handleCookieManually)
                {
                    SaveResponseCookies(url, cookie, response.Headers.GetValues("Set-Cookie"));
                }
                else
                {
                    cookie.Add(response.Cookies);
                }
                responseData.StatusCode = response.StatusCode;
                using (Stream responseStream = response.GetResponseStream())
                {
                    responseData.Image = Image.FromStream(responseStream);
                }
            }
            return responseData;
        }

        private static bool DefaultValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static CookieContainer SaveResponseCookies(string url, CookieContainer cookie, string[] headerRaw)
        {
            if (headerRaw == null)
            {
                return cookie;
            }
            for (int i = 0; i < headerRaw.Length; i++)
            {
                string[] messages = headerRaw[i].Split(';');
                int index = messages[0].IndexOf('=');
                if (index < 0)
                {
                    continue;
                }
                Cookie newCookie = new Cookie(messages[0].Substring(0, index).Trim(), GetCookieValue(messages[0]));
                if (messages.Length > 1)
                {
                    for (int j = 1; j < messages.Length; j++)
                    {
                        string argText = messages[j].Trim();
                        if (argText.StartsWith("Version", StringComparison.CurrentCultureIgnoreCase))
                        {
                            newCookie.Version = int.Parse(GetCookieValue(argText));
                        }
                        else if (argText.StartsWith("Domain", StringComparison.CurrentCultureIgnoreCase))
                        {
                            string domain = GetCookieValue(argText);
                            if (domain[0] != '.')
                            {
                                domain = "." + domain;
                            }
                            newCookie.Domain = domain;
                        }
                        else if (argText.StartsWith("Path", StringComparison.CurrentCultureIgnoreCase))
                        {
                            newCookie.Path = GetCookieValue(argText);
                        }
                    }
                }
                if (string.IsNullOrEmpty(newCookie.Domain))
                {
                    int searchIndex = -1;
                    for (int j = 0; j < 3; j++)
                    {
                        searchIndex = url.IndexOf('/', searchIndex + 1);
                    }
                    if (searchIndex > 0)
                    {
                        url = url.Substring(0, searchIndex);
                    }
                    cookie.Add(new Uri(url), newCookie);
                }
                else
                {
                    cookie.Add(newCookie);
                }
            }
            return cookie;
        }

        private static string GetCookieValue(string cookieText)
        {
            int index = cookieText.IndexOf('=');
            return cookieText.Substring(index + 1).Trim();
        }

        /// <summary>
        /// update a cookie container based on the response cookie information
        /// </summary>
        public static CookieContainer UpdateCookieContainer(CookieContainer container, string rawCookies, string domain)
        {
            if (!string.IsNullOrEmpty(rawCookies))
            {
                string[] cookies = rawCookies.Split(';');
                for (int i = 0; i < cookies.Length; i++)
                {
                    int index = cookies[i].IndexOf('=');
                    Cookie newCookie = new Cookie(cookies[i].Substring(0, index).Trim(), cookies[i].Substring(index + 1).Trim());
                    newCookie.Domain = domain;
                    container.Add(newCookie);
                }
            }
            return container;
        }
    }
}