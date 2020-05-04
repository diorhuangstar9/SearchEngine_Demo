using System;
using System.Collections.Generic;
using System.IO;
using Gather.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gather
{
    class Gather
    {
        private static readonly double MAX_GATHER_COUNT = Math.Pow(10, 4);
        private static readonly string SEARCH_PATTERN_LIN_START = "<a href=\"";
        private static readonly string SEARCH_PATTERN_LIN_END = "\"";
        private static readonly int LINK_MAX_LENGTH = 5 * 1024 * 1024;
        private static readonly int DOCRAW_MAX_LENGTH = 1024 * 1024 * 1024;
        private static readonly int DOCID_MAX_LENGTH = 50 * 1024 * 1024;
        private static readonly string LINK_BIN = "links.bin";
        private static readonly string DOC_RAW_BIN = "doc_raw.bin";
        private static readonly string DOC_ID_BIN = "doc_id.bin";
        private static readonly string[] INITIAL_SEEDS = new string[] { "https://www.qq.com", "https://www.sina.com.cn" };

        private static async System.Threading.Tasks.Task<string> WriteFileAsync(string currentFilePath, string content, int byteLimit)
        {
            StreamWriter sw;
            var currentFile = new FileInfo(currentFilePath);
            var newFilePath = currentFilePath;
            if (!currentFile.Exists)
                sw = currentFile.CreateText();
            else if (currentFile.Length > byteLimit)
            {
                newFilePath = Path.Combine(currentFile.DirectoryName, $"{DateTime.Now.Ticks}.txt");
                currentFile = new FileInfo(newFilePath);
                sw = currentFile.CreateText();
            }
            else
                sw = currentFile.AppendText();

            using (sw)
            {
                await sw.WriteLineAsync(content);
            }
            return newFilePath;
        }

        static async System.Threading.Tasks.Task Main(string[] seeds)
        {
            seeds = INITIAL_SEEDS;
            var builder = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHttpClient();
                services.AddSingleton<IMyHttpService, MyHttpService>();
                services.AddSingleton<IStringSearchService, StringSearchService>();
                services.AddSingleton<IBloomFilterService, BloomFilterService>(bf => new BloomFilterService((int)MAX_GATHER_COUNT));
            }).UseConsoleLifetime();

            var host = builder.Build();
            var services = host.Services;
            var myHttpService = services.GetRequiredService<IMyHttpService>();
            var stringSearchService = services.GetRequiredService<IStringSearchService>();
            var bloomFilterService = services.GetRequiredService<IBloomFilterService>();
            // BFS
            // if no seeds provided, point the specified links.bin folder, do the job
            // else, seeds provided, create a new links.bin folder
            var fileHandleQueue = new Queue<string>();
            DirectoryInfo baseFolder;
            if (seeds.Length <= 0)
            {
                // TODO the specified folder(the dateTime before)
                baseFolder = new DirectoryInfo("");
                foreach (var fileName in Directory.EnumerateFiles(LINK_BIN, "*.txt"))
                {
                    fileHandleQueue.Enqueue(fileName);
                }
            }
            else
            {
                baseFolder = new DirectoryInfo(DateTime.Now.Ticks.ToString());
                baseFolder.Create();
                Directory.CreateDirectory(Path.Combine(baseFolder.Name, LINK_BIN));
                Directory.CreateDirectory(Path.Combine(baseFolder.Name, DOC_RAW_BIN));
                Directory.CreateDirectory(Path.Combine(baseFolder.Name, DOC_ID_BIN));
                var filePath = Path.Combine(baseFolder.Name, LINK_BIN, $"{DateTime.Now.Ticks}.txt");
                await File.WriteAllLinesAsync(filePath, seeds);
                fileHandleQueue.Enqueue(filePath);
            }

            var currentWriteFilePath = Path.Combine(baseFolder.Name, LINK_BIN, $"{DateTime.Now.Ticks}.txt");
            fileHandleQueue.Enqueue(currentWriteFilePath);
            var currentDocRawFilePath = Path.Combine(baseFolder.Name, DOC_RAW_BIN, $"{DateTime.Now.Ticks}.txt");
            var currentDocIdFilePath = Path.Combine(baseFolder.Name, DOC_ID_BIN, $"{DateTime.Now.Ticks}.txt");

            double handledCount = 0;
            var currentDocId = 0;
            while (handledCount <= MAX_GATHER_COUNT)
            {
                // 爬虫从 links.bin 文件中，取出链接去爬取对应的页面。等爬取到网页之后，将解析出来的链接，直接存储到 links.bin 文件中
                if (!fileHandleQueue.TryDequeue(out string currentFilePath))
                    break;

                var currentReadFile = new FileInfo(currentFilePath);
                if (!currentReadFile.Exists)
                    continue;
                
                using (var sr = currentReadFile.OpenText())
                {
                    string currentPageLink;
                    while ((currentPageLink = sr.ReadLine()) != null)
                    {
                        try
                        {
                            // get one page content
                            var pageContent = await myHttpService.GetPage(currentPageLink);
                            // search for <link>
                            var startIdx = 0;
                            while (startIdx < pageContent.Length)
                            {
                                var linkStartIndex = stringSearchService.Search(pageContent, SEARCH_PATTERN_LIN_START, startIdx) + SEARCH_PATTERN_LIN_START.Length;
                                if (linkStartIndex < SEARCH_PATTERN_LIN_START.Length)
                                    break;
                                var linkEndIndex = stringSearchService.Search(pageContent, SEARCH_PATTERN_LIN_END, linkStartIndex);
                                if (linkEndIndex <= -1)
                                    break;
                                startIdx = linkEndIndex + SEARCH_PATTERN_LIN_END.Length;
                                var link = pageContent[linkStartIndex..linkEndIndex];
                                // if the link is duplicated(bloom_filter.bin), do not queue
                                // else(not duplicated), put into queue
                                if (!bloomFilterService.CheckExists(link))
                                {
                                    var newFilePath = await WriteFileAsync(currentWriteFilePath, link, LINK_MAX_LENGTH);
                                    if (newFilePath != currentWriteFilePath)
                                    {
                                        currentWriteFilePath = newFilePath;
                                        fileHandleQueue.Enqueue(newFilePath);
                                    }
                                }
                            }
                            currentDocId++;
                            // save the doc raw content
                            var newDocRawFilePath = await WriteFileAsync(currentDocRawFilePath,
                                $"{currentDocId}\t{pageContent.Length}\t{pageContent}{Environment.NewLine}", DOCRAW_MAX_LENGTH);
                            currentDocRawFilePath = currentDocRawFilePath == newDocRawFilePath ? currentDocRawFilePath : newDocRawFilePath;
                            // save the doc id content
                            // TODO should add docid range on fileName?
                            var newDocIdFilePath = await WriteFileAsync(currentDocIdFilePath, $"{currentDocId}\t{currentPageLink}", DOCID_MAX_LENGTH);
                            currentDocIdFilePath = currentDocIdFilePath == newDocIdFilePath ? currentDocIdFilePath : newDocIdFilePath;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Cannot get content:{currentPageLink}, {ex.Message}");
                        }
                        handledCount += 1;
                    }
                }
            }


        }


    }
}
