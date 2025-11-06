using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CsvTimeSeriesViewer
{
    public partial class PressureAnalysisForm : Form
    {
        private Dictionary<string, CsvFileInfo> csvFiles;

        public PressureAnalysisForm(Dictionary<string, CsvFileInfo> files)
        {
            csvFiles = files;
            InitializeComponent();
            AnalyzeData();
        }

        private void AnalyzeData()
        {
            dgvAnalysis.Rows.Clear();

            foreach (var file in csvFiles)
            {
                // 압력 관련 컬럼을 더 넓게 검색 (Pressure, Torr, Pirani, Ion 등)
                var pressureColumns = file.Value.Headers.Where(h =>
                    h.Contains("Pressure", StringComparison.OrdinalIgnoreCase) ||
                    h.Contains("Torr", StringComparison.OrdinalIgnoreCase) ||
                    h.Contains("Pirani", StringComparison.OrdinalIgnoreCase) ||
                    h.Contains("Ion", StringComparison.OrdinalIgnoreCase) ||
                    h.Contains("ATM", StringComparison.OrdinalIgnoreCase)).ToList();

                // 디버그 정보
                System.Diagnostics.Debug.WriteLine($"파일: {file.Value.FileName}");
                System.Diagnostics.Debug.WriteLine($"  발견된 압력 컬럼: {string.Join(", ", pressureColumns)}");
                System.Diagnostics.Debug.WriteLine($"  선택된 컬럼: {string.Join(", ", file.Value.SelectedColumns)}");

                foreach (var col in pressureColumns)
                {
                    // 선택된 컬럼인지 확인
                    if (!file.Value.SelectedColumns.Contains(col))
                    {
                        System.Diagnostics.Debug.WriteLine($"  {col} - 선택되지 않음");
                        continue;
                    }

                    if (file.Value.DataColumns.ContainsKey(col) && file.Value.DataColumns[col].Count > 0)
                    {
                        var stats = PressureAnalysisTools.AnalyzePressureData(
                            file.Value.Timestamps,
                            file.Value.DataColumns[col]);

                        if (stats != null)
                        {
                            int rowIndex = dgvAnalysis.Rows.Add();
                            var row = dgvAnalysis.Rows[rowIndex];

                            row.Cells["colFile"].Value = file.Value.FileName;
                            row.Cells["colColumn"].Value = col;
                            row.Cells["colMin"].Value = $"{stats.Min:E2}";
                            row.Cells["colMax"].Value = $"{stats.Max:E2}";
                            row.Cells["colAvg"].Value = $"{stats.Average:E2}";
                            row.Cells["colStdDev"].Value = $"{stats.StdDev:E2}";
                            row.Cells["colVacuumLevel"].Value = $"{stats.MinVacuumLevel} - {stats.MaxVacuumLevel}";
                            row.Cells["colLeakRate"].Value = $"{stats.LeakRate:E2}";
                            row.Cells["colSpikes"].Value = stats.SpikeCount;

                            // 리크율에 따른 색상 표시
                            if (stats.LeakRate > 1e-5)
                            {
                                row.Cells["colLeakRate"].Style.BackColor = Color.LightCoral;
                            }
                            else if (stats.LeakRate > 1e-6)
                            {
                                row.Cells["colLeakRate"].Style.BackColor = Color.LightYellow;
                            }
                            else
                            {
                                row.Cells["colLeakRate"].Style.BackColor = Color.LightGreen;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  {col} - 데이터 없음");
                    }
                }
            }

            // 분석 결과가 없을 경우 메시지 표시
            if (dgvAnalysis.Rows.Count == 0)
            {
                MessageBox.Show("분석할 수 있는 압력 데이터가 없습니다.\n\n" +
                    "다음 사항을 확인하세요:\n" +
                    "1. 압력 관련 컬럼이 선택되어 있는지 확인\n" +
                    "2. 데이터가 로드되어 있는지 확인\n" +
                    "3. 컬럼명에 Pressure, Torr, Pirani, Ion, ATM 등이 포함되어 있는지 확인",
                    "데이터 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

            private void BtnRefresh_Click(object sender, EventArgs e)
        {
            AnalyzeData();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*";
                saveDialog.Title = "분석 결과 내보내기";
                saveDialog.FileName = $"PressureAnalysis_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    ExportToCSV(saveDialog.FileName);
                    MessageBox.Show("분석 결과가 저장되었습니다.", "완료",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExportToCSV(string filename)
        {
            var lines = new List<string>();

            // 헤더
            var headers = new List<string>();
            foreach (DataGridViewColumn col in dgvAnalysis.Columns)
            {
                headers.Add(col.HeaderText);
            }
            lines.Add(string.Join(",", headers));

            // 데이터
            foreach (DataGridViewRow row in dgvAnalysis.Rows)
            {
                if (!row.IsNewRow)
                {
                    var values = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        values.Add(cell.Value?.ToString() ?? "");
                    }
                    lines.Add(string.Join(",", values));
                }
            }

            System.IO.File.WriteAllLines(filename, lines);
        }

        private void DgvAnalysis_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvAnalysis.Columns[e.ColumnIndex].Name == "colVacuumLevel")
            {
                string value = e.Value?.ToString() ?? "";
                if (value.Contains("HighVacuum"))
                {
                    e.CellStyle.ForeColor = Color.Blue;
                }
                else if (value.Contains("MediumVacuum"))
                {
                    e.CellStyle.ForeColor = Color.Green;
                }
                else if (value.Contains("RoughVacuum"))
                {
                    e.CellStyle.ForeColor = Color.Orange;
                }
            }
        }
    }
}