using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace TagSearcher.Proxy
{
    public class ProxyChecker
    {
        private IWebDriver Browser { get; set; }

        private readonly string _proxyHost;
        private readonly string _proxyPort;

        public ProxyChecker(string proxy_host, string proxy_port)
        {
            _proxyHost = proxy_host;
            _proxyPort = proxy_port;

#pragma warning disable CS0618 // Type or member is obsolete

            PhantomJSDriverService service = PhantomJSDriverService.CreateDefaultService(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            PhantomJSOptions options = null;

            if (_proxyHost != "0.0.0.0" && _proxyPort != "0")
            {
                OpenQA.Selenium.Proxy proxy = new OpenQA.Selenium.Proxy();
                proxy.HttpProxy = String.Format(_proxyHost + ":" + _proxyPort);
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

        public string Check()
        {
            string url = "https://2ip.ru/";

            Browser.Navigate().GoToUrl(url);

            //Browser.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            //IEnumerable<IWebElement> webElements = Browser.FindElements(By.CssSelector(".v1Nh3.kIKUG._bz0w a"));

            //if (webElements.Count() == 0)
            //{
            //    return 0;
            //}

            //url = webElements.First().GetAttribute("href");

            //Browser.Navigate().GoToUrl(url);
            //Thread.Sleep(1000);
            //string source = Browser.PageSource;

            //webElements = Browser.FindElements(By.CssSelector("._0mzm-.sqdOP.yWX7d._8A5w5 span"));

            //if (webElements.Count() == 0)
            //{
            //    return 0;
            //}

            return Browser.PageSource;
        }
    }
}
