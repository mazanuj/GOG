using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GogLib.Http;
using GogLib.Utilities;
using Newtonsoft.Json.Linq;
using xNet.Net;
using ProxyHelper = GogLib.Proxy.ProxyHelper;

namespace GogLib
{
    public static class MainCycle
    {
        private static readonly object Locker = new object();

        public static async Task<Dictionary<string, string>> InitializTask(IEnumerable<string> codes,
            IEnumerable<string> proxies, int dop)
        {
            var resultList = new Dictionary<string, string>();
            try
            {
                var reqList = await ProxyHelper.GetRequestList(proxies);

                await codes.Distinct().ForEachAsync(dop, async code =>
                {
                    string result;
                    try
                    {
                        HttpRequest req;
                        lock (Locker)
                        {
                            req = reqList[0];
                            reqList.Move(req);
                        }
                        result = await MainRequest.Get(code, req, "");

                        dynamic json = JObject.Parse(result);
                        result = json.products[0]["title"].ToString();                        
                    }
                    catch (Exception)
                    {
                        result = "Invalid";
                    }

                    resultList.Add(code, result);
                });
            }
            catch (Exception ex)
            {
                Informer.RaiseOnResultReceived(ex);
            }

            return resultList;
        }
    }
}