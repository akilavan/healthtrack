using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace coursework_2.Models
{
    public static class RecaptchaService
    {
        private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

        public static bool Enabled
        {
            get { return SettingsService.Current.RecaptchaEnabled; }
        }

        public static string SiteKey
        {
            get { return SettingsService.Current.RecaptchaSiteKey; }
        }

        public static bool Verify(string response, string remoteIp)
        {
            SystemSetting settings = SettingsService.Current;

            if (!settings.RecaptchaEnabled) return true;
            if (string.IsNullOrWhiteSpace(response)) return false;
            if (string.IsNullOrWhiteSpace(settings.RecaptchaSecretKey)) return false;

            try
            {
                string form = "secret=" + Uri.EscapeDataString(settings.RecaptchaSecretKey.Trim())
                    + "&response=" + Uri.EscapeDataString(response.Trim())
                    + "&remoteip=" + Uri.EscapeDataString(remoteIp ?? "");

                var request = (HttpWebRequest)WebRequest.Create(VerifyUrl);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = 10000;

                byte[] body = Encoding.UTF8.GetBytes(form);
                request.ContentLength = body.Length;

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(body, 0, body.Length);
                }

                using (var httpResponse = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var data = new JavaScriptSerializer()
                        .Deserialize<Dictionary<string, object>>(reader.ReadToEnd());

                    return data != null
                        && data.ContainsKey("success")
                        && Convert.ToBoolean(data["success"]);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
