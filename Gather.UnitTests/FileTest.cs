using System;
using System.IO;
using System.Text;
using Xunit;

namespace Gather.UnitTests
{
    public class FileTest
    {
        public FileTest()
        {
        }

        [Fact]
        public void File_Test1()
        {
            //Create the file.
            var path = "fileTest.txt";
            //var replaceStr = "link1,";
            using (var fs = File.Open(path, FileMode.Open))
            using (var sr = new StreamReader(fs))
            using (var sw = new StreamWriter(fs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    fs.Seek(0, SeekOrigin.End);
                    sw.WriteLine();
                    var appendStr = $"{line}-{DateTime.Now.Ticks}";
                    sw.WriteLine(appendStr);
                    sw.WriteLine(appendStr);
                    sw.Flush();
                    fs.Seek(0, SeekOrigin.Begin);
                    for (var i = 0; i < line.Length; i++)
                        sw.Write(string.Empty);
                    sw.Flush();
                }
            }

        }

        private static void AppendText(FileStream fs, string value)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }

        private static void AddText(FileStream fs, string value)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }
    }
}
