using System;
using System.Threading.Tasks;
using GogLib.DataTypes;
using GogLib.Utilities;
using xNet.Net;

namespace GogLib.Http
{
    internal static class RequestHelper
    {
        internal static HttpRequest GetRequest => new HttpRequest
        {
            Cookies = new CookieDictionary(),
            UserAgent = HttpHelper.ChromeUserAgent(),
            EnableAdditionalHeaders = true,
            EnableEncodingContent = true,
            ConnectTimeout = 30000,
            ReadWriteTimeout = 30000,
            AllowAutoRedirect = true,
            MaximumAutomaticRedirections = 30,
            [HttpHeader.AcceptLanguage] = "ru-RU,ru;q=0.8,en-US;q=0.6,en;q=0.4"
        };

        internal static async Task<HttpRequest> GetRequestTaskAsync(ProxyStruct str)
        {
            var req = GetRequest;
            return await Task.Run(() =>
            {
                try
                {
                    var host = str.Host;
                    var login = str.Login;
                    var type = str.Type;

                    if (string.IsNullOrEmpty(host) || !host.Contains(":"))
                        return req;

                    var arr = host.Split(':');
                    switch (type)
                    {
                        case ProxyType.Http:
                            req.Proxy = new HttpProxyClient(arr[0], int.Parse(arr[1]));
                            break;
                        case ProxyType.Socks4:
                            req.Proxy = new Socks4ProxyClient(arr[0], int.Parse(arr[1]));
                            break;
                        case ProxyType.Socks4a:
                            req.Proxy = new Socks4aProxyClient(arr[0], int.Parse(arr[1]));
                            break;
                        case ProxyType.Socks5:
                            req.Proxy = new Socks5ProxyClient(arr[0], int.Parse(arr[1]));
                            break;
                        default:
                            req.Proxy = new HttpProxyClient(arr[0], int.Parse(arr[1]));
                            break;
                    }

                    if (!string.IsNullOrEmpty(login) && login.Contains(":"))
                    {
                        var arrr = login.Split(':');
                        req.Proxy.Username = arrr[0];
                        req.Proxy.Password = arrr[1];
                    }
                }
                catch (Exception ex)
                {
                    Informer.RaiseOnResultReceived(ex);
                }
                return req;
            });
        }
    }
}