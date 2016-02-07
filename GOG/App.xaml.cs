using System;
using System.IO;
using System.Linq;
using System.Windows;
using GogLib.Utilities;
using GOG.Properties;

namespace GOG
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnExit(ExitEventArgs e)
        {
            foreach (var driverStruct in Utils.DriverList)
            {
                try
                {
                    driverStruct.Driver.Dispose();
                }
                catch (Exception)
                {
                }
            }

            if(Utils.Result.Any())
                File.WriteAllLines($"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.txt",
                        Utils.Result.Select(x => $"{x.Code} => {x.Result}"));

            Settings.Default.Save();
            base.OnExit(e);
        }
    }
}