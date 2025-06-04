using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ScottPlot;
using ScottPlot.Plottable;

namespace CsvTimeSeriesViewer
{
    public partial class MainForm : Form
    {
        private Dictionary<string, CsvFileInfo> csvFiles;
        private System.Threading.Timer updateTimer;
        private object dataLock = new object();
        private bool isMonitoringEnabled = true;
        private ScatterPlot selectedPlot = null;
        private int selectedPointIndex = -1;
        private Crosshair crossHair;
        private MarkerPlot selectedMarker = null;

        public MainForm()
        {
            try
            {
                InitializeComponent();
                csvFiles = new Dictionary<string, CsvFileInfo>();

                // SplitContainer 설정
                SetupSplitContainers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"폼 초기화 오류:\n{ex.Message}\n\n{ex.StackTrace}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void SetupSplitContainers()
        {
            // SplitContainer에 더블클릭 이벤트 추가 (패널 접기/펼치기)
            splitMain.SplitterMoved += (s, e) =>
            {
                if (splitMain.SplitterDistance < 50)
                    splitMain.SplitterDistance = 0;
            };

            splitLeft.SplitterMoved += (s, e) =>
            {
                if (splitLeft.SplitterDistance < 50)
                    splitLeft.SplitterDistance = 0;
            };

            // 초기 크기 설정
            splitMain.SplitterDistance = 600;
            splitLeft.SplitterDistance = 295;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializePlot();
            StartUpdateTimer();
            InitializeAnalysisMenu();
        }

        private void InitializeAnalysisMenu()
        {
            try
            {
                // null 체크 추가
                if (contextMenuAnalysis == null)
                {
                    contextMenuAnalysis = new ContextMenuStrip();
                }

                // 압력 분석 컨텍스트 메뉴 설정
                var menuPressureAnalysis = new ToolStripMenuItem("압력 데이터 분석");
                menuPressureAnalysis.Click += (s, e) => ShowPressureAnalysis();

                var menuLeakTest = new ToolStripMenuItem("리크 테스트");
                menuLeakTest.Click += (s, e) => ShowLeakTest();

                var menuPumpdownCurve = new ToolStripMenuItem("펌프다운 곡선 분석");
                menuPumpdownCurve.Click += (s, e) => ShowPumpdownAnalysis();

                var menuExportReport = new ToolStripMenuItem("분석 리포트 내보내기");
                menuExportReport.Click += (s, e) => ExportAnalysisReport();

                contextMenuAnalysis.Items.Clear(); // 기존 항목 제거
                contextMenuAnalysis.Items.AddRange(new ToolStripItem[] {
                    menuPressureAnalysis,
                    menuLeakTest,
                    menuPumpdownCurve,
                    new ToolStripSeparator(),
                    menuExportReport
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"메뉴 초기화 오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializePlot()
        {
            formsPlot.Plot.Title("다중 CSV 시계열 데이터");
            formsPlot.Plot.XLabel("시간");
            formsPlot.Plot.YLabel("압력 (Torr)");
            formsPlot.Plot.Style(Style.Seaborn);

            // ScottPlot 기본 메뉴 완전 비활성화
            formsPlot.RightClicked -= formsPlot.DefaultRightClickEvent;
            formsPlot.Configuration.EnableRightClickMenu = false;
            formsPlot.Configuration.EnableRightClickZoom = false;

            // ScottPlot 상호작용 기능 설정
            formsPlot.Configuration.Quality = ScottPlot.Control.QualityMode.High;
            formsPlot.Configuration.DoubleClickBenchmark = false;
            formsPlot.Configuration.Pan = true;
            formsPlot.Configuration.Zoom = true;
            formsPlot.Configuration.ScrollWheelZoom = true;
            formsPlot.Configuration.MiddleClickAutoAxis = true;
            formsPlot.Configuration.MiddleClickDragZoom = false;
            formsPlot.Configuration.LockVerticalAxis = false;
            formsPlot.Configuration.LockHorizontalAxis = false;

            // 마우스 이벤트 핸들러
            formsPlot.MouseMove += FormsPlot_MouseMove;
            formsPlot.MouseClick += FormsPlot_MouseClick;
            formsPlot.MouseDoubleClick += FormsPlot_MouseDoubleClick;
            formsPlot.MouseUp += FormsPlot_MouseUp;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // 크로스헤어 추가
            crossHair = formsPlot.Plot.AddCrosshair(0, 0);
            crossHair.IsVisible = false;
            crossHair.LineWidth = 1;
            crossHair.Color = Color.Gray;

            formsPlot.Refresh();
        }

        private void FormsPlot_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // 선택 해제
                if (selectedMarker != null)
                {
                    formsPlot.Plot.Remove(selectedMarker);
                    selectedMarker = null;
                    crossHair.IsVisible = false;
                    lblCrosshair.BackColor = Color.FromArgb(255, 255, 192); // 원래 색상으로
                    formsPlot.Refresh();
                }

                // 컨텍스트 메뉴 표시
                ShowPlotContextMenu(e.Location);
            }
        }

        private void ShowPlotContextMenu(Point location)
        {
            var cm = new ContextMenuStrip();

            // 자동 축 조정
            var autoAxis = new ToolStripMenuItem("자동 축 조정");
            autoAxis.Click += (s, ev) => { formsPlot.Plot.AxisAuto(); formsPlot.Refresh(); };
            cm.Items.Add(autoAxis);

            // Y축 로그 스케일 토글
            var logScale = new ToolStripMenuItem("Y축 로그 스케일");
            logScale.Checked = isLogScale;
            logScale.Click += (s, ev) => { ToggleLogScale(); };
            cm.Items.Add(logScale);

            cm.Items.Add(new ToolStripSeparator());

            // 이미지로 저장
            var saveImage = new ToolStripMenuItem("이미지로 저장...");
            saveImage.Click += (s, ev) => SavePlotImage();
            cm.Items.Add(saveImage);

            // 데이터 내보내기
            var exportData = new ToolStripMenuItem("데이터 내보내기...");
            exportData.Click += (s, ev) => ExportPlotData();
            cm.Items.Add(exportData);

            cm.Items.Add(new ToolStripSeparator());

            // 그리드 토글
            var gridToggle = new ToolStripMenuItem("그리드 표시");
            gridToggle.Checked = true;
            gridToggle.Click += (s, ev) =>
            {
                formsPlot.Plot.Grid(!gridToggle.Checked);
                formsPlot.Refresh();
            };
            cm.Items.Add(gridToggle);

            // 범례 토글
            var legendToggle = new ToolStripMenuItem("범례 표시");
            legendToggle.Checked = isLegendVisible;
            legendToggle.Click += (s, ev) =>
            {
                isLegendVisible = !isLegendVisible;
                UpdatePlot();
            };
            cm.Items.Add(legendToggle);

            cm.Items.Add(new ToolStripSeparator());

            // 축 고정/해제
            var lockX = new ToolStripMenuItem("X축 고정");
            lockX.Checked = formsPlot.Configuration.LockHorizontalAxis;
            lockX.Click += (s, ev) =>
            {
                formsPlot.Configuration.LockHorizontalAxis = !formsPlot.Configuration.LockHorizontalAxis;
            };
            cm.Items.Add(lockX);

            var lockY = new ToolStripMenuItem("Y축 고정");
            lockY.Checked = formsPlot.Configuration.LockVerticalAxis;
            lockY.Click += (s, ev) =>
            {
                formsPlot.Configuration.LockVerticalAxis = !formsPlot.Configuration.LockVerticalAxis;
            };
            cm.Items.Add(lockY);

            cm.Show(formsPlot, location);
        }

        private void FormsPlot_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 더블클릭으로 자동 축 조정
                formsPlot.Plot.AxisAuto();
                formsPlot.Refresh();
            }
        }

        private bool isLogScale = false;
        private bool isLegendVisible = true;

        private void ToggleLogScale()
        {
            isLogScale = !isLogScale;
            UpdatePlot();
        }

        private void SavePlotImage()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG 이미지|*.png|JPG 이미지|*.jpg|BMP 이미지|*.bmp|모든 파일|*.*";
                sfd.Title = "그래프 이미지 저장";
                sfd.FileName = $"압력그래프_{DateTime.Now:yyyyMMdd_HHmmss}.png";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    formsPlot.Plot.SaveFig(sfd.FileName);
                    MessageBox.Show("이미지가 저장되었습니다.", "완료",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExportPlotData()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV 파일|*.csv|텍스트 파일|*.txt|모든 파일|*.*";
                sfd.Title = "플롯 데이터 내보내기";
                sfd.FileName = $"플롯데이터_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ExportVisibleDataToCSV(sfd.FileName);
                    MessageBox.Show("데이터가 저장되었습니다.", "완료",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExportVisibleDataToCSV(string filename)
        {
            var lines = new List<string>();
            lines.Add("Timestamp,FileName,Column,Value");

            var limits = formsPlot.Plot.GetAxisLimits();

            foreach (var file in csvFiles)
            {
                var fileInfo = file.Value;

                for (int i = 0; i < fileInfo.Timestamps.Count; i++)
                {
                    double x = fileInfo.Timestamps[i].ToOADate();

                    // 현재 보이는 범위의 데이터만 내보내기
                    if (x >= limits.XMin && x <= limits.XMax)
                    {
                        foreach (var col in fileInfo.DataColumns)
                        {
                            if (i < col.Value.Count && !double.IsNaN(col.Value[i]))
                            {
                                lines.Add($"{fileInfo.Timestamps[i]:yyyy-MM-dd HH:mm:ss}," +
                                         $"{fileInfo.FileName},{col.Key},{col.Value[i]}");
                            }
                        }
                    }
                }
            }

            File.WriteAllLines(filename, lines);
        }

        private void FormsPlot_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                (double mouseCoordX, double mouseCoordY) = formsPlot.GetMouseCoordinates();

                // 마우스 근처의 데이터 포인트 찾기
                var nearestPoint = FindNearestPoint(mouseCoordX, mouseCoordY);
                if (nearestPoint != null)
                {
                    double xValue = nearestPoint.Item1;
                    double yValue = nearestPoint.Item2;

                    // 시간 변환
                    DateTime time;
                    try
                    {
                        time = DateTime.FromOADate(xValue);
                    }
                    catch
                    {
                        // OADate 변환 실패 시 기본값
                        time = new DateTime(2025, 6, 4);
                    }

                    // 압력값 포맷팅
                    string valueStr = FormatPressureValue(yValue);

                    // 파일명과 컬럼명 분리
                    string[] labelParts = nearestPoint.Item3.Split(':');
                    string fileName = labelParts.Length > 0 ? labelParts[0].Trim() : "";
                    string columnName = labelParts.Length > 1 ? labelParts[1].Trim() : nearestPoint.Item3;

                    lblCrosshair.Text = $"파일: {fileName}\n" +
                                       $"컬럼: {columnName}\n" +
                                       $"시간: {time:yyyy-MM-dd HH:mm:ss}\n" +
                                       $"값: {valueStr}";
                    lblCrosshair.Visible = true;

                    // 크로스헤어 위치 업데이트 (그래프상의 위치 사용)
                    if (crossHair != null && nearestPoint.Item4 != null && nearestPoint.Item5 < nearestPoint.Item4.Ys.Length)
                    {
                        crossHair.X = xValue;
                        crossHair.Y = nearestPoint.Item4.Ys[nearestPoint.Item5];
                        crossHair.IsVisible = true;
                    }
                }
                else
                {
                    lblCrosshair.Visible = false;
                    if (crossHair != null)
                    {
                        crossHair.IsVisible = false;
                    }
                }

                formsPlot.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MouseMove error: {ex.Message}");
            }
        }

        private string FormatPressureValue(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "N/A";

            // 0 체크
            if (value == 0)
                return "0.00 Torr";

            // 절대값으로 처리 (음수 방지)
            double absValue = Math.Abs(value);

            if (absValue >= 100)
                return $"{value:F0} Torr";
            else if (absValue >= 1)
                return $"{value:F2} Torr";
            else if (absValue >= 1e-3)
                return $"{value:F4} Torr";
            else if (absValue >= 1e-6)
                return $"{value:E2} Torr";
            else if (absValue >= 1e-9)
                return $"{value:E3} Torr";
            else
                return $"{value:E3} Torr";
        }

        private void FormsPlot_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                try
                {
                    (double mouseX, double mouseY) = formsPlot.GetMouseCoordinates();
                    var nearestPoint = FindNearestPoint(mouseX, mouseY);

                    if (nearestPoint != null)
                    {
                        // 기존 마커 제거
                        if (selectedMarker != null)
                        {
                            formsPlot.Plot.Remove(selectedMarker);
                            selectedMarker = null;
                        }

                        selectedPlot = nearestPoint.Item4;
                        selectedPointIndex = nearestPoint.Item5;

                        // 그래프상의 Y 위치 사용
                        double markerX = nearestPoint.Item1;
                        double markerY = selectedPlot.Ys[selectedPointIndex];

                        // 선택된 포인트에 마커 추가
                        selectedMarker = formsPlot.Plot.AddMarker(markerX, markerY);
                        selectedMarker.MarkerSize = 12;
                        selectedMarker.MarkerShape = MarkerShape.filledCircle;
                        selectedMarker.MarkerColor = Color.Red;

                        // 실제 값 표시
                        double actualValue = nearestPoint.Item2;
                        selectedMarker.Text = FormatPressureValue(actualValue);
                        selectedMarker.TextFont.Size = 11;
                        selectedMarker.TextFont.Bold = true;
                        selectedMarker.TextFont.Color = Color.DarkRed;

                        // 크로스헤어 고정
                        crossHair.IsVisible = true;
                        crossHair.X = markerX;
                        crossHair.Y = markerY;

                        // 정보 업데이트
                        DateTime time = DateTime.FromOADate(markerX);
                        string[] labelParts = nearestPoint.Item3.Split(':');
                        string fileName = labelParts.Length > 0 ? labelParts[0].Trim() : "";
                        string columnName = labelParts.Length > 1 ? labelParts[1].Trim() : nearestPoint.Item3;

                        lblCrosshair.Text = $"[선택됨]\n" +
                                           $"파일: {fileName}\n" +
                                           $"컬럼: {columnName}\n" +
                                           $"시간: {time:yyyy-MM-dd HH:mm:ss}\n" +
                                           $"값: {FormatPressureValue(actualValue)}";
                        lblCrosshair.BackColor = Color.LightYellow;
                        lblCrosshair.Visible = true;

                        formsPlot.Refresh();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MouseClick error: {ex.Message}");
                }
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (selectedPlot != null && selectedPointIndex >= 0)
            {
                bool moved = false;

                if (e.KeyCode == Keys.Left && selectedPointIndex > 0)
                {
                    selectedPointIndex--;
                    moved = true;
                }
                else if (e.KeyCode == Keys.Right && selectedPointIndex < selectedPlot.Xs.Length - 1)
                {
                    selectedPointIndex++;
                    moved = true;
                }

                if (moved)
                {
                    double x = selectedPlot.Xs[selectedPointIndex];
                    double y = selectedPlot.Ys[selectedPointIndex];

                    crossHair.X = x;
                    crossHair.Y = y;

                    // 실제 데이터 값 찾기
                    double actualValue = y;
                    foreach (var file in csvFiles.Values)
                    {
                        if (selectedPlot.Label.Contains(file.FileName))
                        {
                            foreach (var col in file.DataColumns)
                            {
                                if (selectedPlot.Label.Contains(col.Key) &&
                                    selectedPointIndex < col.Value.Count)
                                {
                                    actualValue = col.Value[selectedPointIndex];
                                    break;
                                }
                            }
                            break;
                        }
                    }

                    DateTime time = DateTime.FromOADate(x);

                    // 파일명과 컬럼명 분리
                    string[] labelParts = selectedPlot.Label.Split(':');
                    string fileName = labelParts.Length > 0 ? labelParts[0].Trim() : "";
                    string columnName = labelParts.Length > 1 ? labelParts[1].Trim() : selectedPlot.Label;

                    lblCrosshair.Text = $"파일: {fileName}\n" +
                                       $"컬럼: {columnName}\n" +
                                       $"시간: {time:yyyy-MM-dd HH:mm:ss}\n" +
                                       $"값: {FormatPressureValue(actualValue)}";
                    lblCrosshair.Visible = true;

                    // 마커 업데이트
                    if (selectedMarker != null)
                    {
                        formsPlot.Plot.Remove(selectedMarker);
                    }

                    selectedMarker = formsPlot.Plot.AddMarker(x, y);
                    selectedMarker.MarkerSize = 10;
                    selectedMarker.MarkerShape = MarkerShape.filledCircle;
                    selectedMarker.MarkerColor = Color.Red;
                    selectedMarker.Text = FormatPressureValue(actualValue);
                    selectedMarker.TextFont.Size = 12;
                    selectedMarker.TextFont.Bold = true;

                    formsPlot.Refresh();
                }
            }
        }

        private Tuple<double, double, string, ScatterPlot, int> FindNearestPoint(double mouseX, double mouseY)
        {
            double minDistance = double.MaxValue;
            Tuple<double, double, string, ScatterPlot, int> nearestPoint = null;

            var plottables = formsPlot.Plot.GetPlottables();

            // 현재 축 범위 가져오기
            var limits = formsPlot.Plot.GetAxisLimits();
            double xRange = limits.XMax - limits.XMin;
            double yRange = limits.YMax - limits.YMin;

            foreach (var plottable in plottables)
            {
                if (plottable is ScatterPlot plot)
                {
                    if (plot.Xs == null || plot.Ys == null || plot.Xs.Length == 0) continue;
                    if (string.IsNullOrEmpty(plot.Label)) continue; // 레이블이 없는 플롯 무시

                    // 파일명과 컬럼명 파싱
                    string fileName = "";
                    string columnName = "";
                    if (plot.Label.Contains(":"))
                    {
                        string[] parts = plot.Label.Split(':');
                        fileName = parts[0].Trim();
                        columnName = parts[1].Trim();
                    }
                    else
                    {
                        columnName = plot.Label;
                    }

                    // 해당 파일의 실제 데이터 찾기
                    CsvFileInfo fileInfo = null;
                    foreach (var file in csvFiles.Values)
                    {
                        string shortFileName = Path.GetFileNameWithoutExtension(file.FileName);
                        if (fileName.Equals(shortFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            fileInfo = file;
                            break;
                        }
                    }

                    // 마우스 위치에 가장 가까운 데이터 포인트 찾기
                    for (int i = 0; i < plot.Xs.Length && i < plot.Ys.Length; i++)
                    {
                        // 정규화된 거리 계산
                        double xDist = (plot.Xs[i] - mouseX) / xRange;
                        double yDist = (plot.Ys[i] - mouseY) / yRange;
                        double distance = Math.Sqrt(xDist * xDist + yDist * yDist);

                        if (distance < minDistance)
                        {
                            minDistance = distance;

                            // 실제 데이터 값 찾기
                            double actualValue = plot.Ys[i];
                            if (fileInfo != null && fileInfo.DataColumns.ContainsKey(columnName))
                            {
                                var dataColumn = fileInfo.DataColumns[columnName];
                                if (i < dataColumn.Count && !double.IsNaN(dataColumn[i]))
                                {
                                    actualValue = dataColumn[i];
                                    System.Diagnostics.Debug.WriteLine($"Found actual value: {actualValue} for {columnName} at index {i}");
                                }
                            }

                            nearestPoint = new Tuple<double, double, string, ScatterPlot, int>(
                                plot.Xs[i], actualValue, plot.Label, plot, i);
                        }
                    }
                }
            }

            // 너무 멀리 떨어진 포인트는 무시 (화면의 5% 이상)
            if (minDistance > 0.05)
                return null;

            return nearestPoint;
        }

        private void ChkEnableMonitoring_CheckedChanged(object sender, EventArgs e)
        {
            isMonitoringEnabled = chkEnableMonitoring.Checked;

            if (isMonitoringEnabled)
            {
                StartUpdateTimer();
                lblStatus.Text = "실시간 모니터링 활성화됨";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                StopUpdateTimer();
                lblStatus.Text = "실시간 모니터링 중지됨";
                lblStatus.ForeColor = Color.Orange;
            }
        }

        private void BtnFilter_Click(object sender, EventArgs e)
        {
            using (var filterForm = new FilterForm(csvFiles))
            {
                if (filterForm.ShowDialog() == DialogResult.OK)
                {
                    UpdateGraph(null);
                }
            }
        }

        private void BtnAddFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*";
                openFileDialog.Title = "CSV 파일 선택";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        AddCsvFile(filePath);
                    }
                    UpdateColumnTree();
                }
            }
        }

        private void AddCsvFile(string filePath)
        {
            if (csvFiles.ContainsKey(filePath))
            {
                MessageBox.Show($"파일이 이미 추가되어 있습니다: {Path.GetFileName(filePath)}",
                              "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var fileInfo = new CsvFileInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            // 파일 감시자 설정
            string directory = Path.GetDirectoryName(filePath);
            fileInfo.Watcher = new FileSystemWatcher(directory)
            {
                Filter = fileInfo.FileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            fileInfo.Watcher.Changed += (s, e) => OnFileChanged(filePath);
            fileInfo.Watcher.EnableRaisingEvents = true;

            // 헤더 읽기
            LoadCsvHeaders(fileInfo);

            csvFiles[filePath] = fileInfo;
            lstFiles.Items.Add(fileInfo.FileName);

            lblStatus.Text = $"{csvFiles.Count}개 파일 모니터링 중...";
            lblStatus.ForeColor = Color.Green;
        }

        private void BtnRemoveFile_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem != null)
            {
                string fileName = lstFiles.SelectedItem.ToString();
                var fileToRemove = csvFiles.FirstOrDefault(f => f.Value.FileName == fileName);

                if (fileToRemove.Key != null)
                {
                    // 파일 감시자 정리
                    if (fileToRemove.Value.Watcher != null)
                    {
                        fileToRemove.Value.Watcher.EnableRaisingEvents = false;
                        fileToRemove.Value.Watcher.Dispose();
                    }

                    csvFiles.Remove(fileToRemove.Key);
                    lstFiles.Items.Remove(fileName);

                    UpdateColumnTree();
                    UpdateGraph(null);
                }
            }
        }

        private void LoadCsvHeaders(CsvFileInfo fileInfo)
        {
            try
            {
                using (var fs = new FileStream(fileInfo.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    string headerLine = reader.ReadLine();
                    if (headerLine != null)
                    {
                        fileInfo.Headers = headerLine.Split(',').Select(h => h.Trim()).ToList();

                        // Timestamp 컬럼 자동 감지
                        for (int i = 0; i < fileInfo.Headers.Count; i++)
                        {
                            string header = fileInfo.Headers[i];
                            if (header.Contains("Timestamp", StringComparison.OrdinalIgnoreCase) ||
                                header.Contains("Time", StringComparison.OrdinalIgnoreCase) ||
                                header.Contains("Date", StringComparison.OrdinalIgnoreCase))
                            {
                                fileInfo.TimeColumnIndex = i;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"헤더 읽기 오류 ({fileInfo.FileName}): {ex.Message}",
                              "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateColumnTree()
        {
            trvColumns.BeginUpdate();
            trvColumns.Nodes.Clear();

            foreach (var file in csvFiles.Values)
            {
                var fileNode = new TreeNode(file.FileName)
                {
                    Tag = file.FilePath,
                    Checked = false
                };

                // 시간 컬럼 선택 노드
                string timeColumnText = file.TimeColumnIndex >= 0
                    ? file.Headers[file.TimeColumnIndex]
                    : "(행 번호 사용)";

                var timeNode = new TreeNode($"시간 컬럼: {timeColumnText}")
                {
                    Tag = "TIME_COLUMN",
                    ForeColor = Color.DarkBlue,
                    NodeFont = new Font(trvColumns.Font, FontStyle.Bold)
                };
                fileNode.Nodes.Add(timeNode);

                // 데이터 컬럼 노드들
                for (int i = 0; i < file.Headers.Count; i++)
                {
                    var columnNode = new TreeNode(file.Headers[i])
                    {
                        Tag = i,
                        Checked = file.SelectedColumns.Contains(file.Headers[i])
                    };

                    // 압력 관련 컬럼 자동 선택
                    if (file.SelectedColumns.Count == 0 &&
                        (file.Headers[i].Contains("Pressure", StringComparison.OrdinalIgnoreCase) ||
                         file.Headers[i].Contains("Ion", StringComparison.OrdinalIgnoreCase) ||
                         file.Headers[i].Contains("Pirani", StringComparison.OrdinalIgnoreCase) ||
                         file.Headers[i].Contains("ATM", StringComparison.OrdinalIgnoreCase)))
                    {
                        columnNode.Checked = true;
                        file.SelectedColumns.Add(file.Headers[i]);
                    }

                    // 필터가 적용된 컬럼 표시
                    if (file.Filters.ContainsKey(file.Headers[i]) && file.Filters[file.Headers[i]].Enabled)
                    {
                        columnNode.ForeColor = Color.Red;
                        columnNode.Text += " [필터]";
                    }

                    fileNode.Nodes.Add(columnNode);
                }

                fileNode.Expand();
                trvColumns.Nodes.Add(fileNode);
            }

            trvColumns.EndUpdate();
        }

        private void TrvColumns_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Level == 0) // 파일 노드
            {
                // 파일 노드 체크 시 모든 하위 노드 체크
                foreach (TreeNode childNode in e.Node.Nodes)
                {
                    if (childNode.Tag.ToString() != "TIME_COLUMN")
                    {
                        childNode.Checked = e.Node.Checked;
                    }
                }
            }
            else if (e.Node.Level == 1) // 컬럼 노드
            {
                string filePath = e.Node.Parent.Tag.ToString();
                if (csvFiles.ContainsKey(filePath))
                {
                    var fileInfo = csvFiles[filePath];

                    if (e.Node.Tag.ToString() == "TIME_COLUMN")
                    {
                        // 시간 컬럼 선택 다이얼로그
                        e.Node.Checked = false; // 체크박스는 사용하지 않음
                        ShowTimeColumnDialog(fileInfo, e.Node);
                    }
                    else
                    {
                        int columnIndex = (int)e.Node.Tag;
                        string columnName = fileInfo.Headers[columnIndex];

                        if (e.Node.Checked)
                        {
                            fileInfo.SelectedColumns.Add(columnName);
                        }
                        else
                        {
                            fileInfo.SelectedColumns.Remove(columnName);
                        }
                    }
                }
            }

            UpdateGraph(null);
        }

        private void ShowTimeColumnDialog(CsvFileInfo fileInfo, TreeNode timeNode)
        {
            using (var form = new Form())
            {
                form.Text = "시간 컬럼 선택";
                form.Size = new Size(350, 200);
                form.StartPosition = FormStartPosition.CenterParent;

                var lblInfo = new Label
                {
                    Text = "시간 데이터로 사용할 컬럼을 선택하세요.\n" +
                           "Timestamp 컬럼이 있으면 선택하세요.",
                    Location = new Point(20, 20),
                    Size = new Size(300, 40),
                    AutoSize = false
                };

                var cmbColumns = new ComboBox
                {
                    Location = new Point(20, 70),
                    Size = new Size(290, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                cmbColumns.Items.Add("(행 번호 사용 - 파일 시간 기준)");

                // Timestamp 컬럼을 우선 추가
                foreach (var header in fileInfo.Headers)
                {
                    if (header.Contains("Timestamp", StringComparison.OrdinalIgnoreCase) ||
                        header.Contains("Time", StringComparison.OrdinalIgnoreCase) ||
                        header.Contains("Date", StringComparison.OrdinalIgnoreCase))
                    {
                        cmbColumns.Items.Insert(1, header);
                    }
                    else
                    {
                        cmbColumns.Items.Add(header);
                    }
                }

                // 현재 선택 복원
                if (fileInfo.TimeColumnIndex == -1)
                {
                    cmbColumns.SelectedIndex = 0;
                }
                else
                {
                    string currentColumn = fileInfo.Headers[fileInfo.TimeColumnIndex];
                    int index = cmbColumns.Items.IndexOf(currentColumn);
                    cmbColumns.SelectedIndex = index >= 0 ? index : 0;
                }

                var btnOK = new Button
                {
                    Text = "확인",
                    Location = new Point(120, 120),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.OK
                };

                form.Controls.AddRange(new Control[] { lblInfo, cmbColumns, btnOK });

                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (cmbColumns.SelectedIndex == 0)
                    {
                        fileInfo.TimeColumnIndex = -1;
                    }
                    else
                    {
                        string selectedColumn = cmbColumns.SelectedItem.ToString();
                        fileInfo.TimeColumnIndex = fileInfo.Headers.IndexOf(selectedColumn);
                    }

                    timeNode.Text = $"시간 컬럼: {cmbColumns.SelectedItem}";

                    // 데이터 다시 읽기
                    UpdateGraph(null);
                }
            }
        }

        private void LstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 선택된 파일의 컬럼을 트리뷰에서 하이라이트
            if (lstFiles.SelectedItem != null)
            {
                string selectedFileName = lstFiles.SelectedItem.ToString();
                foreach (TreeNode node in trvColumns.Nodes)
                {
                    if (node.Text == selectedFileName)
                    {
                        trvColumns.SelectedNode = node;
                        node.EnsureVisible();
                        break;
                    }
                }
            }
        }

        private void OnFileChanged(string filePath)
        {
            if (isMonitoringEnabled)
            {
                UpdateGraph(null);
            }
        }

        private void StartUpdateTimer()
        {
            if (updateTimer == null)
            {
                int intervalMs = (int)(nudUpdateInterval.Value * 1000);
                updateTimer = new System.Threading.Timer(UpdateGraph, null, 0, intervalMs);
            }
        }

        private void StopUpdateTimer()
        {
            if (updateTimer != null)
            {
                updateTimer.Dispose();
                updateTimer = null;
            }
        }

        private void UpdateGraph(object state)
        {
            if (!isMonitoringEnabled && state != null) return; // 타이머에서 호출된 경우만 체크

            try
            {
                ReadAllCsvData();

                this.Invoke((MethodInvoker)delegate
                {
                    UpdatePlot();
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    lblStatus.Text = $"오류: {ex.Message}";
                    lblStatus.ForeColor = Color.Red;
                });
            }
        }

        private void ReadAllCsvData()
        {
            lock (dataLock)
            {
                foreach (var fileInfo in csvFiles.Values)
                {
                    ReadCsvFile(fileInfo);
                }
            }
        }

        private void ReadCsvFile(CsvFileInfo fileInfo)
        {
            fileInfo.DataColumns.Clear();
            fileInfo.Timestamps.Clear();

            if (fileInfo.SelectedColumns.Count == 0) return;

            foreach (string colName in fileInfo.SelectedColumns)
            {
                fileInfo.DataColumns[colName] = new List<double>();
            }

            int retryCount = 3;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using (var fs = new FileStream(fileInfo.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        string line;
                        bool isFirstLine = true;
                        int rowNumber = 0;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (isFirstLine)
                            {
                                isFirstLine = false;
                                continue;
                            }

                            string[] values = line.Split(',');

                            // 시간 처리
                            DateTime timestamp;
                            if (fileInfo.TimeColumnIndex == -1)
                            {
                                // 행 번호 사용 - 파일 수정 시간을 기준으로
                                var fileTime = File.GetLastWriteTime(fileInfo.FilePath);
                                timestamp = fileTime.AddSeconds(rowNumber);
                            }
                            else
                            {
                                // 선택된 컬럼에서 시간 파싱
                                if (fileInfo.TimeColumnIndex < values.Length)
                                {
                                    string timeStr = values[fileInfo.TimeColumnIndex].Trim();
                                    if (DateTime.TryParse(timeStr, out timestamp))
                                    {
                                        // 성공
                                    }
                                    else
                                    {
                                        // 파싱 실패 시 행 번호 사용
                                        var fileTime = File.GetLastWriteTime(fileInfo.FilePath);
                                        timestamp = fileTime.AddSeconds(rowNumber);
                                    }
                                }
                                else
                                {
                                    var fileTime = File.GetLastWriteTime(fileInfo.FilePath);
                                    timestamp = fileTime.AddSeconds(rowNumber);
                                }
                            }

                            // 선택된 데이터 컬럼 읽기
                            bool allColumnsFiltered = true;
                            var rowData = new Dictionary<string, double>();

                            foreach (string colName in fileInfo.SelectedColumns)
                            {
                                int colIndex = fileInfo.Headers.IndexOf(colName);
                                if (colIndex >= 0 && colIndex < values.Length)
                                {
                                    string valueStr = values[colIndex].Trim();

                                    // 과학적 표기법 처리 (E 표기법)
                                    valueStr = valueStr.Replace("E", "e"); // 대문자 E를 소문자로

                                    if (double.TryParse(valueStr, NumberStyles.Float | NumberStyles.AllowExponent,
                                                      CultureInfo.InvariantCulture, out double value))
                                    {
                                        // 필터 적용
                                        if (fileInfo.Filters.ContainsKey(colName))
                                        {
                                            var filter = fileInfo.Filters[colName];
                                            if (!filter.PassesFilter(value))
                                            {
                                                continue; // 이 컬럼은 필터에 걸림
                                            }
                                        }

                                        rowData[colName] = value;
                                        allColumnsFiltered = false;
                                    }
                                }
                            }

                            // 모든 컬럼이 필터에 걸리지 않은 경우만 추가
                            if (!allColumnsFiltered && rowData.Count > 0)
                            {
                                fileInfo.Timestamps.Add(timestamp);
                                foreach (var kvp in rowData)
                                {
                                    fileInfo.DataColumns[kvp.Key].Add(kvp.Value);
                                }

                                // 빈 값으로 채우기
                                foreach (string colName in fileInfo.SelectedColumns)
                                {
                                    if (!rowData.ContainsKey(colName))
                                    {
                                        fileInfo.DataColumns[colName].Add(double.NaN);
                                    }
                                }
                            }

                            rowNumber++;
                        }
                    }

                    // 디버깅: 읽은 데이터 확인
                    System.Diagnostics.Debug.WriteLine($"File: {fileInfo.FileName}");
                    System.Diagnostics.Debug.WriteLine($"Timestamps count: {fileInfo.Timestamps.Count}");
                    foreach (var col in fileInfo.DataColumns)
                    {
                        System.Diagnostics.Debug.WriteLine($"Column {col.Key}: {col.Value.Count} values");
                        if (col.Value.Count > 0)
                        {
                            var nonZeroValues = col.Value.Where(v => !double.IsNaN(v) && v != 0).Take(5).ToList();
                            if (nonZeroValues.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"  Sample values: {string.Join(", ", nonZeroValues.Select(v => v.ToString("E2")))}");
                            }
                        }
                    }

                    break;
                }
                catch (IOException)
                {
                    if (i == retryCount - 1) throw;
                    Thread.Sleep(100);
                }
            }
        }

        private void UpdatePlot()
        {
            lock (dataLock)
            {
                // 기존 플롯 저장
                var existingLimits = formsPlot.Plot.GetAxisLimits();

                formsPlot.Plot.Clear();

                // 크로스헤어 다시 추가
                crossHair = formsPlot.Plot.AddCrosshair(crossHair.X, crossHair.Y);
                crossHair.IsVisible = selectedPlot != null;

                // 선택된 마커 복원
                if (selectedMarker != null && selectedPlot != null && selectedPointIndex >= 0)
                {
                    selectedMarker = formsPlot.Plot.AddMarker(
                        selectedPlot.Xs[selectedPointIndex],
                        selectedPlot.Ys[selectedPointIndex]);
                    selectedMarker.MarkerSize = 10;
                    selectedMarker.MarkerShape = MarkerShape.filledCircle;
                    selectedMarker.MarkerColor = Color.Red;
                    selectedMarker.Text = $"{selectedPlot.Ys[selectedPointIndex]:F2}";
                    selectedMarker.TextFont.Size = 12;
                    selectedMarker.TextFont.Bold = true;
                }

                var colors = new[] {
                    Color.Blue, Color.Red, Color.Green, Color.Orange,
                    Color.Purple, Color.Brown, Color.Pink, Color.Gray,
                    Color.Cyan, Color.Magenta, Color.DarkBlue, Color.DarkRed,
                    Color.DarkGreen, Color.DarkOrange, Color.Indigo, Color.Olive
                };

                int colorIndex = 0;
                int totalPoints = 0;
                int totalColumns = 0;

                // 시간축 동기화를 위한 최소/최대 시간 찾기
                DateTime? minTime = null;
                DateTime? maxTime = null;

                if (chkSyncTimeAxis.Checked)
                {
                    foreach (var fileInfo in csvFiles.Values)
                    {
                        if (fileInfo.Timestamps.Count > 0)
                        {
                            var fileMin = fileInfo.Timestamps.Min();
                            var fileMax = fileInfo.Timestamps.Max();

                            minTime = minTime == null ? fileMin : (fileMin < minTime ? fileMin : minTime);
                            maxTime = maxTime == null ? fileMax : (fileMax > maxTime ? fileMax : maxTime);
                        }
                    }
                }

                foreach (var file in csvFiles)
                {
                    var fileInfo = file.Value;

                    if (fileInfo.Timestamps.Count == 0) continue;

                    // X축 데이터 (시간을 double로 변환)
                    double[] xs = fileInfo.Timestamps.Select(t => t.ToOADate()).ToArray();

                    foreach (var column in fileInfo.DataColumns)
                    {
                        if (column.Value.Count > 0)
                        {
                            double[] ys = column.Value.ToArray();

                            // 레이블에 파일명 포함 (짧게)
                            string shortFileName = Path.GetFileNameWithoutExtension(fileInfo.FileName);
                            string label = $"{shortFileName}: {column.Key}";

                            var signal = formsPlot.Plot.AddScatterLines(xs, ys);
                            signal.Color = colors[colorIndex % colors.Length];
                            signal.LineWidth = 2;
                            signal.Label = label;
                            signal.MarkerSize = 0; // 마커 제거로 성능 향상

                            // Ion 압력은 로그 스케일로 표시
                            if (column.Key.Contains("Ion"))
                            {
                                signal.LineStyle = LineStyle.Dash;
                            }

                            colorIndex++;
                            totalColumns++;
                        }
                    }

                    totalPoints += fileInfo.Timestamps.Count;
                }

                // X축을 날짜/시간 형식으로 설정
                formsPlot.Plot.XAxis.DateTimeFormat(true);

                // Y축 로그 스케일 설정 (압력 데이터에 적합)
                bool hasIonData = csvFiles.Values.Any(f =>
                    f.SelectedColumns.Any(c => c.Contains("Ion")));

                if (hasIonData)
                {
                    // 로그 스케일 적용
                    formsPlot.Plot.YAxis.MinimumTickSpacing(1);
                    formsPlot.Plot.YAxis.TickLabelNotation(invertSign: false);
                    formsPlot.Plot.YAxis.TickLabelStyle(fontSize: 10);

                    // Y축 범위 설정
                    double minY = 1e-10;
                    double maxY = 1000;

                    foreach (var file in csvFiles.Values)
                    {
                        foreach (var col in file.DataColumns)
                        {
                            if (col.Value.Count > 0)
                            {
                                var validValues = col.Value.Where(v => !double.IsNaN(v) && v > 0).ToList();
                                if (validValues.Count > 0)
                                {
                                    double colMin = validValues.Min();
                                    double colMax = validValues.Max();
                                    if (colMin > 0 && colMin < minY) minY = colMin * 0.5;
                                    if (colMax > maxY) maxY = colMax * 2;
                                }
                            }
                        }
                    }

                    formsPlot.Plot.SetAxisLimitsY(minY, maxY);
                    formsPlot.Plot.YAxis.ManualTickSpacing(Math.Pow(10, Math.Floor(Math.Log10(minY))));
                }

                // 축 범위 설정
                if (!chkAutoScale.Checked)
                {
                    // 기존 축 범위 유지
                    formsPlot.Plot.SetAxisLimits(existingLimits);
                }
                else if (chkSyncTimeAxis.Checked && minTime.HasValue && maxTime.HasValue)
                {
                    formsPlot.Plot.SetAxisLimits(
                        xMin: minTime.Value.ToOADate(),
                        xMax: maxTime.Value.ToOADate()
                    );
                }
                else
                {
                    formsPlot.Plot.AxisAuto();
                }

                if (totalColumns > 0)
                {
                    var legend = formsPlot.Plot.Legend(true);
                    legend.Location = Alignment.UpperRight;
                }

                formsPlot.Refresh();

                string monitoringStatus = isMonitoringEnabled ? "모니터링 중" : "모니터링 중지";
                lblStatus.Text = $"{monitoringStatus}... (파일: {csvFiles.Count}개, " +
                               $"총 데이터 포인트: {totalPoints}개, " +
                               $"표시 컬럼: {totalColumns}개, " +
                               $"마지막 업데이트: {DateTime.Now:HH:mm:ss})";
                lblStatus.ForeColor = isMonitoringEnabled ? Color.Green : Color.Orange;
            }
        }

        private void BtnRefreshAll_Click(object sender, EventArgs e)
        {
            foreach (var fileInfo in csvFiles.Values)
            {
                LoadCsvHeaders(fileInfo);
            }
            UpdateColumnTree();
            UpdateGraph(null);
        }

        private void NudUpdateInterval_ValueChanged(object sender, EventArgs e)
        {
            if (updateTimer != null && isMonitoringEnabled)
            {
                StopUpdateTimer();
                StartUpdateTimer();
            }
        }

        private void ChkAutoScale_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePlot();
        }

        private void ChkSyncTimeAxis_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePlot();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 모든 파일 감시자 정리
            foreach (var fileInfo in csvFiles.Values)
            {
                if (fileInfo.Watcher != null)
                {
                    fileInfo.Watcher.EnableRaisingEvents = false;
                    fileInfo.Watcher.Dispose();
                }
            }

            if (updateTimer != null)
            {
                updateTimer.Dispose();
            }
        }

        private void BtnAnalysis_Click(object sender, EventArgs e)
        {
            if (contextMenuAnalysis == null || contextMenuAnalysis.Items.Count == 0)
            {
                InitializeAnalysisMenu();
            }
            contextMenuAnalysis.Show(btnAnalysis, new Point(0, btnAnalysis.Height));
        }

        private void ShowPressureAnalysis()
        {
            if (csvFiles.Count == 0)
            {
                MessageBox.Show("분석할 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new PressureAnalysisForm(csvFiles))
            {
                form.ShowDialog();
            }
        }

        private void ShowLeakTest()
        {
            if (csvFiles.Count == 0)
            {
                MessageBox.Show("분석할 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 리크 테스트 다이얼로그 표시
            var results = new System.Text.StringBuilder();
            results.AppendLine("=== 리크 테스트 결과 ===\n");

            foreach (var file in csvFiles)
            {
                results.AppendLine($"파일: {file.Value.FileName}");

                // Ion_Pressure 컬럼 찾기
                var pressureColumns = file.Value.Headers.Where(h =>
                    h.Contains("Pressure", StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var col in pressureColumns)
                {
                    if (file.Value.DataColumns.ContainsKey(col))
                    {
                        var pressures = file.Value.DataColumns[col];
                        var times = file.Value.Timestamps;

                        double leakRate = PressureAnalysisTools.DetectLeakRate(times, pressures);
                        results.AppendLine($"  {col}: 리크율 = {leakRate:E2} Torr/sec");

                        if (leakRate > 1e-5)
                            results.AppendLine($"    ⚠️ 경고: 높은 리크율 감지!");
                    }
                }
                results.AppendLine();
            }

            MessageBox.Show(results.ToString(), "리크 테스트 결과",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowPumpdownAnalysis()
        {
            // 펌프다운 분석 구현
            MessageBox.Show("펌프다운 곡선 분석 기능은 개발 중입니다.", "알림",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportAnalysisReport()
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "텍스트 파일 (*.txt)|*.txt|모든 파일 (*.*)|*.*";
                saveDialog.Title = "분석 리포트 저장";
                saveDialog.FileName = $"PressureAnalysis_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var report = GenerateAnalysisReport();
                    System.IO.File.WriteAllText(saveDialog.FileName, report);
                    MessageBox.Show("리포트가 저장되었습니다.", "완료",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private string GenerateAnalysisReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"압력 데이터 분석 리포트");
            report.AppendLine($"생성일시: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine(new string('=', 50));

            foreach (var file in csvFiles)
            {
                report.AppendLine($"\n파일: {file.Value.FileName}");
                report.AppendLine(new string('-', 30));

                var pressureColumns = file.Value.Headers.Where(h =>
                    h.Contains("Pressure", StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var col in pressureColumns)
                {
                    if (file.Value.DataColumns.ContainsKey(col) && file.Value.DataColumns[col].Count > 0)
                    {
                        var stats = PressureAnalysisTools.AnalyzePressureData(
                            file.Value.Timestamps,
                            file.Value.DataColumns[col]);

                        if (stats != null)
                        {
                            report.AppendLine($"\n컬럼: {col}");
                            report.AppendLine($"  최소값: {stats.Min:E2} Torr ({stats.MinVacuumLevel})");
                            report.AppendLine($"  최대값: {stats.Max:E2} Torr ({stats.MaxVacuumLevel})");
                            report.AppendLine($"  평균: {stats.Average:E2} Torr");
                            report.AppendLine($"  표준편차: {stats.StdDev:E2} Torr");
                            report.AppendLine($"  스파이크 수: {stats.SpikeCount}");
                            report.AppendLine($"  리크율: {stats.LeakRate:E2} Torr/sec");

                            if (stats.StabilizationTime.HasValue)
                            {
                                report.AppendLine($"  안정화 시간: {stats.StabilizationTime.Value.TotalSeconds:F1} 초");
                            }
                        }
                    }
                }
            }

            return report.ToString();
        }
    }
}