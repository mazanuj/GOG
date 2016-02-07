using System;

namespace GogLib.DataTypes
{
    public class CaptchaStruct
    {
        public string Answer { get; set; }
        public string Challenge { get; set; }
        public DateTime Date { get; set; }
    }
}