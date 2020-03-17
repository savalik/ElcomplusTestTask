using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Server
{
    public class Logger : IDisposable
    {
        private const int AppendLogPeriod = 3000;
        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private bool _cancellationRequested;

        public Logger()
        {
            var thread = new Thread(WriteLog);
            thread.Start();
        }

        public void Log(string message)
        {
            if (!_cancellationRequested)
                _queue.Enqueue(message);
        }

        private void WriteLog()
        {
            while (!_cancellationRequested || (_cancellationRequested && !_queue.IsEmpty))
            {
                if (_queue.TryDequeue(out var message))
                {
                    using (var w = File.AppendText("log.txt"))
                    {
                        Log(message, w);
                    }
                }
                else
                {
                    Thread.Sleep(AppendLogPeriod);
                } 
            }

            using (var w = File.AppendText("log.txt"))
            {
                Log("Server is stopped.", w);
            }
        }

        private static void Log(string logMessage, TextWriter w)
        {
            w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToShortDateString()} : {logMessage}");
        }

        public void Dispose()
        {
            _cancellationRequested = true;
        }
    }
}
