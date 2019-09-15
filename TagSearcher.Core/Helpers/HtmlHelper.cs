using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TagSearcher.Core.Helpers
{
    public static class HtmlHelper
    {
        public static string GetHtmlPage(string url, bool isDefault = false)
        {
            string HtmlText = String.Empty;

            HttpWebRequest myHttwebrequest;
            HttpWebResponse myHttpWebresponse;
            StreamReader strm;
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
                    myHttwebrequest = (HttpWebRequest)HttpWebRequest.Create(url);
                    myHttpWebresponse = (HttpWebResponse)myHttwebrequest.GetResponse();
                    if (isDefault)
                    {
                        strm = new StreamReader(myHttpWebresponse.GetResponseStream(), Encoding.Default);
                    }
                    else
                    {
                        strm = new StreamReader(myHttpWebresponse.GetResponseStream());
                    }
                    HtmlText = strm.ReadToEnd();
                    myHttpWebresponse.GetResponseStream().Close();
                    myHttwebrequest.GetResponse().Close();
                }
                catch (Exception e)
                {
                    Thread.Sleep(100);
                    continue;
                }
                break;
            }
            return HtmlText;
        }
    }
}
