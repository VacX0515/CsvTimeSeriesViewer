namespace CsvTimeSeriesViewer
{
    /// <summary>
    /// 데이터 필터링을 위한 클래스
    /// </summary>
    public class DataFilter
    {
        public bool Enabled { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }

        public DataFilter()
        {
            Enabled = false;
            MinValue = double.MinValue;
            MaxValue = double.MaxValue;
        }

        /// <summary>
        /// 값이 필터를 통과하는지 확인
        /// </summary>
        /// <param name="value">확인할 값</param>
        /// <returns>필터 통과 여부</returns>
        public bool PassesFilter(double value)
        {
            if (!Enabled) return true;
            return value >= MinValue && value <= MaxValue;
        }
    }
}