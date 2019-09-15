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
using TagSearcher.Facebook.Account;
using TagSearcher.Core.Helpers;
using Newtonsoft.Json;

namespace TagSearcher.Facebook
{
    public enum QueryType
    {
        Info, Followers, Friends, None
    }

    public class FbParser : IDisposable
    {
        private IWebDriver Browser { get; set; }

        public string ProxyHost { get; set; }
        public string ProxyPort { get; set; }
        public string FB_Login { get; set; }
        public string FB_Password { get; set; }
        public string Data { get; set; }
        public QueryType QueryType { get; set; }

        [Obsolete]
        public FbParser(string proxy_host, string proxy_port, string fb_login, string fb_password, QueryType queryType)
        {
            ProxyHost = proxy_host;
            ProxyPort = proxy_port;
            FB_Login = fb_login;
            FB_Password = fb_password;
            QueryType = queryType;

            Data = String.Format("{0}/{1}/{2}/{3}", ProxyHost, ProxyPort, FB_Login, FB_Password);

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

#pragma warning restore CS0618 // Type or member is obsolete

            // login

            FileHelper.WriteToLog("Login...", Data);

            Browser.Navigate().GoToUrl("http://m.facebook.com");
            WebDriverWait ww = new WebDriverWait(Browser, TimeSpan.FromSeconds(10));

            IWebElement SearchInput = ww.Until(ExpectedConditions.ElementIsVisible(By.Id("m_login_email")));

            SearchInput.SendKeys(FB_Login);
            SearchInput = Browser.FindElement(By.Id("m_login_password"));
            SearchInput.SendKeys(FB_Password + OpenQA.Selenium.Keys.Enter);

            string source = Browser.PageSource;

            ww.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("._2pis button"))).Click();

            FileHelper.WriteToLog("Login complete!", Data);

            //
        }

        public string[] CountLikes(string tag)
        {
            string countLikes = "0";
            Data = String.Format("{0}/{1}/{2}/{3}/{4}", tag, ProxyHost, ProxyPort, FB_Login, FB_Password);
            string url = String.Format("http://m.facebook.com/search/top/?q=%23{0}&epa=SEARCH_BOX", tag);

            FileHelper.WriteToLog("Searching tag...", Data);
            Browser.Navigate().GoToUrl(url);

            string source = Browser.PageSource;

            IEnumerable<IWebElement> webElements = Browser.FindElements(By.CssSelector("._1g06"));

            FileHelper.WriteToLog("Searching tag complete!", Data);

            if (webElements.Count() != 0)
            {
                countLikes = webElements.First().Text;
            }

            webElements = Browser.FindElements(By.CssSelector("a[data-sigil='feed-ufi-trigger']"));
            if (webElements.Count() != 0)
            {
                switch (QueryType)
                {
                    case QueryType.Info:
                        return new string[] { countLikes, JsonConvert.SerializeObject(Parse(webElements.First().GetAttribute("href").Split(new string[] { "&id", "&" }, StringSplitOptions.RemoveEmptyEntries).First(x=>x[0] == '=').Replace("=", ""))) };
                    default:
                        return new string[] { countLikes };
                }
            }

            return new string[] { countLikes };
        }

        [Obsolete]
        public FacebookAccount Parse(string id)
        {
            Data = String.Format("{0}/{1}/{2}/{3}/{4}", ProxyHost, ProxyPort, FB_Login, FB_Password, id);
            FacebookAccount facebookAccount = new FacebookAccount();

            FileHelper.WriteToLog("User search...", Data);

            string url = "http://m.facebook.com/profile.php?id=" + id;

            Browser.Navigate().GoToUrl(url);

            facebookAccount.Id = id;

            string source = Browser.PageSource;
            WebDriverWait ww = new WebDriverWait(Browser, TimeSpan.FromSeconds(10));
            facebookAccount.Name = ww.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("#cover-name-root ._6x2x"))).Text;

            string moreInfoUrl = Browser.FindElements(By.CssSelector("._5s61._5cn0._5i2i._52we > div > a")).Last().GetAttribute("href");

            string friendsUrl = "";

            foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("._2jl2 > a")))
            {
                if (webElement.GetAttribute("href").Contains("friends"))
                {
                    friendsUrl = webElement.GetAttribute("href");
                    break;
                }
            }

            string followersUrl = "";

            foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("._5s61._5cn0._5i2i._52we a")))
            {
                if (webElement.GetAttribute("href").Contains("followers"))
                {
                    followersUrl = webElement.GetAttribute("href");
                    break;
                }
            }

            Browser.Navigate().GoToUrl(moreInfoUrl);
            System.Threading.Thread.Sleep(3000);
            IJavaScriptExecutor js = (IJavaScriptExecutor)Browser;
            int tempLength;
            for (; ; )
            {
                tempLength = Browser.FindElements(By.CssSelector("._4_-j.timeline > div")).Count;
                js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 1000);");

                for (int i = 0; i < 100; i++)
                {
                    if (Browser.FindElements(By.CssSelector("._4_-j.timeline > div")).Count != tempLength)
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(50);
                }

                List<LogEntry> logEntries = Browser.Manage().Logs.GetLog("browser").ToList();

                if (Browser.FindElements(By.CssSelector("._4_-j.timeline > div")).Count == tempLength)
                {
                    break;
                }
            }

            FileHelper.WriteToLog("User search complete!", Data);

            IWebElement temp = null;

            if (QueryType == QueryType.Info)
            {

                #region Work

                FileHelper.WriteToLog("Work search...", Data);

                facebookAccount.Works = new List<Work> { };
                Work tempWork;
                foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("#work > div > div")))
                {
                    try
                    {
                        temp = webElement.FindElement(By.ClassName("_4e81"));
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    tempWork = new Work
                    {
                        Name = temp.Text,
                        Link = temp.GetAttribute("href"),
                    };

                    if (webElement.FindElements(By.ClassName("_5p1r")).Count != 0)
                    {
                        tempWork.Description = webElement.FindElement(By.CssSelector("._5p1r ._52jb")).FindElement(By.TagName("span")).Text;
                    }

                    if (webElement.FindElements(By.ClassName("_52ja")).Count != 0)
                    {
                        tempWork.Position = webElement.FindElement(By.ClassName("_52ja")).Text;
                    }
                    tempWork.Items = webElement.FindElements(By.ClassName("_52j9")).Select(e => e.Text).ToList();

                    facebookAccount.Works.Add(tempWork);
                }

                FileHelper.WriteToLog("Work search complete!", Data);

                #endregion

                #region Education

                FileHelper.WriteToLog("Education search...", Data);

                facebookAccount.Educations = new List<Education> { };
                Education tempEducation;
                foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("#education > div > div")))
                {
                    try
                    {
                        temp = webElement.FindElement(By.ClassName("_4e81"));
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    tempEducation = new Education
                    {
                        Name = temp.Text,
                        Link = temp.GetAttribute("href"),
                    };

                    if (webElement.FindElements(By.ClassName("_5p1r")).Count != 0)
                    {
                        tempEducation.Description = webElement.FindElement(By.CssSelector("._5p1r ._52jb")).FindElement(By.TagName("span")).Text;
                    }

                    foreach (IWebElement tempElement in webElement.FindElements(By.CssSelector("._52ja")))
                    {
                        if (tempElement.FindElements(By.TagName("span")).Count == 0)
                        {
                            tempEducation.InsideEducation = tempElement.Text;
                            break;
                        }
                    }

                    if (webElement.FindElements(By.CssSelector("._52ja > span")).Count != 0)
                    {
                        tempEducation.Specialties = webElement.FindElement(By.CssSelector("._52ja > span")).Text.Split(new string[] { "<span aria-hidden=\"true\"> · </span>" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    }

                    tempEducation.Items = webElement.FindElements(By.ClassName("_52j9")).Select(e => e.Text).ToList();

                    facebookAccount.Educations.Add(tempEducation);
                }

                FileHelper.WriteToLog("Education search complete!", Data);

                #endregion

                #region Living

                FileHelper.WriteToLog("Living search...", Data);

                facebookAccount.Cities = new List<City> { };
                City tempCity;
                foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("#living > div > div")))
                {
                    try
                    {
                        temp = webElement.FindElement(By.TagName("header"));
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    tempCity = new City
                    {
                        Name = temp.FindElements(By.TagName("h4"))[0].Text,
                        Description = temp.FindElements(By.TagName("h4"))[1].Text,
                    };

                    if (webElement.FindElements(By.CssSelector("._5b6s")).Count != 0)
                    {
                        tempCity.Link = webElement.FindElement(By.CssSelector("._5b6s")).GetAttribute("href");
                    }


                    facebookAccount.Cities.Add(tempCity);
                }

                FileHelper.WriteToLog("Living search complete!", Data);

                #endregion

                #region ContactInfo

                FileHelper.WriteToLog("Contact info search...", Data);

                facebookAccount.ContactInformations = new List<ContactInformation> { };
                ContactInformation tempContactInformation;
                foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("#contact-info > div > div")))
                {
                    tempContactInformation = new ContactInformation
                    {
                        Name = webElement.FindElement(By.ClassName("_52jd")).Text,
                        Value = webElement.FindElement(By.ClassName("_5cdv")).Text,
                    };

                    facebookAccount.ContactInformations.Add(tempContactInformation);
                }

                FileHelper.WriteToLog("Contact info search complete!", Data);

                #endregion

                #region BasicInfo

                FileHelper.WriteToLog("Basic info search...", Data);

                facebookAccount.BasicInfos = new List<BasicInfo> { };
                foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("#basic-info > div > div")))
                {
                    facebookAccount.BasicInfos.Add(new BasicInfo
                    {
                        Name = webElement.FindElement(By.ClassName("_52jd")).Text,
                        Value = webElement.FindElement(By.ClassName("_5cdv")).Text,
                    });
                }

                FileHelper.WriteToLog("Basic info search complete!", Data);

                #endregion

                #region Relationship

                FileHelper.WriteToLog("Relationship search...", Data);

                if (Browser.FindElements(By.CssSelector("#relationship ._5cdt")).Count != 0)
                {
                    facebookAccount.Relationship = Browser.FindElement(By.CssSelector("#relationship ._5cdt")).Text;
                }
                else
                {
                    facebookAccount.Relationship = String.Empty;
                }

                FileHelper.WriteToLog("Relationship search complete!", Data);

                #endregion

                #region Family

                FileHelper.WriteToLog("Family search...", Data);

                facebookAccount.Family = new List<FamilyMember> { };
                FamilyMember tempFamilyMember;
                foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("#family > div > div")))
                {
                    tempFamilyMember = new FamilyMember { };

                    if (webElement.FindElements(By.ClassName("_52jb")).Count != 0)
                    {
                        tempFamilyMember.Name = webElement.FindElement(By.ClassName("_52jb")).Text;
                    }

                    if (webElement.FindElements(By.ClassName("header h3 a")).Count != 0)
                    {
                        tempFamilyMember.Link = webElement.FindElement(By.CssSelector("header h3 a")).Text;
                    }

                    if (webElement.FindElements(By.ClassName("_52jg")).Count != 0)
                    {
                        tempFamilyMember.Relationship = webElement.FindElement(By.ClassName("_52jg")).Text;
                    }

                    facebookAccount.Family.Add(tempFamilyMember);
                }

                FileHelper.WriteToLog("Family search complete!", Data);

                #endregion

                #region About

                FileHelper.WriteToLog("About search...", Data);

                if (Browser.FindElements(By.CssSelector("#relationship ._5cdt")).Count != 0)
                {
                    facebookAccount.About = Browser.FindElement(By.CssSelector("#bio ._5cdt")).Text;
                }
                else
                {
                    facebookAccount.About = String.Empty;
                }

                FileHelper.WriteToLog("About search complete!", Data);

                #endregion

                #region Quote

                FileHelper.WriteToLog("Quote search...", Data);

                if (Browser.FindElements(By.CssSelector("#quote ._5cdt")).Count != 0)
                {
                    facebookAccount.FavoriteQuotes = Browser.FindElement(By.CssSelector("#quote ._5cdt")).Text;
                }
                else
                {
                    facebookAccount.FavoriteQuotes = String.Empty;
                }

                FileHelper.WriteToLog("Quote search complete!", Data);

                #endregion

                #region Skills

                FileHelper.WriteToLog("Skills search...", Data);

                if (Browser.FindElements(By.CssSelector("#skills .fcg")).Count != 0)
                {
                    facebookAccount.Skills = Browser.FindElement(By.CssSelector("#skills .fcg")).Text;
                }
                else
                {
                    facebookAccount.Skills = String.Empty;
                }

                FileHelper.WriteToLog("Skills search complete!", Data);

                #endregion

                #region Nicknames

                FileHelper.WriteToLog("Nicknames search...", Data);

                List<string> nicknames = new List<string> { };

                foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("#nicknames ._5cdv.r")))
                {
                    nicknames.Add(webElement.Text);
                }

                facebookAccount.Nicknames = nicknames;

                FileHelper.WriteToLog("Nicknames search complete!", Data);

                #endregion

                //#region Publics

                //FileHelper.WriteToLog("Publics search...");

                //HtmlWeb web = new HtmlWeb();

                //var htmlDoc = web.Load(url);

                //List<Public> publics = new List<Public> { };

                //foreach (HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//*[@id=\"u_0_e\"]/span[contains(concat(\" \",normalize-space(@class),\" \"),\" visible \")]/a"))
                //{
                //    publics.Add(new Public { Name = node.InnerText, Link = node.Attributes["href"].Value });
                //}

                //foreach (HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//*[@id=\"u_0_e\"]/span[contains(concat(\" \",normalize-space(@class),\" \"),\" hiddenItem \")]/a"))
                //{
                //    publics.Add(new Public { Name = node.InnerText, Link = node.Attributes["href"].Value });
                //}

                //facebookAccount.Publics = publics;

                //FileHelper.WriteToLog("Publics search complete!", Data);

                //#endregion

                string checkinsUrl = "";
                string sportsUrl = "";
                string musicUrl = "";
                string moviewsUrl = "";
                string tvshowsUrl = "";
                string booksUrl = "";

                foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("._56bq._52ja._52jh._59e9._55wp._3knw")))
                {
                    if (webElement.Text.ToLower().Contains("check-ins"))
                    {
                        checkinsUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                    }
                    else if (webElement.Text.ToLower().Contains("sports"))
                    {
                        sportsUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                    }
                    else if (webElement.Text.ToLower().Contains("music"))
                    {
                        musicUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                    }
                    else if (webElement.Text.ToLower().Contains("movies"))
                    {
                        moviewsUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                    }
                    else if (webElement.Text.ToLower().Contains("tv shows"))
                    {
                        tvshowsUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                    }
                    else if (webElement.Text.ToLower().Contains("books"))
                    {
                        booksUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                    }
                }

                #region Checkins 

                FileHelper.WriteToLog("Checkins search...", Data);

                if (checkinsUrl != "")
                {
                    string placesUrl = "";
                    string recentUrl = "";
                    string visitedCitiesUrl = "";

                    Browser.Navigate().GoToUrl(checkinsUrl);
                    System.Threading.Thread.Sleep(3000);
                    foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("._56bq._52ja._52jh._59e9._55wp._3knw")))
                    {
                        if (webElement.Text.ToLower().Contains("places"))
                        {
                            if (webElement.FindElements(By.ClassName("_5b6s")).Count != 0)
                            {
                                placesUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                            }
                            else
                            {
                                placesUrl = checkinsUrl;
                            }
                        }
                        else if (webElement.Text.ToLower().Contains("recent"))
                        {
                            if (webElement.FindElements(By.ClassName("_5b6s")).Count != 0)
                            {
                                recentUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                            }
                            else
                            {
                                recentUrl = checkinsUrl;
                            }
                        }
                        else if (webElement.Text.ToLower().Contains("visited cities"))
                        {
                            if (webElement.FindElements(By.ClassName("_5b6s")).Count != 0)
                            {
                                visitedCitiesUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                            }
                            else
                            {
                                visitedCitiesUrl = checkinsUrl;
                            }
                        }
                    }

                    if (placesUrl != "")
                    {
                        Browser.Navigate().GoToUrl(placesUrl);
                        System.Threading.Thread.Sleep(3000);

                        int tempNumber = 0;//
                        IEnumerable<IWebElement> tempWebElements;

                        js = (IJavaScriptExecutor)Browser;
                        for (; ; )
                        {
                            tempWebElements = Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw"));
                            tempNumber = tempWebElements.Count();

                            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

                            for (int i = 0; i < 100; i++)
                            {
                                if (Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw")).Count != tempNumber)
                                {
                                    break;
                                }
                                System.Threading.Thread.Sleep(50);
                            }

                            if (Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw")).Count == tempNumber)
                            {
                                break;
                            }

                            js.ExecuteScript("window.scrollTo(0, 0);");
                        }

                        facebookAccount.Places = new List<Place> { };
                        foreach (IWebElement webElement in tempWebElements)
                        {
                            facebookAccount.Places.Add(new Place { Name = webElement.FindElement(By.CssSelector(".title.allowWrap.mfsm.fcb strong")).Text, Link = webElement.FindElement(By.CssSelector(".touchable.primary")).GetAttribute("href") });
                        }
                    }

                    if (recentUrl != "")
                    {
                        Browser.Navigate().GoToUrl(recentUrl);
                        System.Threading.Thread.Sleep(3000);

                        int tempNumber = 0;//
                        IEnumerable<IWebElement> tempWebElements;

                        js = (IJavaScriptExecutor)Browser;
                        for (; ; )
                        {
                            tempWebElements = Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw"));
                            tempNumber = tempWebElements.Count();

                            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

                            for (int i = 0; i < 100; i++)
                            {
                                if (Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw")).Count != tempNumber)
                                {
                                    break;
                                }
                                System.Threading.Thread.Sleep(50);
                            }

                            if (Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw")).Count == tempNumber)
                            {
                                break;
                            }

                            js.ExecuteScript("window.scrollTo(0, 0);");
                        }

                        facebookAccount.Recent = new List<RecentItem> { };
                        foreach (IWebElement webElement in tempWebElements)
                        {
                            facebookAccount.Recent.Add(new RecentItem { Name = webElement.FindElement(By.CssSelector(".title.allowWrap.mfsm.fcb strong")).Text, Link = webElement.FindElement(By.CssSelector(".touchable.primary")).GetAttribute("href"), Items = webElement.FindElements(By.CssSelector("._52jc._52ja._52jg")).Select(e => e.Text).ToList() });
                        }
                    }

                    if (visitedCitiesUrl != "")
                    {
                        Browser.Navigate().GoToUrl(visitedCitiesUrl);
                        System.Threading.Thread.Sleep(3000);

                        int tempNumber = 0;//
                        IEnumerable<IWebElement> tempWebElements;

                        js = (IJavaScriptExecutor)Browser;
                        for (; ; )
                        {
                            tempWebElements = Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw"));
                            tempNumber = tempWebElements.Count();

                            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

                            for (int i = 0; i < 100; i++)
                            {
                                if (Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw")).Count != tempNumber)
                                {
                                    break;
                                }
                                System.Threading.Thread.Sleep(50);
                            }

                            if (Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw")).Count == tempNumber)
                            {
                                break;
                            }

                            js.ExecuteScript("window.scrollTo(0, 0);");
                        }

                        facebookAccount.VisitedCities = new List<VisitedCity> { };
                        foreach (IWebElement webElement in tempWebElements)
                        {
                            facebookAccount.VisitedCities.Add(new VisitedCity { Name = webElement.FindElement(By.CssSelector(".title.allowWrap.mfsm.fcb strong")).Text, Link = webElement.FindElement(By.CssSelector(".touchable.primary")).GetAttribute("href") });
                        }
                    }
                }

                FileHelper.WriteToLog("Checkins search complete!", Data);

                #endregion

                #region Sport 

                FileHelper.WriteToLog("Sport search...", Data);

                if (sportsUrl != "")
                {
                    string sportsTeamsUrl = "";
                    string athletesUrl = "";

                    Browser.Navigate().GoToUrl(sportsUrl);
                    System.Threading.Thread.Sleep(3000);
                    foreach (IWebElement webElement in Browser.FindElements(By.CssSelector("._56bq._52ja._52jh._59e9._55wp._3knw")))
                    {
                        if (webElement.Text.ToLower().Contains("sports teams"))
                        {
                            if (webElement.FindElements(By.ClassName("_5b6s")).Count != 0)
                            {
                                sportsTeamsUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                            }
                            else
                            {
                                sportsTeamsUrl = sportsUrl;
                            }
                        }
                        else if (webElement.Text.ToLower().Contains("athletes"))
                        {
                            if (webElement.FindElements(By.ClassName("_5b6s")).Count != 0)
                            {
                                athletesUrl = webElement.FindElement(By.ClassName("_5b6s")).GetAttribute("href");
                            }
                            else
                            {
                                athletesUrl = sportsUrl;
                            }
                        }
                    }

                    if (sportsTeamsUrl != "")
                    {
                        Browser.Navigate().GoToUrl(sportsTeamsUrl);
                        System.Threading.Thread.Sleep(3000);

                        int tempNumber = 0;//
                        IEnumerable<IWebElement> tempWebElements;

                        js = (IJavaScriptExecutor)Browser;
                        for (; ; )
                        {
                            tempWebElements = Browser.FindElements(By.CssSelector("._1a5p"));
                            tempNumber = tempWebElements.Count();

                            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

                            for (int i = 0; i < 100; i++)
                            {
                                if (Browser.FindElements(By.CssSelector("._1a5p")).Count != tempNumber)
                                {
                                    break;
                                }
                                System.Threading.Thread.Sleep(50);
                            }

                            if (Browser.FindElements(By.CssSelector("._1a5p")).Count == tempNumber)
                            {
                                break;
                            }

                            js.ExecuteScript("window.scrollTo(0, 0);");
                        }

                        facebookAccount.SportTeams = new List<SportTeam> { };
                        foreach (IWebElement webElement in tempWebElements)
                        {
                            facebookAccount.SportTeams.Add(new SportTeam { Name = webElement.FindElement(By.CssSelector("._2w79")).Text, Link = webElement.FindElement(By.CssSelector(".darkTouch._51b5")).GetAttribute("href") });
                        }
                    }

                    if (athletesUrl != "")
                    {
                        Browser.Navigate().GoToUrl(athletesUrl);
                        System.Threading.Thread.Sleep(3000);

                        int tempNumber = 0;//
                        IEnumerable<IWebElement> tempWebElements;

                        js = (IJavaScriptExecutor)Browser;
                        for (; ; )
                        {
                            tempWebElements = Browser.FindElements(By.CssSelector("._1a5p"));
                            tempNumber = tempWebElements.Count();

                            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

                            for (int i = 0; i < 100; i++)
                            {
                                if (Browser.FindElements(By.CssSelector("._1a5p")).Count != tempNumber)
                                {
                                    break;
                                }
                                System.Threading.Thread.Sleep(50);
                            }

                            if (Browser.FindElements(By.CssSelector("._1a5p")).Count == tempNumber)
                            {
                                break;
                            }

                            js.ExecuteScript("window.scrollTo(0, 0);");
                        }

                        facebookAccount.SportAthletes = new List<SportAthlete> { };
                        foreach (IWebElement webElement in tempWebElements)
                        {
                            facebookAccount.SportAthletes.Add(new SportAthlete { Name = webElement.FindElement(By.CssSelector("._2w79")).Text, Link = webElement.FindElement(By.CssSelector(".darkTouch._51b5")).GetAttribute("href") });
                        }
                    }
                }

                FileHelper.WriteToLog("Sport search complete!", Data);

                #endregion

                #region Music 

                FileHelper.WriteToLog("Music search...", Data);

                if (musicUrl != "")
                {
                    Browser.Navigate().GoToUrl(musicUrl);
                    System.Threading.Thread.Sleep(3000);

                    int tempNumber = 0;//
                    IEnumerable<IWebElement> tempWebElements;

                    js = (IJavaScriptExecutor)Browser;
                    for (; ; )
                    {
                        tempWebElements = Browser.FindElements(By.CssSelector(".item._1zq-.tall.itemWithAction.acw"));
                        tempNumber = tempWebElements.Count();

                        js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

                        for (int i = 0; i < 100; i++)
                        {
                            if (Browser.FindElements(By.CssSelector(".item._1zq-.tall.itemWithAction.acw")).Count != tempNumber)
                            {
                                break;
                            }
                            System.Threading.Thread.Sleep(50);
                        }

                        if (Browser.FindElements(By.CssSelector(".item._1zq-.tall.itemWithAction.acw")).Count == tempNumber)
                        {
                            break;
                        }

                        js.ExecuteScript("window.scrollTo(0, 0);");
                    }

                    facebookAccount.Music = new List<MusicItem> { };

                    MusicItem musicItem;
                    foreach (IWebElement webElement in tempWebElements)
                    {
                        musicItem = new MusicItem { Name = webElement.FindElement(By.CssSelector(".title.allowWrap.mfsm.fcb strong")).Text, Link = webElement.FindElement(By.CssSelector(".touchable.primary")).GetAttribute("href") };
                        if (webElement.FindElements(By.CssSelector(".twoLines.preview.mfss.fcg span")).Count != 0)
                        {
                            musicItem.Description = webElement.FindElement(By.CssSelector(".twoLines.preview.mfss.fcg span")).Text;
                        }

                        facebookAccount.Music.Add(musicItem);
                    }
                }

                FileHelper.WriteToLog("Music search complete!", Data);

                #endregion

                #region Movies

                FileHelper.WriteToLog("Movies search...", Data);

                if (moviewsUrl != "")
                {
                    Browser.Navigate().GoToUrl(moviewsUrl);
                    System.Threading.Thread.Sleep(3000);

                    IEnumerable<IWebElement> tempWebElements;

                    js = (IJavaScriptExecutor)Browser;
                    for (; ; )
                    {
                        if (Browser.FindElements(By.ClassName("primarywrap")).Count != 0)
                        {
                            try
                            {
                                js.ExecuteScript("document.documentElement.getElementsByClassName('primarywrap')[0].click();");
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    facebookAccount.Movies = new List<Movie> { };

                    tempWebElements = Browser.FindElements(By.CssSelector("._5qk0"));
                    foreach (IWebElement webElement in tempWebElements)
                    {
                        try
                        {
                            facebookAccount.Movies.Add(new Movie { Name = webElement.FindElement(By.CssSelector("._52jh._5tg_")).Text, Link = webElement.FindElement(By.CssSelector("a")).GetAttribute("href") });
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                FileHelper.WriteToLog("Movies search complete!", Data);

                #endregion

                #region TVShows 

                FileHelper.WriteToLog("TVShows search...", Data);

                if (tvshowsUrl != "")
                {
                    Browser.Navigate().GoToUrl(tvshowsUrl);
                    System.Threading.Thread.Sleep(3000);

                    IEnumerable<IWebElement> tempWebElements;

                    js = (IJavaScriptExecutor)Browser;
                    for (; ; )
                    {
                        if (Browser.FindElements(By.ClassName("primarywrap")).Count != 0)
                        {
                            try
                            {
                                js.ExecuteScript("document.documentElement.getElementsByClassName('primarywrap')[0].click();");
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    facebookAccount.TVShows = new List<TVShow> { };

                    tempWebElements = Browser.FindElements(By.CssSelector("._5qk0"));
                    foreach (IWebElement webElement in tempWebElements)
                    {
                        facebookAccount.TVShows.Add(new TVShow { Name = webElement.FindElement(By.CssSelector("._52jh._5tg_")).Text, Link = webElement.FindElement(By.CssSelector("a")).GetAttribute("href") });
                    }
                }

                FileHelper.WriteToLog("TVShows search complete!", Data);

                #endregion

                #region Books 

                FileHelper.WriteToLog("Books search...", Data);

                if (booksUrl != "")
                {
                    Browser.Navigate().GoToUrl(booksUrl);
                    System.Threading.Thread.Sleep(3000);

                    IEnumerable<IWebElement> tempWebElements;

                    js = (IJavaScriptExecutor)Browser;
                    for (; ; )
                    {
                        if (Browser.FindElements(By.ClassName("primarywrap")).Count != 0)
                        {
                            try
                            {
                                js.ExecuteScript("document.documentElement.getElementsByClassName('primarywrap')[0].click();");
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    facebookAccount.Books = new List<Book> { };

                    tempWebElements = Browser.FindElements(By.CssSelector("._5qk0"));
                    foreach (IWebElement webElement in tempWebElements)
                    {
                        facebookAccount.Books.Add(new Book { Name = webElement.FindElement(By.CssSelector("._52jh._5tg_")).Text, Link = webElement.FindElement(By.CssSelector("a")).GetAttribute("href") });
                    }
                }

                FileHelper.WriteToLog("Books search complete!", Data);

                #endregion

            }
            //else if (QueryType == QueryType.Friends)
            //{
            //    #region Friends

            //    FileHelper.WriteToLog("Friends search...", Data);

            //    if (friendsUrl != "")
            //    {

            //        Browser.Navigate().GoToUrl(friendsUrl);
            //        System.Threading.Thread.Sleep(3000);

            //        List<Friend> friends = new List<Friend> { };
            //        int tempNumber = 0;//
            //        IEnumerable<IWebElement> tempWebElements;
            //        IWebElement tempWebElement;

            //        js = (IJavaScriptExecutor)Browser;
            //        for (; ; )
            //        {
            //            FileHelper.WriteToLog(String.Format("Added {0} friends...", friends.Count), Data);
            //            tempWebElements = Browser.FindElements(By.CssSelector("._55wp._7om2._5pxa"));
            //            tempNumber = tempWebElements.Count();

            //            foreach (IWebElement webElement in tempWebElements)
            //            {
            //                tempWebElement = webElement.FindElement(By.CssSelector("._52jh._5pxc a"));
            //                friends.Add(new Friend { Name = tempWebElement.Text, Link = tempWebElement.GetAttribute("href") });
            //            }

            //            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

            //            for (int i = 0; i < 100; i++)
            //            {
            //                if (Browser.FindElements(By.CssSelector("._55wp._7om2._5pxa")).Count != tempNumber)
            //                {
            //                    break;
            //                }
            //                System.Threading.Thread.Sleep(50);
            //            }

            //            if (Browser.FindElements(By.CssSelector("._55wp._7om2._5pxa")).Count == tempNumber)
            //            {
            //                break;
            //            }

            //            js.ExecuteScript("window.scrollTo(0, 0);document.getElementsByClassName('_55wo _55x2')[document.getElementsByClassName('_55wo _55x2').length - 2].innerHTML = ''; ");
            //        }
            //        facebookAccount.Friends = friends;

            //    }

            //    FileHelper.WriteToLog("Friends search complete!", Data);

            //    #endregion
            //}

            //else if (QueryType == QueryType.Followers)
            //{
            //    #region Followers

            //    FileHelper.WriteToLog("Followers search...", Data);

            //    if (followersUrl != "")
            //    {
            //        Browser.Navigate().GoToUrl(followersUrl);
            //        System.Threading.Thread.Sleep(3000);

            //        facebookAccount.Followers = new List<Follower> { };
            //        int tempNumber = 0;//
            //        IEnumerable<IWebElement> tempWebElements;

            //        js = (IJavaScriptExecutor)Browser;
            //        for (; ; )
            //        {
            //            tempWebElements = Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw"));
            //            FileHelper.WriteToLog(String.Format("Added {0} followers...", tempWebElements.Count()), Data);
            //            tempNumber = tempWebElements.Count();

            //            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

            //            for (int i = 0; i < 1000; i++)
            //            {
            //                if (Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw")).Count != tempNumber)
            //                {
            //                    break;
            //                }
            //                System.Threading.Thread.Sleep(50);
            //            }

            //            if (Browser.FindElements(By.CssSelector(".item._1zq-.tall.acw")).Count == tempNumber)
            //            {
            //                break;
            //            }
            //        }

            //        foreach (IWebElement webElement in tempWebElements)
            //        {
            //            facebookAccount.Followers.Add(new Follower { Name = webElement.FindElement(By.CssSelector(".title.mfsm.fcb strong")).Text, Link = webElement.FindElement(By.CssSelector(".touchable.primary")).GetAttribute("href") });
            //        }
            //    }

            //    FileHelper.WriteToLog("Followers search complete!", Data);

            //    #endregion

            //}

            return facebookAccount;
        }

        public void Dispose()
        {
            Browser.Dispose();
        }
    }
}
