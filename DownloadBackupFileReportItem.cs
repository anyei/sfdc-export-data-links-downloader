using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceBackupFilesDownloader
{


    class DownloadBackupFileReportItem
    {
        DateTime startTime;
        DateTime endTime;
        static object locker = new object();
        bool completed = false;
        public bool fileCopied = false;
        public DownloadBackupFileReportItem()
        {
            startTime = DateTime.Now;
        }
        public void ToDisplayProgress()
        {
            lock (locker)
            {
                Console.SetCursorPosition(0, ConsoleCursorTop);
                Console.WriteLine($"Download {fileName} {(fileCopied ? "Copied" : "Temp")} { (completed ? "Completed" : "In Progress") } downloaded {(DowloadedBytes / 1024)}KB  {TimeProgress}");
            }
        }
        public void SetComplete() { completed = true; endTime = DateTime.Now; }
        public Task<HttpResponseMessage> response { get; set; }
        public string fileName { get; set; }
        public TimeSpan TimeProgress { get { return DateTime.Now - startTime; } }
        public bool Completed { get { return completed; } }
        public long DowloadedBytes { get; set; }
        public int ConsoleCursorTop { get; set; }
        public string taskId { get; set; }
    }
}
