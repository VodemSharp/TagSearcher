using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.PhantomJS;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using OpenQA.Selenium.Support.Extensions;
using System.Reflection;
using System.Threading;
using TagSearcher.Core.Helpers;
using TagSearcher.VK.Account;
using Newtonsoft.Json;

namespace TagSearcher.VK
{
    public enum QueryType
    {
        Info, Friends, Followers, None
    }

    public class VKParser : IDisposable
    {
        private IWebDriver Browser { get; set; }

        public string Id { get; set; }
        public string ProxyHost { get; set; }
        public string ProxyPort { get; set; }
        public string VK_Login { get; set; }
        public string VK_Password { get; set; }
        public QueryType QueryType { get; set; }
        public string Data { get; set; }

        public VKParser(string id, string proxy_host, string proxy_port, string vk_login, string vk_password, QueryType queryType)
        {
            Id = id;
            ProxyHost = proxy_host;
            ProxyPort = proxy_port;
            VK_Login = vk_login;
            VK_Password = vk_password;
            QueryType = queryType;

            Data = String.Format("{0}/{1}/{2}/{3}", ProxyHost, ProxyPort, VK_Login, VK_Password);

#pragma warning disable CS0618 // Type or member is obsolete

            PhantomJSDriverService service = PhantomJSDriverService.CreateDefaultService(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            PhantomJSOptions options = null;

            if (ProxyHost != "0.0.0.0" && ProxyPort != "0")
            {
                OpenQA.Selenium.Proxy proxy = new OpenQA.Selenium.Proxy();
                proxy.HttpProxy = String.Format(ProxyHost + ":" + ProxyPort);
                proxy.Kind = ProxyKind.Manual;

                service.ProxyType = "http";
                service.Proxy = proxy.HttpProxy;

                options = new PhantomJSOptions { Proxy = proxy };
            }

            service.HideCommandPromptWindow = true;
            service.IgnoreSslErrors = true;
            service.SslProtocol = "any";
            service.WebSecurity = false;
            service.LocalToRemoteUrlAccess = true;
            service.LoadImages = false;

            if (options == null)
            {
                Browser = new OpenQA.Selenium.PhantomJS.PhantomJSDriver(service);
            }
            else
            {
                Browser = new OpenQA.Selenium.PhantomJS.PhantomJSDriver(service, options);
            }

            // login

            FileHelper.WriteToLog("Login...", Data);

            Browser.Navigate().GoToUrl("https://m.vk.com/");

            WebDriverWait ww = new WebDriverWait(Browser, TimeSpan.FromSeconds(10));
            IWebElement SearchInput = ww.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("input[name='email']")));

            SearchInput.SendKeys(VK_Login);
            SearchInput = Browser.FindElement(By.CssSelector("input[name='pass']"));
            SearchInput.SendKeys(VK_Password + OpenQA.Selenium.Keys.Enter);

            FileHelper.WriteToLog("Login complete!", Data);
        }

        public string[] CountLikes(string tag)
        {
            int countLikes = 0;
            Data = String.Format("{0}/{1}/{2}/{3}/{4}", tag, ProxyHost, ProxyPort, VK_Login, VK_Password);
            string url = String.Format("https://m.vk.com/search?c[section]=auto&c[q]=%23{0}", tag);

            FileHelper.WriteToLog("Searching tag...", Data);
            Browser.Navigate().GoToUrl(url);

            Browser.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            IEnumerable<IWebElement> webElements = Browser.FindElements(By.CssSelector(".v_like"));

            FileHelper.WriteToLog("Searching tag complete!", Data);
            if (webElements.Count() != 0)
            {
                countLikes = Convert.ToInt32(webElements.First().Text.Replace(" ", ""));
            }

            switch (QueryType)
            {
                case QueryType.Info:
                    return new string[] { countLikes.ToString(), JsonConvert.SerializeObject(Parse(Browser.FindElements(By.CssSelector("a[data-post-owner-type='user']")).First().GetAttribute("href").Split(new char[] { '?' })[0].Replace("https://m.vk.com/", ""))) };
                default:
                    return new string[] { countLikes.ToString() };
            }
        }

        public VKAccount Parse(string id)
        {
            VKAccount vkAccount = new VKAccount
            {
                InfoItems = new List<InfoItem> { }
            };
            Browser.Navigate().GoToUrl(String.Format("https://m.vk.com/{0}", id));

            List<IWebElement> tempWebElements;

            tempWebElements = Browser.FindElements(By.CssSelector(".pp_cont > .op_header")).ToList();
            if (tempWebElements.Count != 0)
            {
                vkAccount.NameSurname = tempWebElements.First().Text;
            }

            tempWebElements = Browser.FindElements(By.CssSelector(".pp_last_activity_offline_text")).ToList();
            if (tempWebElements.Count != 0)
            {
                vkAccount.LastActivity = tempWebElements.First().Text;
            }

            tempWebElements = Browser.FindElements(By.CssSelector(".pp_info")).ToList();
            if (tempWebElements.Count != 0)
            {
                vkAccount.Info = tempWebElements.First().Text;
            }

            tempWebElements = Browser.FindElements(By.CssSelector(".pp_status")).ToList();
            if (tempWebElements.Count != 0)
            {
                vkAccount.Status = tempWebElements.First().Text;
            }

            Browser.Navigate().GoToUrl(String.Format("https://m.vk.com/{0}?act=info", id));

            List<IWebElement> elements = Browser.FindElements(By.CssSelector(".pinfo_row")).ToList();
            InfoItem infoItem;

            foreach (IWebElement webElement in elements)
            {
                if (webElement.FindElements(By.CssSelector("dt")).Count != 0)
                {
                    infoItem = new InfoItem
                    {
                        Name = webElement.FindElement(By.CssSelector("dt")).Text
                    };

                    tempWebElements = webElement.FindElements(By.CssSelector("dd > a")).ToList();
                    if (tempWebElements.Count > 0)
                    {
                        infoItem.Value = String.Join(", ", tempWebElements.Select(t=>t.Text));
                    }
                    tempWebElements = webElement.FindElements(By.CssSelector("dd")).ToList();
                    if (tempWebElements.Count > 1)
                    {
                        if (String.IsNullOrEmpty(infoItem.Value))
                        {
                            infoItem.Value = String.Join(", ", tempWebElements.Select(t => t.Text));
                        }
                        else
                        {
                            infoItem.Value += ", ";
                            infoItem.Value += String.Join(", ", tempWebElements.Select(t => t.Text));
                        }
                    }

                    vkAccount.InfoItems.Add(infoItem);
                }
            }

            return vkAccount;
        }

        public void Dispose()
        {
            Browser.Dispose();
        }
    }
}
