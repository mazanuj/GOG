using System;
using System.Threading.Tasks;
using GogLib.Utilities;
using xNet.Net;

namespace GogLib.Http
{
    internal static class MainRequest
    {
        internal static async Task<string> Get(string code, HttpRequest req, string challenge)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return req.Get($"http://www.gog.com/redeem/{code}/get?g-recaptcha-response={challenge}").ToString();
                }
                catch (Exception ex)
                {
                    Informer.RaiseOnResultReceived(ex);
                    return string.Empty;
                }
            });
        }
    }
}