using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace WebSkanner
{
    class Program
    {
        static WebRequest webPage;
        static WebBrowser webBrowser1;
        public const int numberOfDownloadingThreads = 3;
        //static private List<String> gameLinks;
        static private List<String> links;
        static private List<string> fullText;
        static private HtmlWeb web;
        [STAThread]
        static void Main(string[] args)
        {
            //var html = @"https://beta.faceit.com/en/home/rankings";
            //var html = @"https://beta.faceit.com/en/players/Hoitelija/stats/csgo";
            //var html = @"https://en.wikipedia.org/wiki/Dano-Swedish_war";
            var html = @"C:\Users\fredr\OneDrive\Dokument\KandidatArbetet\Demo Filer\Kalle\teest.html";
            web = new HtmlWeb();
            webBrowser1 = new WebBrowser();
            var htmlDoc = web.Load(html);
            var node = htmlDoc.DocumentNode.SelectNodes("//span");
            links = new List<string>();
            List<String> gameLinks;
            gameLinks = new List<String>();

            Thread[] dowloadingStreamThreads = new Thread[numberOfDownloadingThreads];
            fullText = new List<string>();

            foreach (var player in node)
            {
                if (!Regex.IsMatch(player.InnerText, @"^\d+$"))
                    if (player.FirstChild is HtmlTextNode)
                        if (player.Attributes.Count == 1)
                            if (player.ParentNode.Name == "strong")
                                links.Add("https://beta.faceit.com/en/players/" + player.InnerText + "/stats/csgo");
            }
            int x = 1;
            long rows = links.Count / numberOfDownloadingThreads;
            while(x<numberOfDownloadingThreads+1)
            {
                List<String> tempList = new List<string>();
                for (int w=(int)rows*(x-1); w < rows*x; w++)
                {
                    tempList.Add(links[w]);
                }

                dowloadingStreamThreads[x-1] = new Thread(delegate ()
                {
                    new cpthread(tempList);
                });
                dowloadingStreamThreads[x-1].Start();
                x++;
            }
            
            int b = 0;
            while (true)
            {
                b = 0;
                foreach(var thread in dowloadingStreamThreads)
                {
                    if (thread.IsAlive) b++;
                }

                if (b == 0)
                {
                    break;
                }
            }

            System.IO.File.WriteAllLines(@"C:\Users\fredr\OneDrive\Dokument\KandidatArbetet\Demo Filer\Kalle\gamelinks.txt", fullText);
            Console.ReadKey();
        }

        public class cpthread
        {

            public cpthread(List<String> links)
            {
                Console.WriteLine("Thread start");
                var html = @"C:\Users\fredr\OneDrive\Dokument\KandidatArbetet\Demo Filer\Kalle\test.html";
                //webBrowser1 = new WebBrowser();
                var htmlDoc = web.Load(html);
                var node = htmlDoc.DocumentNode.SelectNodes("//span");
                List<String> gameLinks;
                gameLinks = new List<String>();

                IWebDriver _driver;
                ChromeOptions options = new ChromeOptions();
                options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);


                _driver = new ChromeDriver(options);

                int count = 0;
                DateTime startTime;
                int realoads;


                foreach (var p in links)
                {
                    _driver.Navigate().GoToUrl(p);
                    startTime = DateTime.Now;
                    realoads = 0;
                    while (true)
                    {

                        string pageSource = _driver.PageSource;
                        var htmlDocs = new HtmlAgilityPack.HtmlDocument();
                        htmlDocs.LoadHtml(pageSource);
                        node = htmlDocs.DocumentNode.SelectNodes("//a[@href]");


                        if (node.Count > 35 || realoads > 2)
                        {
                            break;
                        }
                        if (DateTime.Now - startTime > new TimeSpan(0, 0, 3))
                        {
                            _driver.Navigate().GoToUrl(p);
                            startTime = DateTime.Now;
                            realoads++;
                        }
                        System.Threading.Thread.Sleep(100);
                    }

                    foreach (var nodeee in node)
                        if (nodeee.InnerText.Contains("Match"))
                            gameLinks.Add("https://beta.faceit.com" + nodeee.GetAttributeValue("href", null));
                    if (count > 100)
                    {
                        break;
                    }
                    count++;
                }

                int completed = 0;
                int totalAmount = gameLinks.Count;
                foreach (var s in gameLinks)
                {
                    _driver.Navigate().GoToUrl(s);
                    while (true)
                    {
                        string pageSource = _driver.PageSource;
                        var htmlDocs = new HtmlAgilityPack.HtmlDocument();
                        htmlDocs.LoadHtml(pageSource);
                        node = htmlDocs.DocumentNode.SelectNodes("//a[@href]");


                        if (node == null || node.Count > 20)
                        {
                            break;
                        }

                    }
                    if (node != null)
                        foreach (var nodeee in node)
                            if (nodeee.InnerText.Contains("Demo"))
                                if (!fullText.Contains(nodeee.GetAttributeValue("href", null)))
                                {
                                    fullText.Add(nodeee.GetAttributeValue("href", null));
                                    Console.WriteLine(completed++ + " / " + totalAmount);
                                }
                    
                }

                _driver.Close();
            }
        }
       


        static private String LoadHtmlWithBrowser(String url)
        {
            /*webBrowser1.ScriptErrorsSuppressed = true;
            webBrowser1.Navigate(url);*/

            webPage = WebRequest.Create(url);
            webPage.Timeout = 4000;
            //waitTillLoad(webBrowser1);
            WebResponse response = webPage.GetResponse();

            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);


            StringBuilder sb = new StringBuilder();
            Byte[] buf = new byte[8192];
            int count = buf.Length;
            do
            {
                count = dataStream.Read(buf, 0, buf.Length);
                if (count != 0)
                {
                    sb.Append(Encoding.UTF8.GetString(buf, 0, count)); // just hardcoding UTF8 here
                }
            } while (count > 0);
            String html = sb.ToString();


            //string responseFromServer = reader.ReadToEnd();

            Console.WriteLine('"' + html + '"');

            /*HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            var documentAsIHtmlDocument3 = (mshtml.IHTMLDocument3)webBrowser1.Document.DomDocument;
            StringReader sr = new StringReader(documentAsIHtmlDocument3.documentElement.outerHTML);*/

            reader.Close();
            response.Close();
            return html;
        }

        static private void waitTillLoad(WebBrowser webBrControl)
        {
            WebBrowserReadyState loadStatus;
            int waittime = 100000;
            int counter = 0;
            while (true)
            {
                loadStatus = webBrControl.ReadyState;
                Application.DoEvents();
                if ((counter > waittime) || (loadStatus == WebBrowserReadyState.Uninitialized) || (loadStatus == WebBrowserReadyState.Loading) || (loadStatus == WebBrowserReadyState.Interactive))
                {
                    break;
                }
                counter++;
            }

            counter = 0;
            while (true)
            {
                loadStatus = webBrControl.ReadyState;
                Application.DoEvents();
                if (loadStatus == WebBrowserReadyState.Complete && webBrControl.IsBusy != true)
                {
                    break;
                }
                counter++;
            }
        }
    }


}
//*[@id="main-container-height-wrapper"]/div/div/section/div/section/div/div/table/tbody/tr/td/div/div/div/
//*[@id="home-main-height-wrapper"]/div/section/div/div/ng-include/div/div[2]/table/tbody/tr[151]/td[2]/strong/span