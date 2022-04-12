using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Concurrent;
using CsvHelper;
using System.Globalization;
using System.Diagnostics;

namespace CellshopperScrapingTask
{
    class Program
    {
                static ConcurrentBag<Product> Products = new ConcurrentBag<Product>();
                static readonly object _object = new object();

        static void Main(string[] args)
        {
            try
            {
                while (true)
                {
                    String data = Get("https://cellet.com/");

                    if (!String.IsNullOrEmpty(data))
                    {
                        HtmlDocument Doc = new HtmlDocument();
                        Doc.LoadHtml(data);

                        //Selecting Script Tag from HTML Responce
                        var SelectedNodes = Doc.DocumentNode.SelectNodes("//div[@id='primarynav']/ul[1]/li/a/@href");                //      div[@id='primarynav']/ul[1]/li/a    // This will all the required <a> elements of the page 

                        List<string> UrlLinks = new List<string>();

                        GetLinksUrls(SelectedNodes, UrlLinks);


                        foreach (var item in UrlLinks)
                        {
                            Thread childThread = new Thread(() =>
                                {
                                    PerformUrlsRequest(item);
                                });

                            childThread.Start();
                        }

                        //List<Task> tasks = new List<Task>();

                        DataToCSV(Products);
                        Console.ReadLine();

                        break;
                    } 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        static void PerformUrlsRequest(string url)
        {
            try
            {
                Console.WriteLine(url);

                String data = Get("http://www.cellshopper.com/store/" + url);

                if (!String.IsNullOrEmpty(data))
                {
                    HtmlDocument Doc = new HtmlDocument();
                    Doc.LoadHtml(data);

                    if (Doc.DocumentNode.SelectNodes("//div[@id='content']/div[@id='phonelist']") != null)
                    {
                        //Selecting Script Tag from HTML Responce
                        var SelectedNodes = Doc.DocumentNode.SelectNodes("//div[@id='phonelist']/a[1]/@href");

                        List<string> UrlLinks = new List<string>();
                        GetLinksUrls(SelectedNodes, UrlLinks);

                        foreach (var item in UrlLinks)
                        {
                            PerformUrlsRequest(item);
                        }
                    }
                    else if (Doc.DocumentNode.SelectNodes("//div[@id='products_list']") != null)
                    {
                        var SelectedNodes = Doc.DocumentNode.SelectNodes("//div[@id='products_list']/a[1]/@href");

                        List<string> UrlLinks = new List<string>();
                        GetLinksUrls(SelectedNodes, UrlLinks);

                        foreach (var item in UrlLinks)
                        {
                            PerformUrlsRequest(item);
                        }
                    }

                    else
                    {
                        string NavigationAddress = string.Empty;
                        var NavigationPath = Doc.DocumentNode.SelectNodes("//div[@id='bread-crumb']/a");

                        bool ProductsNotFound = Doc.DocumentNode.SelectSingleNode("//div[@id='content']").InnerText.Contains("There are no available products under this category");

                        if (!ProductsNotFound)
                        {
                            foreach (var item in NavigationPath)
                            {
                                NavigationAddress += " - " + "\"" + item.InnerHtml + "\"";
                            }

                            int index = Doc.DocumentNode.SelectSingleNode("//div[@id='product_page_details']/h4[@class='price']").InnerText.LastIndexOf(';');
                            int index2 = Doc.DocumentNode.SelectSingleNode("//div[@class='full'][4]").InnerText.Replace("\n", "").LastIndexOf(":");

                            Product MyProduct = new Product
                            {
                                NavigationAddress = NavigationAddress == null ? "" : NavigationAddress,
                                ProductInfo = Doc.DocumentNode.SelectSingleNode("//div[@id='product_page_details']/h2") == null ? "" : Doc.DocumentNode.SelectSingleNode("//div[@id='product_page_details']/h2").InnerText,
                                ItemCode = Doc.DocumentNode.SelectSingleNode("//div[@id='product_page_details']/h4[1]") == null ? "" : Doc.DocumentNode.SelectSingleNode("//div[@id='product_page_details']/h4[1]").InnerText.Replace("Item Code: ", ""),

                                ItemId = Doc.DocumentNode.SelectSingleNode("//div[@id='product_page_details']/h4[2]") == null ? 0 : Convert.ToInt32(Doc.DocumentNode.SelectSingleNode("//div[@id='product_page_details']/h4[2]").InnerText.Replace("&nbsp;", "").Replace("Item ID:", "")),
                                Price = Doc.DocumentNode.SelectSingleNode("//div[@id='product_page_details']/h4 [@class='price']") == null ? "" : Doc.DocumentNode.SelectSingleNode("//div[@id='product_page_details']/h4 [@class='price']").InnerText.Remove(0, index + 1),
                                Description = Doc.DocumentNode.SelectSingleNode("//div[@id='content']/div[@class='full'][3]/p") == null ? "" : Doc.DocumentNode.SelectSingleNode("//div[@id='content']/div[@class='full'][3]/p").InnerText,
                                Compatibility = Doc.DocumentNode.SelectSingleNode("//div[@class='full'][4]") == null ? "" : Doc.DocumentNode.SelectSingleNode("//div[@class='full'][4]").InnerText.Replace("\n", "").Remove(0, index2 + 1),
                            };

                            //Console.WriteLine(MyProduct.ProductInfo);
                            //if (!Products.Contains(MyProduct))
                            //{
                            //    Products.Add(MyProduct);
                            //}

                            lock (_object)
                            {
                                if (!Products.Contains(MyProduct))
                                {
                                    Console.WriteLine(MyProduct.ProductInfo);
                                    Products.Add(MyProduct);
                                }
                                else {
                                    Console.WriteLine("Duplicate");
                                }
                            }

                            return;
                        }
                        else
                        {
                            return;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static List<string> GetLinksUrls(HtmlNodeCollection nodes, List<string> links)
        {
            try
            {
                //Selecting 'href' attributes of <a> Tag
                foreach (var item in nodes)
                {
                    links.Add(item.Attributes["href"].Value.Replace("&amp;", "&"));
                }
                Debug.WriteLine(links.Count);
                return links;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        static String Get(String url)
        {
            String resul = String.Empty;
            try
            {
                HttpWebRequest _WebRequest = (HttpWebRequest)WebRequest.Create(url);

                //WebProxy myproxy = new WebProxy("127.0.0.1:8888", false);
                //myproxy.BypassProxyOnLocal = false;
                //_WebRequest.Proxy = myproxy;
                _WebRequest.Method = "GET";

                _WebRequest.Credentials = CredentialCache.DefaultCredentials;

                //GetResponce
                HttpWebResponse responce = (HttpWebResponse)_WebRequest.GetResponse();
                Console.WriteLine(responce.StatusDescription);

                if (responce.StatusDescription == "OK")
                {
                    //Read Responce Stream
                    using (Stream dataStream = responce.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(dataStream))
                        {
                            resul = reader.ReadToEnd();
                            //Console.WriteLine(responcefromServer);
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }
            return resul;
        }


        static void GetImages(string url)
        {
            HttpWebRequest _WebRequest = (HttpWebRequest)WebRequest.Create(url);
            _WebRequest.Method = "GET";
            _WebRequest.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse responce = (HttpWebResponse)_WebRequest.GetResponse();


            //var stream = responce.GetResponseStream();
            ////var image = Image.FromStream(stream).Save(;

            //string strFile = DateTime.Now.ToString("dd_MMM_yymmss") + ".jpg";
            ////FileStream log = new FileStream(;
            //byte[] buffer = new byte[1024];
            //int c;
            //while ((c = _WebRequest.GetRequestStream().Read(buffer, 0, buffer.Length)) > 0)
            //{
            //    log.Write(buffer, 0, c);
            //}
            ////Write jpg filename to be picked up by regex and displayed on flash html page.
            //Console.Write(strFile);
            //log.Close();
        }


        static void DataToCSV(ConcurrentBag<Product> DataList)
        {
            try
            {
                using (var writer = new StreamWriter("../Data.csv", false))
                {
                    using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csvWriter.Configuration.Delimiter = ",";
                        csvWriter.Configuration.HasHeaderRecord = true;
                        csvWriter.Configuration.AutoMap<Product>();

                        //csvWriter.WriteField("JobNum");
                        //csvWriter.WriteField("JobId");
                        //csvWriter.WriteField("JobDescription");
                        //csvWriter.WriteField("Jobtitle");
                        //csvWriter.WriteField("CompanyName");
                        //csvWriter.WriteField("LastDate");
                        //csvWriter.WriteField("city");
                        //csvWriter.NextRecord();

                        //foreach (var item in DataList)
                        //{
                        //    csvWriter.WriteField(item.JobNum);
                        //    csvWriter.WriteField(item.JobId);
                        //    csvWriter.WriteField(item.JobDescription);
                        //    csvWriter.WriteField(item.Jobtitle);
                        //    csvWriter.WriteField(item.CompanyName);
                        //    csvWriter.WriteField(item.LastDate);
                        //    csvWriter.WriteField(item.City);
                        //    csvWriter.NextRecord();
                        //}

                        csvWriter.WriteRecords(DataList);
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
