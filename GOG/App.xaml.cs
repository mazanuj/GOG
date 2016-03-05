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

            if (Utils.ResultAll.Any())
                File.WriteAllLines($"{DateTime.Now.ToString("yyMMdd_HHmmss")}_All.txt",
                    Utils.ResultAll.Select(x => $"{x.Code} => {x.Result}"));

            if (Utils.ResultTrue.Any())
                File.WriteAllLines($"{DateTime.Now.ToString("yyMMdd_HHmmss")}_True.txt",
                    Utils.ResultTrue.Select(x => $"{x.Code} => {x.Result}"));

            if (Utils.ResultFalse.Any())
                File.WriteAllLines($"{DateTime.Now.ToString("yyMMdd_HHmmss")}_False.txt",
                    Utils.ResultFalse.Select(x => $"{x.Code} => {x.Result}"));

            if (Utils.Result403.Any())
                File.WriteAllLines($"{DateTime.Now.ToString("yyMMdd_HHmmss")}_403.txt",
                    Utils.Result403.Select(x => $"{x.Code} => {x.Result}"));

            Settings.Default.Save();
            base.OnExit(e);
        }
    }
}