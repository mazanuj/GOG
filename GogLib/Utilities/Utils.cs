using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GogLib.DataTypes;

namespace GogLib.Utilities
{
    public static class Utils
    {
        private static int count = 0;
        private static readonly object Locker = new object();
        public static bool IsPermit { get; set; }
        public static List<DriverStruct> DriverList { get; }
        public static List<MenuStruct> Result { get; }
        private static readonly Queue<CaptchaStruct> captchaQueue;
        public static int CaptchaQueueCount => captchaQueue.Count;
        public static CancellationTokenSource CancelToken { get; set; }

        public static CaptchaStruct CaptchaQueue
        {
            get
            {
                while (CaptchaQueueCount > 0)
                {
                    return captchaQueue.Dequeue();
                }
                return new CaptchaStruct {Answer = "Reload", Challenge = string.Empty, Date = DateTime.Now};
            }
            set { captchaQueue.Enqueue(value); }
        }

        static Utils()
        {
            Result = new List<MenuStruct>();
            DriverList = new List<DriverStruct>();
            captchaQueue = new Queue<CaptchaStruct>();
            CancelToken = new CancellationTokenSource();
        }

        internal static void SetIncrement()
        {
            lock (Locker)
                Informer.RaiseOnCountChanged((++count).ToString());
        }

        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }

        public static void Move<T>(this List<T> list, T item)
        {
            list.Remove(item);
            list.Add(item);
        }
    }
}