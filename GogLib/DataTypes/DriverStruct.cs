using OpenQA.Selenium;

namespace GogLib.DataTypes
{
    public class DriverStruct
    {
        public IWebDriver Driver { get; set; }
        public bool IsEnabled { get; set; }
        public int Num { get; set; }
    }
}