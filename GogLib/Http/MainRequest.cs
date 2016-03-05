using System;
using System.Net;
using System.Threading.Tasks;
using xNet.Net;

namespace GogLib.Http
{
    internal static class MainRequest
    {
        internal static async Task<string> Get(string code, HttpRequest req, string challenge)
        {
            try
            {
               return await new WebClient().DownloadStringTaskAsync($@"http://www.gog.com/redeem/{code}/get?g-recaptcha-response={challenge}");
                ////req.Proxy = new HttpProxyClient("127.0.0.1", 64536);
                //req.AddHeader(HttpHeader.Accept, "application/json, text/plain, */*");
                //req.AddParam("{\"data\":{\"product_ids\":false,\"series_ids\":false}}");
                //req.Post("http://www.gog.com/userData.json").None();
                //req.Get($"http://www.gog.com/redeem/{code}").None();

                //req.AddUrlParam("g-recaptcha-response", challenge);
                //var t = req.Get($"http://www.gog.com/redeem/{code}/get");
                //return t.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message.Contains("403") ? "403" : string.Empty;
            }
        }
    }
}