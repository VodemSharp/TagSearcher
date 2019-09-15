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
using TagSearcher.Instagram.Account;
using Newtonsoft.Json;

namespace TagSearcher.Instagram
{
    public enum QueryType
    {
        Info, Followers, Friends, None
    }

    public class InParser : IDisposable
    {
        private IWebDriver Browser { get; set; }

        public string Id { get; set; }
        public string ProxyHost { get; set; }
        public string ProxyPort { get; set; }
        public string Data { get; set; }
        public QueryType QueryType { get; set; }

        public InParser(string id, string proxy_host, string proxy_port, QueryType queryType)
        {
            Id = id;
            ProxyHost = proxy_host;
            ProxyPort = proxy_port;
            QueryType = queryType;

            Data = String.Format("{0}/{1}", ProxyHost, ProxyPort);

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
        }

        public string[] CountLikes(string tag)
        {
            Data = String.Format("{0}/{1}/{2}", tag, ProxyHost, ProxyPort);
            string url = String.Format("https://www.instagram.com/explore/tags/{0}", tag);

            FileHelper.WriteToLog("Searching tag...", Data);
            Browser.Navigate().GoToUrl(url);

            Browser.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            IEnumerable<IWebElement> webElements = Browser.FindElements(By.CssSelector(".v1Nh3.kIKUG._bz0w a"));

            if (webElements.Count() == 0)
            {
                FileHelper.WriteToLog("Searching tag complete!", Data);
                return new string[] { "0" };
            }

            url = webElements.First().GetAttribute("href");

            Browser.Navigate().GoToUrl(url);

            webElements = Browser.FindElements(By.CssSelector("._0mzm-.sqdOP.yWX7d._8A5w5 span"));

            FileHelper.WriteToLog("Searching tag complete!", Data);
            if (webElements.Count() == 0)
            {
                return new string[] { "0" };
            }
            //pi_author
            switch (QueryType)
            {
                case QueryType.Info:
                    return new string[] { Convert.ToInt32(webElements.First().Text.Replace(" ", "")).ToString(), JsonConvert.SerializeObject(Parse(Browser.FindElement(By.CssSelector("a.FPmhX.notranslate.nJAzx")).Text)) };
                case QueryType.None:
                default:
                    return new string[] { Convert.ToInt32(webElements.First().Text.Replace(" ", "")).ToString() };
            }
        }

        public InstagramAccount Parse(string username)
        {
            InstagramAccount instagramAccount = new InstagramAccount();

            Browser.Navigate().GoToUrl(String.Format("https://www.instagram.com/{0}", username));

            WebDriverWait ww = new WebDriverWait(Browser, TimeSpan.FromSeconds(10));
            instagramAccount.MainPhoto = ww.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("._6q-tv"))).GetAttribute("src");

            ww.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".g47SY.lOXF2")));
            List<IWebElement> webElements = Browser.FindElements(By.CssSelector(".g47SY.lOXF2")).ToList();
            instagramAccount.CountPosts = webElements[0].Text;
            instagramAccount.CountReaders = webElements[1].Text;
            instagramAccount.CountFollowers = webElements[2].Text;

            instagramAccount.Username = username;

            return instagramAccount;
        }

        public void Dispose()
        {
            Browser.Dispose();
        }
    }
}
