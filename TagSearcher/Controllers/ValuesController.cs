using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TagSearcher.Core.Helpers;
using TagSearcher.Facebook;
using TagSearcher.Instagram;
using TagSearcher.Proxy;
using TagSearcher.VK;

namespace TagSearcher.Controllers
{
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpGet]
        [Route("")]
        public ActionResult<string> Index()
        {
            return System.IO.File.ReadAllText("readme.txt");
        }

        [HttpGet]
        [Route("proxy/check/{host}/{port}")]
        public ActionResult<string> ProxyCheck(string host, string port)
        {
            try
            {
                ProxyChecker proxyChecker = new ProxyChecker(host, port);
                return proxyChecker.Check();
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    FileHelper.WriteToLog(e.Message + " " + e.InnerException.Message, "");
                }
                else
                {
                    FileHelper.WriteToLog(e.Message, "");
                }
            }
            return "Proxy is not available!";
        }

        //https://localhost:44324/fb/mytag/0.0.0.0/0/dobrij.den3352@gmail.com/airus1K
        [Route("fb/{tag}/{proxy_host}/{proxy_port}/{fb_login}/{fb_password}/{query_type=}")]
        [Obsolete]
        public ActionResult<string[]> FacebookLikes(string tag, string proxy_host, string proxy_port, string fb_login, string fb_password, string query_type = "none")
        {
            Facebook.QueryType queryType;

            switch (query_type)
            {
                case "info":
                    queryType = Facebook.QueryType.Info;
                    break;
                default:
                    queryType = Facebook.QueryType.None;
                    break;
            }

            FbParser fbParser = null;

            try
            {
                fbParser = new FbParser(proxy_host, proxy_port, fb_login, fb_password, queryType);
                return fbParser.CountLikes(tag);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    FileHelper.WriteToLog(e.Message + " " + e.InnerException.Message, "");
                }
                else
                {
                    FileHelper.WriteToLog(e.Message, "");
                }
            }
            finally
            {
                if (fbParser != null)
                {
                    fbParser.Dispose();
                }
            }
            return new string[] { "0" };
        }

        //https://localhost:44324/in/mytag/0.0.0.0/0
        [Route("in/{tag}/{proxy_host}/{proxy_port}/{query_type=}")]
        public ActionResult<string[]> InstagramLikes(string tag, string proxy_host, string proxy_port, string query_type)
        {
            Instagram.QueryType queryType;

            switch (query_type)
            {
                case "info":
                    queryType = Instagram.QueryType.Info;
                    break;
                default:
                    queryType = Instagram.QueryType.None;
                    break;
            }

            InParser inParser = null;

            try
            {
                inParser = new InParser(tag, proxy_host, proxy_port, queryType);
                return inParser.CountLikes(tag);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    FileHelper.WriteToLog(e.Message + " " + e.InnerException.Message, "");
                }
                else
                {
                    FileHelper.WriteToLog(e.Message, "");
                }
            }
            finally
            {
                if (inParser != null)
                {
                    inParser.Dispose();
                }
            }

            return new string[] { "0" };
        }

        //https://localhost:44324/in/mytag/0.0.0.0/0/dobrij.den3352@gmail.com/airus1K
        [Route("vk/{tag}/{proxy_host}/{proxy_port}/{vk_login}/{vk_password}/{query=}")]
        public ActionResult<string[]> VKLikes(string tag, string proxy_host, string proxy_port, string vk_login, string vk_password, string query)
        {
            VK.QueryType queryType;

            switch (query)
            {
                case "info":
                    queryType = VK.QueryType.Info;
                    break;
                default:
                    queryType = VK.QueryType.None;
                    break;
            }

            VKParser vkParser = null;

            try
            {
                vkParser = new VKParser(tag, proxy_host, proxy_port, vk_login, vk_password, queryType);
                return vkParser.CountLikes(tag);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    FileHelper.WriteToLog(e.Message + " " + e.InnerException.Message, "");
                }
                else
                {
                    FileHelper.WriteToLog(e.Message, "");
                }
            }
            finally
            {
                if (vkParser != null)
                {
                    vkParser.Dispose();
                }
            }
            return new string[] { "0" };
        }
    }
}
