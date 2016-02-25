using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xNet.Net;

namespace Akumu.Antigate
{
    /// <summary>
    /// Класс реализует работу с сервисом rucaptcha.com
    /// 
    /// </summary>
    public class AntiCaptcha
    {
        /// <summary>
        /// Set/Get Задержка проверки готовности капчи. Стандартно: 15000. (15 сек.)
        /// 
        /// </summary>
        public int CheckDelay = 15000;

        /// <summary>
        /// Set/Get Кол-во попыток проверки готовности капчи. Стандартно: 30
        /// 
        /// </summary>
        public int CheckRetryCount = 30;

        /// <summary>
        /// Set/Get кол-во попыток получения нового слота. Стандартно: 3
        /// 
        /// </summary>
        public int SlotRetry = 3;

        /// <summary>
        /// Set/Get Задержка повторной попытки получения слота на Antigate. Стандартно: 1000
        /// 
        /// </summary>
        public int SlotRetryDelay = 1000;

        /// <summary>
        /// Сервис антикапчи. Стандартно: rucaptcha.com
        /// 
        /// </summary>
        public string ServiceProvider = "rucaptcha.com";

        /// <summary>
        /// Коллекция дополнительных параметров для API запросов.
        /// 
        /// </summary>
        public readonly ParamsContainer Parameters;

        private readonly string Key;
        private string CaptchaId;

        /// <summary>
        /// Инициализирует объект AntiCapcha
        /// 
        /// </summary>
        /// <param name="key">Ваш секретный API ключ</param>
        public AntiCaptcha(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Antigate Key is null or empty");
            Parameters = new ParamsContainer();
            Key = key;
        }

        /// <summary>
        /// Отправляет на антигейт файл прочитанный с диска.
        /// 
        /// </summary>
        /// <param name="ImageFilePath">Путь к файлу изображения</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>
        /// Разгаданный текст капчи или [null] в случае отсутствия свободных слотов или превышения времени ожидания
        /// </returns>
        public async Task<string> GetAnswer(string ImageFilePath, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(ImageFilePath))
                throw new ArgumentNullException(nameof(ImageFilePath));
            if (!File.Exists(ImageFilePath))
                throw new ArgumentException("Image file does not exist");
            byte[] ImageData;
            try
            {
                using (var image = Image.FromFile(ImageFilePath))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        image.Save(memoryStream, ImageFormat.Jpeg);
                        ImageData = memoryStream.ToArray();
                    }
                }
            }
            catch
            {
                throw new ArgumentException("Image has unknown file format");
            }
            return await GetAnswer(ImageData, ct);
        }

        /// <summary>
        /// Отправляет на антигейт изображение объекта Image
        /// 
        /// </summary>
        /// <param name="Img"/>
        /// <param name="ct">CancellationToken</param>
        /// <returns/>
        public async Task<string> GetAnswer(Image Img, CancellationToken ct)
        {
            byte[] ImageData;
            using (var memoryStream = new MemoryStream())
            {
                Img.Save(memoryStream, ImageFormat.Jpeg);
                ImageData = memoryStream.ToArray();
            }
            return await GetAnswer(ImageData, ct);
        }

        /// <summary>
        /// Отправляет на антигейт массив данных изображения в формате PNG.
        /// 
        /// </summary>
        /// <param name="ImageData">Массив данных изображения (PNG)</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>
        /// Разгаданный текст капчи или [null] в случае отсутствия свободных слотов или превышения времени ожидания
        /// </returns>
        public async Task<string> GetAnswer(byte[] ImageData, CancellationToken ct)
        {
            if (ImageData == null || ImageData.Length == 0)
                throw new ArgumentException("Image data array is empty");
            var num = SlotRetry;
            CaptchaId = null;
            string str;
            var req = new HttpRequest();

            while (true)
            {
                if (ct.IsCancellationRequested)
                    ct.ThrowIfCancellationRequested();

                req.AddField("method", "post");
                req.AddField("key", Key);
                req.AddField("soft_id", "1151");
                req.AddFile("file", "image.jpg", "image/jpeg", ImageData);
                if (Parameters.Count > 0)
                {
                    foreach (Param obj in Parameters.GetParams())
                        req.AddField(obj.Key, obj.Value, Encoding.UTF8);
                }

                try
                {
                    str = req.Post($"http://{ServiceProvider}/in.php").ToString();
                }
                catch
                {
                    throw new WebException("Antigate server did not respond");
                }
                if (str.Equals("ERROR_NO_SLOT_AVAILABLE", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (num - 1 > 0)
                    {
                        --num;
                        Thread.Sleep(SlotRetryDelay);
                    }
                    else break;
                }
                else
                    goto label_22;
            }
            throw new WebException(str);

            label_22:
            if (str.StartsWith("ERROR_", StringComparison.InvariantCultureIgnoreCase))
                throw new AntigateErrorException(
                    (AntigateError) Enum.Parse(typeof (AntigateError), str.Substring(6)));
            try
            {
                CaptchaId = str.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries)[1];
            }
            catch
            {
                throw new WebException("Antigate answer is in unknown format or malformed");
            }
            for (var index = 0; index < CheckRetryCount; ++index)
            {
                await Task.Delay(CheckDelay, ct);

                str = req.Get($"http://{ServiceProvider}/res.php?key={Key}&action=get&id={CaptchaId}").ToString();
                if (str.Equals("CAPCHA_NOT_READY", StringComparison.InvariantCultureIgnoreCase)) continue;

                if (str.StartsWith("ERROR_", StringComparison.InvariantCultureIgnoreCase))
                    throw new AntigateErrorException(
                        (AntigateError) Enum.Parse(typeof (AntigateError), str.Substring(6)));
                var strArray = str.Split('|');
                if (strArray[0].Equals("OK", StringComparison.InvariantCultureIgnoreCase))
                    return strArray[1];
            }
            return null;
        }

        /// <summary>
        /// Оповещаем антигейт о том, что последняя отправленная капча была не верной
        /// 
        /// </summary>
        public void FalseCaptcha()
        {
            if (string.IsNullOrEmpty(CaptchaId))
                throw new ArgumentNullException("Captcha is not solved yet. Nothing to report.");
            try
            {
                new HttpRequest().Get($"http://{ServiceProvider}/res.php?key={Key}&action=reportbad&id={CaptchaId}")
                    .None();
            }
            catch
            {
                throw new WebException("Error sending the request");
            }
        }
    }
}