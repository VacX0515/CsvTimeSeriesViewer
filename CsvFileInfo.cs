using System;
using System.Collections.Generic;
using System.IO;

namespace CsvTimeSeriesViewer
{
    /// <summary>
    /// CSV 파일 정보를 관리하는 클래스
    /// </summary>
    public class CsvFileInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public FileSystemWatcher Watcher { get; set; }
        public Dictionary<string, List<double>> DataColumns { get; set; }
        public List<DateTime> Timestamps { get; set; }
        public List<string> Headers { get; set; }
        public int TimeColumnIndex { get; set; }
        public HashSet<string> SelectedColumns { get; set; }
        public Dictionary<string, DataFilter> Filters { get; set; }

        public CsvFileInfo()
        {
            DataColumns = new Dictionary<string, List<double>>();
            Timestamps = new List<DateTime>();
            Headers = new List<string>();
            SelectedColumns = new HashSet<string>();
            Filters = new Dictionary<string, DataFilter>();
            TimeColumnIndex = -1; // -1 means use row number
        }
    }
}