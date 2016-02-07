using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GogLib.DataTypes;
using GogLib.Http;
using GogLib.Utilities;
using xNet.Net;

namespace GogLib.Proxy
{
    internal static class ProxyHelper
    {
        internal static async Task<List<HttpRequest>> GetRequestList(IEnumerable<string> proxies)
        {
            var list = new List<HttpRequest>();
            try
            {
                foreach (var x in proxies.Distinct().Where(x => !string.IsNullOrEmpty(x)))
                {
                    try
                    {
                        var str = new ProxyStruct();
                        var arr = x.Split('@');
                        str.Host = arr[1];
                        str.Login = arr[0];

                        switch (arr[2].ToLower())
                        {
                            case "http":
                            case "https":
                                str.Type = ProxyType.Http;
                                break;
                            case "socks4":
                                str.Type = ProxyType.Socks4;
                                break;
                            case "socks4a":
                                str.Type = ProxyType.Socks4a;
                                break;
                            case "socks5":
                                str.Type = ProxyType.Socks5;
                                break;
                            default:
                                str.Type = ProxyType.Http;
                                break;
                        }

                        list.Add(await RequestHelper.GetRequestTaskAsync(str));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Informer.RaiseOnResultReceived(ex);
            }

            if (list.Count == 0)
                list.Add(RequestHelper.GetRequest);

            return list;
        }
    }
}