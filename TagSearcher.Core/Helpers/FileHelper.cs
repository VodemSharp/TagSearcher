using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TagSearcher.Core.Helpers
{
    public static class FileHelper
    {
        public static void WriteToLog(string text, string data)
        {
            WriteText("log.txt", String.Format("{0} {1} {2}\r\n", DateTime.UtcNow, data, text));
        }

        public static void WriteText(string filePath, string text)
        {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096))
            {
                sourceStream.Write(encodedText, 0, encodedText.Length);
            };
        }
    }
}
