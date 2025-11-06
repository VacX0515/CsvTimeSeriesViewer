using System;
using System.Collections.Generic;
using System.Linq;

namespace CsvTimeSeriesViewer
{
    /// <summary>
    /// 압력 데이터 분석을 위한 도구 클래스
    /// </summary>
    public static class PressureAnalysisTools
    {
        /// <summary>
        /// 압력 단위 변환 상수
        /// </summary>
        public const double TORR_TO_PA = 133.322;
        public const double TORR_TO_MBAR = 1.33322;
        public const double KPA_TO_TORR = 7.50062;
        public const double ATM_TO_TORR = 760.0;

        /// <summary>
        /// 진공 레벨 분류
        /// </summary>
        public enum VacuumLevel
        {
            Atmospheric,      // > 760 Torr
            RoughVacuum,      // 760 - 1 Torr
            MediumVacuum,     // 1 - 10^-3 Torr
            HighVacuum,       // 10^-3 - 10^-7 Torr
            UltraHighVacuum,  // 10^-7 - 10^-12 Torr
            ExtremeHighVacuum // < 10^-12 Torr
        }

        /// <summary>
        /// 압력값에 따른 진공 레벨 판단
        /// </summary>
        public static VacuumLevel GetVacuumLevel(double pressureTorr)
        {
            if (pressureTorr > 760) return VacuumLevel.Atmospheric;
            if (pressureTorr > 1) return VacuumLevel.RoughVacuum;
            if (pressureTorr > 1e-3) return VacuumLevel.MediumVacuum;
            if (pressureTorr > 1e-7) return VacuumLevel.HighVacuum;
            if (pressureTorr > 1e-12) return VacuumLevel.UltraHighVacuum;
            return VacuumLevel.ExtremeHighVacuum;
        }

        /// <summary>
        /// 펌프다운 속도 계산 (Torr/sec)
        /// </summary>
        public static double CalculatePumpingRate(List<DateTime> times, List<double> pressures)
        {
            if (times.Count < 2 || pressures.Count < 2) return 0;

            // 선형 회귀로 펌프다운 속도 계산
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            int n = Math.Min(times.Count, pressures.Count);

            double startTime = times[0].ToOADate();
            for (int i = 0; i < n; i++)
            {
                double x = (times[i].ToOADate() - startTime) * 24 * 3600; // 초 단위
                double y = Math.Log10(pressures[i]); // 로그 스케일

                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return -slope; // 음수일 때 압력 감소
        }

        /// <summary>
        /// 리크 레이트 감지 (압력 상승률)
        /// </summary>
        public static double DetectLeakRate(List<DateTime> times, List<double> pressures, int windowSize = 10)
        {
            if (times.Count < windowSize || pressures.Count < windowSize) return 0;

            // 최근 windowSize 개의 데이터로 압력 상승률 계산
            int startIdx = times.Count - windowSize;
            var recentTimes = times.GetRange(startIdx, windowSize);
            var recentPressures = pressures.GetRange(startIdx, windowSize);

            return -CalculatePumpingRate(recentTimes, recentPressures); // 양수일 때 압력 증가
        }

        /// <summary>
        /// 압력 스파이크 감지
        /// </summary>
        public static List<int> DetectPressureSpikes(List<double> pressures, double threshold = 2.0)
        {
            var spikes = new List<int>();
            if (pressures.Count < 3) return spikes;

            for (int i = 1; i < pressures.Count - 1; i++)
            {
                double prev = pressures[i - 1];
                double curr = pressures[i];
                double next = pressures[i + 1];

                // 이전과 다음 값의 평균보다 threshold 배 이상 크면 스파이크
                double avg = (prev + next) / 2;
                if (curr > avg * threshold)
                {
                    spikes.Add(i);
                }
            }

            return spikes;
        }

        /// <summary>
        /// 안정화 시간 계산 (목표 압력에 도달하는 시간)
        /// </summary>
        public static TimeSpan? CalculateStabilizationTime(List<DateTime> times, List<double> pressures,
            double targetPressure, double tolerance = 0.1)
        {
            if (times.Count == 0 || pressures.Count == 0) return null;

            DateTime startTime = times[0];

            for (int i = 0; i < pressures.Count; i++)
            {
                if (Math.Abs(pressures[i] - targetPressure) / targetPressure < tolerance)
                {
                    // 연속 5개 포인트가 안정 범위 내에 있는지 확인
                    bool isStable = true;
                    for (int j = i; j < Math.Min(i + 5, pressures.Count); j++)
                    {
                        if (Math.Abs(pressures[j] - targetPressure) / targetPressure >= tolerance)
                        {
                            isStable = false;
                            break;
                        }
                    }

                    if (isStable)
                    {
                        return times[i] - startTime;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 압력 통계 정보
        /// </summary>
        public class PressureStatistics
        {
            public double Min { get; set; }
            public double Max { get; set; }
            public double Average { get; set; }
            public double StdDev { get; set; }
            public double Median { get; set; }
            public VacuumLevel MinVacuumLevel { get; set; }
            public VacuumLevel MaxVacuumLevel { get; set; }
            public int SpikeCount { get; set; }
            public double LeakRate { get; set; }
            public TimeSpan? StabilizationTime { get; set; }
        }

        /// <summary>
        /// 압력 데이터 전체 분석
        /// </summary>
        public static PressureStatistics AnalyzePressureData(List<DateTime> times, List<double> pressures)
        {
            if (pressures == null || pressures.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("압력 데이터가 null이거나 비어있음");
                return null;
            }

            var validPressures = pressures.Where(p => !double.IsNaN(p) && !double.IsInfinity(p) && p > 0).ToList();

            System.Diagnostics.Debug.WriteLine($"전체 압력 데이터: {pressures.Count}개, 유효한 데이터: {validPressures.Count}개");

            if (validPressures.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("유효한 압력 데이터가 없음");
                return null;
            }

            var stats = new PressureStatistics
            {
                Min = validPressures.Min(),
                Max = validPressures.Max(),
                Average = validPressures.Average(),
                Median = GetMedian(validPressures),
                MinVacuumLevel = GetVacuumLevel(validPressures.Min()),
                MaxVacuumLevel = GetVacuumLevel(validPressures.Max()),
                SpikeCount = DetectPressureSpikes(validPressures).Count,
                LeakRate = times != null && times.Count >= validPressures.Count ?
                          DetectLeakRate(times.Take(validPressures.Count).ToList(), validPressures) : 0,
                StabilizationTime = times != null && times.Count >= validPressures.Count ?
                                  CalculateStabilizationTime(times.Take(validPressures.Count).ToList(),
                                                           validPressures, validPressures.Min() * 1.1) : null
            };

            // 표준편차 계산
            double variance = validPressures.Sum(p => Math.Pow(p - stats.Average, 2)) / validPressures.Count;
            stats.StdDev = Math.Sqrt(variance);

            System.Diagnostics.Debug.WriteLine($"분석 결과 - Min: {stats.Min:E2}, Max: {stats.Max:E2}, Avg: {stats.Average:E2}");

            return stats;
        }

        private static double GetMedian(List<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            int count = sorted.Count;
            if (count % 2 == 0)
            {
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            }
            else
            {
                return sorted[count / 2];
            }
        }
    }
}