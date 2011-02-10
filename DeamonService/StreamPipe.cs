using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace DeamonService
{
    public class StreamPipe
    {
        private Stream input;
        private Stream output;
        private bool running = true;

        public StreamPipe(Stream input, Stream output)
        {
            this.input = input;
            this.output = output;

            new Thread(CopyStream).Start();
        }

        private void CopyStream()
        {
            byte[] buffer = new byte[512];
            while (running)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;
                output.Write(buffer, 0, read);
            }
        }

        internal void Close()
        {
            running = false;
        }
    }
}
