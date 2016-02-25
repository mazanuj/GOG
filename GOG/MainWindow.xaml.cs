using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using GogLib;
using GogLib.CaptchaHelper;
using GogLib.DataTypes;
using GogLib.Utilities;
using GOG.Annotations;
using GOG.Properties;
using Microsoft.Win32;
using OpenQA.Selenium.Chrome;
using Timer = System.Timers.Timer;

namespace GOG
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        private string all = "0";
        private string curr = "0";
        private Timer CaptchaTimer { get; }
        private IEnumerable<string> codes = new List<string>();
        private IEnumerable<string> Proxies { get; set; }
        public ObservableCollection<MenuStruct> DataItemsMenu { get; }

        private IEnumerable<string> Codes
        {
            get { return codes; }
            set
            {
                codes = value;
                All = Codes.Distinct().Count().ToString();
                OnPropertyChanged();
            }
        }

        public string All
        {
            get { return all; }
            set
            {
                all = value;
                OnPropertyChanged();
            }
        }

        public string Curr
        {
            get { return curr; }
            set
            {
                curr = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            DataContext = this;
            Codes = new List<string>();
            Proxies = new List<string>();
            DataItemsMenu = new ObservableCollection<MenuStruct>();
            InitializeComponent();
            DataGridMenu.ItemsSource = DataItemsMenu;
            ButtonStop.IsEnabled = false;            

            CaptchaTimer = new Timer
            {
                AutoReset = true,
                Interval = 1000
            };

            Informer.OnResultStr +=
                async result =>
                    await
                        Application.Current.Dispatcher.BeginInvoke(
                            new Action(() => DataItemsMenu.Insert(0, new MenuStruct {Result = result})));

            Informer.OnCountChanged +=
                async result =>
                    await
                        Application.Current.Dispatcher.BeginInvoke(
                            new Action(() => Curr = result));

            Informer.OnResultStruct +=
                async result =>
                    await
                        Application.Current.Dispatcher.BeginInvoke(
                            new Action(() => DataItemsMenu.Insert(0, result)));
        }

        private void LaunchGOGOnGitHub(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/mazanuj/GOG");
        }

        private void ButtonAddCodes_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null)
                    return;

                var ofd = new OpenFileDialog
                {
                    Multiselect = false,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Filter = "Text File (TXT)|*.TXT",
                    InitialDirectory = Environment.CurrentDirectory
                };

                if (ofd.ShowDialog() != true) return;

                if (Equals(button, ButtonCodes))
                    Codes = File.ReadAllLines(ofd.FileName);
                else Proxies = File.ReadAllLines(ofd.FileName);
            }
            catch (Exception ex)
            {
                Informer.RaiseOnResultReceived(ex);
            }
        }

        private void ButtonStop_OnClick(object sender, RoutedEventArgs e)
        {
            Utils.IsPermit = false;
            CaptchaTimer.Stop();
            CaptchaTimer.Elapsed -= CaptchaTimer_Elapsed;
            Utils.CancelToken.Cancel();
            ButtonStop.IsEnabled = false;
        }

        private async void ButtonStart_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Codes.Any())
            {
                Informer.RaiseOnResultReceived("No codes found");
                return;
            }

            ButtonIsEnabled(false);
            Utils.IsPermit = true;
            if (Utils.DriverList.Count > Settings.Default.CaptchaNum)
            {
                var num = Utils.DriverList.Count - Settings.Default.CaptchaNum;
                DeleteBrowsers(num);
            }
            else
            {
                var num = Settings.Default.CaptchaNum - Utils.DriverList.Count;
                LoadBrowsers(num);
            }
            CaptchaTimer.Elapsed += CaptchaTimer_Elapsed;
            Utils.CancelToken = new CancellationTokenSource();
            CaptchaTimer.Start();

            await Task.Run(async () =>
            {
                try
                {
                    await MainCycle.InitializTask(Codes, Proxies, Settings.Default.CaptchaNum);
                }
                catch (Exception ex)
                {
                    Informer.RaiseOnResultReceived(ex);
                }
            });

            ButtonStop_OnClick(ButtonStop, null);
            ButtonIsEnabled(true);
        }

        private static async void GetNoCaptcha(DriverStruct drStr)
        {
            drStr.IsEnabled = false;
            var result = await GetCaptcha.GetNoCaptcha(Settings.Default.AntigateKey, drStr);

            drStr.IsEnabled = true;
            if (!result.Answer.StartsWith("Error message") || result.Answer.Contains("Stopped") ||
                result.Answer.Contains("Отменена задача"))
                Utils.CaptchaQueue = result;
        }

        private static void CaptchaTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(Settings.Default.AntigateKey))
                        return;

                    if (!Utils.IsPermit) return;
                    var count = Utils.DriverList.Count(x => x.IsEnabled);

                    if (count <= 0) return;
                    foreach (var source in Utils.DriverList.Where(x => x.IsEnabled))
                        GetNoCaptcha(source);
                }
                catch (Exception)
                {
                    //Informer.RaiseOnResultReceived(ex);
                }
            }).Wait();
        }

        private void ButtonIsEnabled(bool @bool)
        {
            ButtonStart.IsEnabled = @bool;
            ButtonCodes.IsEnabled = @bool;
            ButtonProx.IsEnabled = @bool;
            ButtonStop.IsEnabled = !@bool;
        }

        private static void DeleteBrowsers(int num)
        {
            for (var i = 0; i < num; i++)
            {
                try
                {
                    var t = Utils.DriverList.Last();
                    Utils.DriverList.Remove(t);
                    t.IsEnabled = false;
                    t.Num = 0;
                    t.Driver.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }

        private static void LoadBrowsers(double browsersNum)
        {
            var count = Utils.DriverList.Count;
            for (var i = count; i < browsersNum + count; i++)
            {
                try
                {
                    var options = new ChromeOptions();
                    if (File.Exists("extension.crx"))
                        options.AddExtension(Path.GetFullPath("extension.crx"));
                    else options.AddArgument("-incognito");

                    var driver = new ChromeDriver(options);
                    var driverStruct = new DriverStruct
                    {
                        Driver = driver,
                        Num = i,
                        IsEnabled = true
                    };
                    Utils.DriverList.Add(driverStruct);
                }
                catch (Exception ex)
                {
                    Informer.RaiseOnResultReceived(ex);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}