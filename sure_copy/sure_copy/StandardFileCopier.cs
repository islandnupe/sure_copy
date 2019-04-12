using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sure_copy
{
    class StandardFileCopier
    {
        private long longTotalBytes = 0;
        public StandardFileCopier(string Source, string Dest)
        {
            this.SourceFilePath = Source;
            this.DestFilePath = Dest;

            FileInfo fi = new FileInfo(SourceFilePath);
            longTotalBytes = fi.Length;

            OnProgressChanged += delegate { };
            OnComplete += delegate { };
        }

        public void Copy()
        {
            bool cancelFlag = false;
            

            File.Copy(SourceFilePath, DestFilePath, true);

            OnComplete(SourceFilePath, TotalBytes);
        }

        public string SourceFilePath { get; set; }
        public string DestFilePath { get; set; }
        public long TotalBytes 
        {
            get
            {
                return longTotalBytes;
            }
        }

        public event ProgressChangeDelegate OnProgressChanged;
        public event Completedelegate OnComplete;
    }
}
