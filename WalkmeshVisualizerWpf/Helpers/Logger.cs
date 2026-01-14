using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkmeshVisualizerWpf.Helpers
{
    public class Logger
    {
        public const string DIRECTORY = "Logs";
        public string LogFilePrefix
        {
            get => _logFilePrefix;
            set
            {
                _logFilePrefix = value;
                NewLogFile();
            }
        }
        private string _logFilePrefix;
        private string _logFilePath;

        public Logger(string filePrefix)
        {
            LogFilePrefix = filePrefix;
        }

        public void NewLogFile()
        {
            Directory.CreateDirectory(DIRECTORY);
            _logFilePath = Path.Combine(Environment.CurrentDirectory, DIRECTORY, $"{LogFilePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        }

        public void LogLines(IEnumerable<string> lines, bool includeDateTime = true)
        {
            if (includeDateTime)
            {
                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                lines = lines.Select(line => $"{now} - {line}");
            }
            File.AppendAllLines(_logFilePath, lines);
            File.AppendAllText(_logFilePath, Environment.NewLine);
        }
    }
}
