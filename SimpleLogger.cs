using System;
using System.IO;
using System.Text;

namespace GUIGUI17F
{
    public class SimpleLogger : IDisposable
    {
        private StreamWriter _writer;
        private bool _disposed;

        public SimpleLogger(string path)
        {
            _writer = new StreamWriter(path, true, Encoding.UTF8);
        }

        public void Log(string text)
        {
            _writer.WriteLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss"));
            _writer.WriteLine(text);
            _writer.WriteLine();
            _writer.Flush();
        }

        public void Log(Exception e)
        {
            _writer.WriteLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss"));
            _writer.WriteLine(e.GetType().FullName);
            _writer.WriteLine(e.Message);
            _writer.WriteLine(e.StackTrace);
            _writer.WriteLine();
            _writer.Flush();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _writer.Close();
                _writer.Dispose();
                _disposed = true;
            }
        }
    }
}
