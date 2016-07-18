using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sure_copy
{
    public delegate void ProgressChangeDelegate(string FilePath, double Percentage, ref bool Cancel);
    public delegate void Completedelegate(string FilePath, long TotalBytes);

    class CustomFileCopier
    {
        public CustomFileCopier(string Source, string Dest)
        {
            this.SourceFilePath = Source;
            this.DestFilePath = Dest;

            OnProgressChanged += delegate { };
            OnComplete += delegate { };
        }

        public void Copy()
        {
            byte[] buffer = new byte[1024 * 1024]; // 1MB buffer
            bool cancelFlag = false;
            long totalBytes = 0;
            using (FileStream source = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read))
            {
                long fileLength = source.Length;
                using (FileStream dest = new FileStream(DestFilePath, FileMode.Create, FileAccess.Write))
                {          
                    int currentBlockSize = 0;

                    while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        totalBytes += currentBlockSize;
                        double percentage = (double)totalBytes * 100.0 / fileLength;

                        dest.Write(buffer, 0, currentBlockSize);

                        cancelFlag = false;
                        OnProgressChanged(SourceFilePath, percentage, ref cancelFlag);

                        if (cancelFlag == true)
                        {
                            // Delete dest file here
                            break;
                        }
                    }
                }
            }
            OnComplete(SourceFilePath, totalBytes);
        }

        public string SourceFilePath { get; set; }
        public string DestFilePath { get; set; }

        public event ProgressChangeDelegate OnProgressChanged;
        public event Completedelegate OnComplete;
    }
}
