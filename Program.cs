using Microsoft.Extensions.Configuration;
using SalesforceMagic;
using SalesforceMagic.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SalesforceBackupFilesDownloader
{
    class Program
    {
        static IConfiguration config;
        static Dictionary<string, string> argumentsMap;
        static int Main(string[] args)
        {
            DateTime startTime = DateTime.Now;
            Console.WriteLine($"SFDC Backup Files Downloader, starting at {startTime}");
            Console.WriteLine();
            List<string> processedFiles = new List<string>();
            config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", true, true)
               .Build();
            argumentsMap = ParseArguments(args);

            string baseUrl = GetArgumentOrDefault("--baseurl", config["baseUrl"]);
            string maxParallel = GetArgumentOrDefault("--maxparallel", config["MaxParallel"]);
            string startFromNumber = GetArgumentOrDefault("--startfromfilenumber", config["startFromFileNumber"]);
            string downloadFolder = GetArgumentOrDefault("--downloadfolder", config["downloadFolder"]);
            string reportDelay = GetArgumentOrDefault("--reportdownloaddelay", config["reportDownloadDelay"]);
            string tempFolder = GetArgumentOrDefault("--tempfolder", config["tempFolder"]);
            string username = GetArgumentOrDefault("--username", config["username"]);
            string pass = GetArgumentOrDefault("--pass", config["pass"]);
            string exportPage = GetArgumentOrDefault("--dataexportpagepath", config["dataExportPagePath"]);
            string isSandbox = GetArgumentOrDefault("--issandbox", config["IsSandbox"]);


            int numberFrom = 0;
            if (!int.TryParse(startFromNumber, out numberFrom) && numberFrom > 0) numberFrom = 1;

            int mparallel = 0;
            if (!int.TryParse(maxParallel, out mparallel) && mparallel > 0) mparallel = 6;

            int reportD = 0;
            if (!int.TryParse(reportDelay, out reportD) && reportD > 0) reportD = 6;
            reportD = reportD * 1000;

            bool isSandBox = false;
            bool.TryParse(isSandbox, out isSandBox);

            if (string.IsNullOrEmpty(pass)) throw new ArgumentException("No password found");
            if (string.IsNullOrEmpty(exportPage)) throw new ArgumentException("No data export page path found");
            if (string.IsNullOrEmpty(username)) throw new ArgumentException("No username found");
            if (string.IsNullOrEmpty(baseUrl)) throw new ArgumentException("No base url");
            if (string.IsNullOrEmpty(downloadFolder)) throw new ArgumentException("Please specify download folder.");

            string sessionId;
            Organization org;
            try
            {
                var config = new SalesforceConfig
                {
                    Username = username,
                    Password = pass,
                    IsSandbox = isSandBox
                };

                using (SalesforceClient client = new SalesforceClient(config))
                {
                    // Salesforce logic
                    sessionId = client.GetSessionId();
                    org = client.Query<Organization>(limit: 1).SingleOrDefault();
                }

            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message + "\n" + err.StackTrace);
                Console.ReadKey();
                return 1;
            }
            //adding cookies 
            CookieContainer cookieContainer = new CookieContainer();
            cookieContainer.Add(new Uri(baseUrl), new CookieCollection { new Cookie("oid", org.Id), new Cookie("sid", sessionId) });
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };

            HttpClient _httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromDays(1) };

            if (!Directory.Exists(downloadFolder)) Directory.CreateDirectory(downloadFolder);
            if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

            string fileContent = _httpClient.GetAsync(exportPage).Result.Content.ReadAsStringAsync().Result;
            try
            {

                //reads from the file, the file names must be splitted by semi colon;
                List<string> listOfIds = readXmlTable(fileContent, numberFrom);
                List<Task> downloadTasks = new List<Task>();
                List<DownloadBackupFileReportItem> downloadTaskReport = new List<DownloadBackupFileReportItem>();
                bool lastBatchProcessed = false;
                //tries to download the files in parallel.. if one is completed it will try to  continue to the next one.
                for (int i = 0; i < listOfIds.Count; i++)
                {
                    //instantiating an empty shell for better tracking
                    DownloadBackupFileReportItem reportItem = new DownloadBackupFileReportItem();
                    downloadTaskReport.Add(reportItem);

                    string furl = listOfIds[i];
                    try
                    {
                        string fname = furl.Split('&')[0].Split('=')[1];
                        Console.WriteLine($"Downloading {fname}");

                        reportItem.response = _httpClient.GetAsync($"{furl}", HttpCompletionOption.ResponseHeadersRead);
                        reportItem.fileName = fname;
                        reportItem.ConsoleCursorTop = Console.CursorTop;
                        reportItem.taskId = i.ToString() + fname;

                        //sending the task to the babckground
                        Task aDownloadTaskInTheBackground = downloadFile(reportItem, tempFolder, downloadFolder, reportD);
                        downloadTasks.Add(aDownloadTaskInTheBackground);

                        //if we have the specified number of downloads already started
                        //or is the very last item we are processing
                        //then wait for all of them to finish
                        if ((downloadTasks.Count % mparallel) == 0 || i == (listOfIds.Count - 1))
                        {
                            Task.WaitAll(downloadTasks.ToArray());

                            //let's delete all the tasks inside this list
                            //they should have been completed at this point.
                            downloadTasks.Clear();

                            //cleaning the console.
                            Console.Clear();

                            //if no error happened
                            //this line will set the variable to true
                            //else it means we still have some pending tasks to process
                            lastBatchProcessed = true;
                        }


                    }
                    catch (Exception err)
                    {
                        Console.WriteLine($"Error With {furl}");
                        Console.WriteLine(err.Message);
                        Console.Write(err.StackTrace);
                    }

                    //if there was a problem adding the last 
                    if (lastBatchProcessed != true)
                    {
                        //let's wait a litlte bit to make sure all the tasks completed
                        Task.WaitAll(downloadTasks.ToArray());
                        downloadTasks.Clear();
                    }

                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                Console.WriteLine(err.StackTrace);
                Console.ReadKey();
                return 1;
            }
            DateTime endTime = DateTime.Now;
            Console.WriteLine($"Finished at {endTime}, it took {endTime - startTime} to finish, if you want to close just press a key..");
            Console.ReadKey();
            return 0;
        }


        /// <summary>
        /// To read the html
        /// </summary>
        /// <param name="startFrom">You can specificy the starting link to download, NON zero based</param>
        /// <returns></returns>
        static List<string> readXmlTable(string fileContent, int startFrom)
        {
            List<string> result = new List<string>();
            XmlDocument doc = new XmlDocument();
            string xmlHeader = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            int? startTable = fileContent?.IndexOf("<table class=\"list\"");
            int totalLength = (fileContent?.LastIndexOf("</table>") - startTable).Value;
            string targetTable = fileContent?.Substring(startTable.Value, totalLength);
            targetTable = targetTable.Substring(0, targetTable.IndexOf("</table>")) + "</table>";
            doc.Load(new MemoryStream(Encoding.UTF8.GetBytes(xmlHeader + targetTable)));
            XmlNode root = doc.DocumentElement;

            // Select and display the first node in which the author's   
            // last name is Kingsolver.  
            XmlNodeList nodes = root.SelectNodes("//a[@href]");
            int i = 1;
            foreach (XmlNode node in nodes)
            {
                if (i >= startFrom)
                {
                    string attr = node.Attributes["href"].Value;
                    if (attr != null)
                        result.Add(attr.Replace("amp;", ""));
                }
                i++;
            }
            if (result.Count > 0) Console.WriteLine($"parsing html successfully, total items to download {result.Count}");

            return result;
        }
        static async Task downloadFile(DownloadBackupFileReportItem downloadTask, string tempFolder, string targetFolder, int reportDelay)
        {
            using (HttpResponseMessage response = await downloadTask.response)
            {
                using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                {
                    string fileToWriteTo = Path.Combine(tempFolder, downloadTask.fileName);
                    using (Stream streamToWriteTo = File.Open(fileToWriteTo, FileMode.Create, FileAccess.ReadWrite))
                    {
                        var totalRead = 0L;
                        var totalReads = 0L;
                        var buffer = new byte[51200];
                        var isMoreToRead = true;

                        do
                        {
                            var read = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                                downloadTask.DowloadedBytes = (totalRead);
                                downloadTask.SetComplete();
                            }
                            else
                            {
                                await streamToWriteTo.WriteAsync(buffer, 0, read);

                                totalRead += read;
                                totalReads += 1;

                                if (totalReads % 3 == 0)
                                {
                                    downloadTask.DowloadedBytes = (totalRead);
                                }
                            }
                            downloadTask.ToDisplayProgress();
                            System.Threading.Thread.Sleep(reportDelay);
                        }
                        while (isMoreToRead);

                        try
                        {
                            await streamToWriteTo.FlushAsync();
                            streamToWriteTo.Close();

                            string finalFile = Path.Combine(targetFolder, downloadTask.fileName);
                            File.Move(fileToWriteTo, finalFile);
                            downloadTask.fileCopied = true;
                            downloadTask.ToDisplayProgress();
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine("Error While copying the file locally");
                            Console.WriteLine(err.Message);
                            Console.WriteLine(err.StackTrace);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Tries to get an inputed argument, if does not exists returns a default value
        /// </summary>
        /// <param name="argumentToGet"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        static string GetArgumentOrDefault(string argumentToGet, string defaultValue)
        {
            return !argumentsMap.ContainsKey(argumentToGet) ? defaultValue : argumentsMap[argumentToGet];
        }

        /// <summary>
        /// Maps input arguments (thru command line) into a dictionary.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static System.Collections.Generic.Dictionary<string, string> ParseArguments(string[] args)
        {
            System.Collections.Generic.Dictionary<string, string> arguments = new System.Collections.Generic.Dictionary<string, string>();
            string key = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (i == 0) { key = args[i].ToLower(); continue; }
                if (i != 0 && i % 2 == 0)
                {
                    key = args[i].ToLower();
                }
                else
                    arguments[key] = args[i];
            }
            return arguments;
        }

    }

}
