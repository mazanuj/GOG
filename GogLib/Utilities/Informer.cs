using System;

namespace GogLib.Utilities
{
    public static class Informer
    {
        public delegate void InformMethodStr(string str);

        public static event InformMethodStr OnResultStr;

        public static void RaiseOnResultReceived(string str)
        {
            var handler = OnResultStr;
            handler?.Invoke(str);
        }

        public static void RaiseOnResultReceived(Exception ex)
        {
            var handler = OnResultStr;
            handler?.Invoke(ex.Message);
            //handler?.Invoke(ex.StackTrace);
        }
    }
}