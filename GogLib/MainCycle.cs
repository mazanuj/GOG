using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GogLib.DataTypes;
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

        public static async Task InitializTask(IEnumerable<string> codes, IEnumerable<string> proxies, int dop)
        {
            try
            {
                var reqList = await ProxyHelper.GetRequestList(proxies);

                await codes.Distinct().ForEachAsync(dop, async code =>
                {
                    string result;
                    try
                    {
                        HttpRequest req;
                        var challenge = string.Empty;
                        lock (Locker)
                        {
                            req = reqList[0];
                            reqList.Move(req);

                            while (Utils.IsPermit)
                            {
                                if (Utils.CaptchaQueueCount > 0)
                                {
                                    challenge = Utils.CaptchaQueue.Challenge;
                                    break;
                                }
                                Task.Delay(500).Wait();
                            }
                        }

                        if (!Utils.IsPermit)
                            return;

                        result = await MainRequest.Get(code, req, challenge);

                        dynamic json = JObject.Parse(result);
                        result = json.products[0]["title"].ToString();
                    }
                    catch (Exception)
                    {
                        result = "Invalid";
                    }

                    var res = new MenuStruct {Code = code, Result = result};
                    Informer.RaiseStrReceived(res);
                    Utils.Result.Add(res);
                    Utils.SetIncrement();
                });
            }
            catch (Exception ex)
            {
                Informer.RaiseOnResultReceived(ex);
            }
        }
    }
}