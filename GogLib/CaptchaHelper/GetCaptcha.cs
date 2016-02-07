using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Akumu.Antigate;
using GogLib.DataTypes;
using GogLib.Http;
using GogLib.Utilities;
using OpenQA.Selenium;

namespace GogLib.CaptchaHelper
{
    public static class GetCaptcha
    {
        private static readonly object Lock = new object();
        private static Queue GetCaptchaQueue = new Queue();
        private static readonly object Locker = new object();
        private static readonly object LockerQueue = new object();
        private static IWebDriver WebDriverQueue = Utils.WebDriverQueue;
        private const int PictureOpen = 3000;

        public static async Task<string> GetBalance(string agKey, string service)
        {
            return await Task.Run(() =>
            {
                var answ = string.Empty;
                try
                {
                    var req = RequestHelper.GetRequest;
                    answ = req.Get($"http://{service}/res.php?key={agKey}&action=getbalance").ToString();
                    return answ;
                }
                catch (Exception)
                {
                }
                return answ;
            });
        }

        public static async Task<CaptchaStruct> GetNoCaptcha(string agKey, DriverStruct drStr)
        {
            try
            {
                var challenge = string.Empty;
                while (true)
                {
                    var WebDriver = drStr.Driver;
                    WebDriver.Manage().Cookies.DeleteAllCookies();
                    try
                    {
                        if (!Utils.IsPermit)
                        {
                            return new CaptchaStruct
                            {
                                Answer = "Error message: Stopped",
                                Challenge = string.Empty,
                                Date = DateTime.Now
                            };
                        }

                        WebDriver.Navigate().GoToUrl("http://www.gog.com/redeem");
                        await Task.Delay(PictureOpen);

                        try
                        {
                            WebDriver.SwitchTo()
                            .Frame(WebDriver.FindElement(By.XPath("//iframe[@title='recaptcha widget']")));
                        }
                        catch (Exception)
                        {
                            WebDriver.SwitchTo()
                            .Frame(WebDriver.FindElement(By.XPath("//iframe[@title='виджет reCAPTCHA']")));
                        }
                        
                        WebDriver.FindElement(By.XPath("//div[@class='recaptcha-checkbox-checkmark']")).Click();
                        WebDriver.SwitchTo().DefaultContent();

                        await Task.Delay(PictureOpen);
                        try
                        {
                            WebDriver.SwitchTo()
                            .Frame(WebDriver.FindElement(By.XPath("//iframe[@title='recaptcha challenge']")));
                        }
                        catch (Exception)
                        {
                            WebDriver.SwitchTo()
                            .Frame(WebDriver.FindElement(By.XPath("//iframe[@title='проверка recaptcha']")));
                        }                        

                        while (true)
                        {
                            if (!Utils.IsPermit)
                            {
                                return new CaptchaStruct
                                {
                                    Answer = "Error message: Stopped",
                                    Challenge = string.Empty,
                                    Date = DateTime.Now
                                };
                            }
                            var answ = await GetCaptchaResult(WebDriver, agKey);

                            if (string.IsNullOrEmpty(answ))
                            {
                                if (!Utils.IsPermit)
                                {
                                    return new CaptchaStruct
                                    {
                                        Answer = "Error message: Stopped",
                                        Challenge = string.Empty,
                                        Date = DateTime.Now
                                    };
                                }
                                continue;
                            }

                            var picNums = Regex.Matches(answ, @"\d+");
                            var imageSelectors =
                                WebDriver.FindElements(By.XPath("//div[@class='rc-image-tile-wrapper']"));

                            try
                            {
                                foreach (var z in from object x in picNums select int.Parse(x.ToString()) - 1)
                                    imageSelectors[z].Click();
                            }
                            catch (Exception)
                            {
                                challenge = WebDriver.FindElement(By.Id("recaptcha-token"))
                                    .GetAttribute("value");
                                break;
                            }

                            WebDriver.FindElement(By.Id("recaptcha-verify-button")).Click();
                            await Task.Delay(2000);

                            var incorrect =
                                WebDriver.FindElement(
                                    By.XPath("//div[@class='rc-imageselect-incorrect-response']"))
                                    .Text;
                            var more =
                                WebDriver.FindElement(
                                    By.XPath("//div[@class='rc-imageselect-error-select-more']"))
                                    .Text;
                            var one =
                                WebDriver.FindElement(By.XPath("//div[@class='rc-imageselect-error-select-one']"))
                                    .Text;

                            if (incorrect == "" && more == "" && one == "")
                            {
                                challenge = WebDriver.FindElement(By.Id("recaptcha-token"))
                                    .GetAttribute("value");
                                break;
                            }

                            if (one != "" || more != "")
                                break;
                        }

                        if (string.IsNullOrEmpty(challenge))
                            continue;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("no such element"))
                        {
                            challenge = string.Empty;
                            continue;
                        }

                        if (ex.Message.Contains("unexpected alert open"))
                            throw new Exception("Reload");
                        if (ex.Message.Contains("Antigate server did not respond"))
                            throw new Exception(ex.Message);
                        if (ex.Message.Contains("Индекс за пределами диапазона"))
                        {
                            //throw new Exception("Wrong captcha type");
                        }
                        else if (ex.Message.Contains("Произошла одна или несколько ошибок") ||
                                 ex.Message.Contains("One or more errors occurred"))
                        {
                            if (!ex.InnerException.Message.Contains("Captcha unsolvable") &&
                                !ex.InnerException.Message.Contains("Image file size too big"))
                                throw new Exception(ex.InnerException.Message);
                        }
                        else throw new Exception(ex.Message);
                    }
                }
                return new CaptchaStruct {Answer = string.Empty, Challenge = challenge, Date = DateTime.Now};
            }
            catch (Exception ex)
            {
                return new CaptchaStruct
                {
                    Answer = $"Error message: {ex.Message}  => Browser №{drStr.Num + 1}",
                    Challenge = "",
                    Date = DateTime.Now
                };
            }
        }

        private static bool IsElementPresent(By by, ISearchContext webDriver)
        {
            try
            {
                webDriver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        private static async Task<string> GetCaptchaResult(ISearchContext webDriver, string agKey)
        {
            try
            {
                byte[] mainImgBytes;
                IWebElement type;

                var cap = new AntiCaptcha(agKey)
                {
                    CheckDelay = 2000,
                    SlotRetry = 0,
                    SlotRetryDelay = 0
                };
                cap.Parameters.Set("recaptcha", "1");

                try
                {
                    type = webDriver.FindElement(By.XPath("//img[@class='rc-image-tile-33']"));
                    mainImgBytes = await GetMainImg(type.GetAttribute("src") /*, false*/);
                }
                catch (Exception)
                {
                    try
                    {
                        type = webDriver.FindElement(By.XPath("//img[@class='rc-image-tile-42']"));
                        mainImgBytes = await GetMainImg(type.GetAttribute("src") /*, true*/);
                    }
                    catch (Exception)
                    {
                        type = webDriver.FindElement(By.XPath("//img[@class='rc-image-tile-44']"));
                        mainImgBytes = await GetMainImg(type.GetAttribute("src") /*, true*/);
                    }
                }

                if (mainImgBytes == null)
                    throw new Exception("Image file size too big");

                if (!IsElementPresent(By.XPath("//div[@class='rc-imageselect-desc-no-canonical']"), webDriver))
                {
                    //Text instructions
                    var descPlain =
                        webDriver.FindElement(
                            By.XPath("//div[@class='rc-imageselect-desc']")).GetAttribute("innerHTML");
                    cap.Parameters.Set("textinstructions", Regex.Match(descPlain, @"(?<=<strong>).+(?=</strong>)").Value);

                    //Candidate image Base64
                    var candidateSrcBase64 =
                        webDriver.FindElement(By.Id("rc-imageselect-candidate"))
                            .FindElement(By.TagName("img"))
                            .GetAttribute("src");
                    cap.Parameters.Set("imginstructions",
                        candidateSrcBase64.Replace("data:image/jpeg;base64,", string.Empty));
                }
                else
                {
                    //Text instructions
                    var descPlain =
                        webDriver.FindElement(
                            By.XPath("//div[@class='rc-imageselect-desc-no-canonical']")).GetAttribute("innerHTML");
                    cap.Parameters.Set("textinstructions", Regex.Match(descPlain, @"(?<=<strong>).+(?=</strong>)").Value);
                }

                var res = await cap.GetAnswer(mainImgBytes, Utils.CancelToken.Token);
                if (string.IsNullOrEmpty(res))
                    cap.FalseCaptcha();

                return res;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("no such element"))
                    throw new Exception("Wrong captcha type");
                throw new Exception(ex.Message);
            }
        }

        private static async Task<byte[]> GetMainImg(string lnk)
        {
            var resp = await new WebClient().DownloadDataTaskAsync(lnk);
            return resp.Length < 100000 ? resp : null;
        }
    }
}