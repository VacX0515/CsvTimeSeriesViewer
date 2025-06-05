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
    // 선택된 데이터 포인트를 위한 클래스
    public class SelectedPointData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Label { get; set; }
        public string File { get; set; }
        public string Column { get; set; }
    }

    public partial class MainForm : Form
    {
        private Dictionary<string, CsvFileInfo> csvFiles;
        private System.Threading.Timer updateTimer;
        private object dataLock = new object();
        private bool isMonitoringEnabled = true;
        private Crosshair crossHair;
        private MarkerPlot highlightMarker;
        private Text highlightText;
        private List<ScatterPlot> allPlots = new List<ScatterPlot>();

        // 드래그 선택 관련
        private bool isDragging = false;
        private Point dragStart;
        private Rectangle dragRect;
        private List<SelectedPointData> selectedPoints;
        private VSpan selectionSpan;

        // 다중 Y축 관련
        private Dictionary<string, int> columnToYAxisIndex;
        private List<Color> yAxisColors;

        // 기타 플래그
        private bool isLogScale = false;
        private bool isLegendVisible = true;

        private class SelectedPointData
        {
            public double X { get; set; }
            public double Y { get; set; }
            public string Label { get; set; }
            public string File { get; set; }
            public string Column { get; set; }
        }


        public MainForm()
        {
            try
            {
                InitializeComponent();
                csvFiles = new Dictionary<string, CsvFileInfo>();
                selectedPoints = new List<SelectedPointData>();  // 수정됨
                columnToYAxisIndex = new Dictionary<string, int>();
                yAxisColors = new List<Color> { Color.Black, Color.Blue, Color.Red, Color.Green, Color.Purple };
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
                if (contextMenuAnalysis == null)
                {
                    contextMenuAnalysis = new ContextMenuStrip();
                }

                var menuPressureAnalysis = new ToolStripMenuItem("압력 데이터 분석");
                menuPressureAnalysis.Click += (s, e) => ShowPressureAnalysis();

                var menuLeakTest = new ToolStripMenuItem("리크 테스트");
                menuLeakTest.Click += (s, e) => ShowLeakTest();

                var menuPumpdownCurve = new ToolStripMenuItem("펌프다운 곡선 분석");
                menuPumpdownCurve.Click += (s, e) => ShowPumpdownAnalysis();

                var menuExportReport = new ToolStripMenuItem("분석 리포트 내보내기");
                menuExportReport.Click += (s, e) => ExportAnalysisReport();

                contextMenuAnalysis.Items.Clear();
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
            formsPlot.Plot.YLabel("값");
            formsPlot.Plot.Style(Style.Seaborn);

            // ScottPlot 기본 메뉴 완전 비활성화
            formsPlot.RightClicked -= formsPlot.DefaultRightClickEvent;

            // ScottPlot 상호작용 기능 설정
            formsPlot.Configuration.Quality = ScottPlot.Control.QualityMode.High;
            formsPlot.Configuration.DoubleClickBenchmark = false;
            formsPlot.Configuration.Pan = true;
            formsPlot.Configuration.Zoom = true;
            formsPlot.Configuration.ScrollWheelZoom = true;
            formsPlot.Configuration.MiddleClickAutoAxis = true;
            formsPlot.Configuration.MiddleClickDragZoom = false;
            formsPlot.Configuration.RightClickDragZoom = false;
            formsPlot.Configuration.LockVerticalAxis = false;
            formsPlot.Configuration.LockHorizontalAxis = false;

            // 마우스 이벤트 핸들러
            formsPlot.MouseMove += FormsPlot_MouseMove;
            formsPlot.MouseDown += FormsPlot_MouseDown;
            formsPlot.MouseUp += FormsPlot_MouseUp;
            formsPlot.MouseDoubleClick += FormsPlot_MouseDoubleClick;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // 크로스헤어 추가
            crossHair = formsPlot.Plot.AddCrosshair(0, 0);
            crossHair.IsVisible = false;
            crossHair.LineWidth = 1;
            crossHair.Color = Color.Gray;

            // 하이라이트 마커 추가
            highlightMarker = formsPlot.Plot.AddMarker(0, 0);
            highlightMarker.MarkerShape = MarkerShape.openCircle;
            highlightMarker.MarkerSize = 15;
            highlightMarker.MarkerLineWidth = 2;
            highlightMarker.IsVisible = false;

            // 하이라이트 텍스트 추가
            highlightText = formsPlot.Plot.AddText("", 0, 0);
            highlightText.FontBold = true;
            highlightText.BackgroundFill = true;
            highlightText.BackgroundColor = Color.FromArgb(200, Color.White);
            highlightText.IsVisible = false;

            formsPlot.Refresh();
        }

        private void FormsPlot_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Control))
            {
                isDragging = true;
                dragStart = e.Location;
                dragRect = new Rectangle(e.X, e.Y, 0, 0);

                if (selectionSpan != null)
                {
                    formsPlot.Plot.Remove(selectionSpan);
                    selectionSpan = null;
                }
                selectedPoints.Clear();
            }
        }

        private void FormsPlot_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                int x = Math.Min(dragStart.X, e.X);
                int y = Math.Min(dragStart.Y, e.Y);
                int width = Math.Abs(e.X - dragStart.X);
                int height = Math.Abs(e.Y - dragStart.Y);
                dragRect = new Rectangle(x, y, width, height);

                formsPlot.Refresh();
                using (var g = formsPlot.CreateGraphics())
                {
                    using (var pen = new Pen(Color.Blue, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    {
                        g.DrawRectangle(pen, dragRect);
                    }
                }
            }
            else
            {
                var coords = formsPlot.GetMouseCoordinates();
                double mouseX = coords.x;
                double mouseY = coords.y;

                bool pointFound = false;
                double nearestDistance = double.MaxValue;
                string nearestFile = "";
                string nearestColumn = "";
                DateTime nearestTime = DateTime.MinValue;
                double nearestValue = 0;
                double nearestPlotX = 0;
                double nearestPlotY = 0;
                Color nearestColor = Color.Black;

                var limits = formsPlot.Plot.GetAxisLimits();
                double xSpan = limits.XMax - limits.XMin;
                double ySpan = limits.YMax - limits.YMin;

                // 모든 파일의 데이터를 직접 검색
                foreach (var file in csvFiles)
                {
                    var fileInfo = file.Value;
                    if (fileInfo.Timestamps.Count == 0) continue;

                    // 마우스 X에 가장 가까운 시간 인덱스 찾기
                    int closestTimeIdx = -1;
                    double minTimeDiff = double.MaxValue;

                    for (int i = 0; i < fileInfo.Timestamps.Count; i++)
                    {
                        double timeOA = fileInfo.Timestamps[i].ToOADate();
                        double diff = Math.Abs(timeOA - mouseX);
                        if (diff < minTimeDiff)
                        {
                            minTimeDiff = diff;
                            closestTimeIdx = i;
                        }
                    }

                    if (closestTimeIdx == -1) continue;

                    // 해당 시간의 모든 컬럼 값 확인
                    foreach (var column in fileInfo.DataColumns)
                    {
                        if (!fileInfo.SelectedColumns.Contains(column.Key)) continue;
                        if (closestTimeIdx >= column.Value.Count) continue;

                        double value = column.Value[closestTimeIdx];
                        if (double.IsNaN(value) || double.IsInfinity(value)) continue;

                        double timeOA = fileInfo.Timestamps[closestTimeIdx].ToOADate();

                        // 화면 범위 체크
                        if (timeOA < limits.XMin || timeOA > limits.XMax) continue;
                        if (value < limits.YMin || value > limits.YMax) continue;

                        // 정규화된 거리 계산
                        double xDist = (timeOA - mouseX) / xSpan;
                        double yDist = (value - mouseY) / ySpan;
                        double distance = Math.Sqrt(xDist * xDist + yDist * yDist);

                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestFile = fileInfo.FileName;
                            nearestColumn = column.Key;
                            nearestTime = fileInfo.Timestamps[closestTimeIdx];
                            nearestValue = value;
                            nearestPlotX = timeOA;
                            nearestPlotY = value;
                            pointFound = true;

                            // 해당 플롯의 색상 찾기
                            string label = $"{Path.GetFileNameWithoutExtension(fileInfo.FileName)}: {column.Key}";
                            foreach (var plot in allPlots)
                            {
                                if (plot.Label == label)
                                {
                                    nearestColor = plot.Color;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (nearestDistance > 0.05)
                {
                    pointFound = false;
                }

                if (pointFound)
                {
                    // 크로스헤어 업데이트
                    crossHair.X = nearestPlotX;
                    crossHair.Y = nearestPlotY;
                    crossHair.IsVisible = true;

                    // 하이라이트 마커 업데이트
                    highlightMarker.X = nearestPlotX;
                    highlightMarker.Y = nearestPlotY;
                    highlightMarker.MarkerColor = nearestColor;
                    highlightMarker.IsVisible = true;

                    lblCrosshair.Text = $"파일: {Path.GetFileNameWithoutExtension(nearestFile)}\n" +
                                       $"컬럼: {nearestColumn}\n" +
                                       $"시간: {nearestTime:yyyy-MM-dd HH:mm:ss}\n" +
                                       $"값: {nearestValue:G6}";
                    lblCrosshair.Visible = true;

                    // 하이라이트 텍스트
                    highlightText.Label = $"{nearestValue:G4}";
                    highlightText.X = nearestPlotX;
                    highlightText.Y = nearestPlotY;
                    highlightText.Alignment = Alignment.LowerLeft;
                    highlightText.Color = nearestColor;
                    highlightText.IsVisible = true;
                }
                else
                {
                    crossHair.IsVisible = false;
                    highlightMarker.IsVisible = false;
                    highlightText.IsVisible = false;
                    lblCrosshair.Visible = false;
                }

                formsPlot.Refresh();
            }
        }

        private void FormsPlot_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isDragging)
            {
                isDragging = false;

                var coords1 = formsPlot.GetMouseCoordinates(dragStart.X, dragStart.Y);
                var coords2 = formsPlot.GetMouseCoordinates(e.X, e.Y);

                double x1 = coords1.x;
                double y1 = coords1.y;
                double x2 = coords2.x;
                double y2 = coords2.y;

                double xMin = Math.Min(x1, x2);
                double xMax = Math.Max(x1, x2);
                double yMin = Math.Min(y1, y2);
                double yMax = Math.Max(y1, y2);

                // 드래그 영역이 너무 작으면 무시
                if (Math.Abs(xMax - xMin) < 0.01 || Math.Abs(yMax - yMin) < 0.01)
                {
                    formsPlot.Refresh();
                    return;
                }

                // Shift 키를 누른 상태면 선택 영역으로 줌
                if (ModifierKeys.HasFlag(Keys.Shift))
                {
                    // 선택 영역으로 즉시 줌
                    formsPlot.Plot.SetAxisLimits(xMin, xMax, yMin, yMax);
                    chkAutoScale.Checked = false; // 자동 스케일 해제
                    formsPlot.Refresh();
                }
                else
                {
                    // 일반 선택 - 영역 내 데이터 분석
                    SelectPointsInRegion(xMin, xMax, yMin, yMax);

                    if (selectedPoints.Count > 0)
                    {
                        selectionSpan = formsPlot.Plot.AddVerticalSpan(xMin, xMax);
                        selectionSpan.Color = Color.FromArgb(50, Color.Blue);
                        ShowSelectedPointsInfo();
                    }
                }

                formsPlot.Refresh();
            }
            else if (e.Button == MouseButtons.Right)
            {
                ShowPlotContextMenu(e.Location);
            }
        }

        private void FormsPlot_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                formsPlot.Plot.AxisAuto();
                formsPlot.Refresh();
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // ESC 키로 선택 영역 제거
            if (e.KeyCode == Keys.Escape)
            {
                if (selectionSpan != null)
                {
                    formsPlot.Plot.Remove(selectionSpan);
                    selectionSpan = null;
                    selectedPoints.Clear();
                    formsPlot.Refresh();
                }
            }
            // R 키로 전체 보기
            else if (e.KeyCode == Keys.R && !e.Control && !e.Shift)
            {
                chkAutoScale.Checked = true;
                UpdatePlot();
            }
        }

        private int FindClosestXIndex(double[] xs, double targetX)
        {
            int left = 0;
            int right = xs.Length - 1;

            while (left <= right)
            {
                int mid = (left + right) / 2;

                if (xs[mid] == targetX)
                    return mid;

                if (xs[mid] < targetX)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            if (right < 0) return 0;
            if (left >= xs.Length) return xs.Length - 1;

            return (targetX - xs[right] < xs[left] - targetX) ? right : left;
        }

        // SelectPointsInRegion 메서드 수정
        private void SelectPointsInRegion(double xMin, double xMax, double yMin, double yMax)
        {
            selectedPoints.Clear();

            foreach (var file in csvFiles)
            {
                var fileInfo = file.Value;
                if (fileInfo.Timestamps.Count == 0) continue;

                for (int i = 0; i < fileInfo.Timestamps.Count; i++)
                {
                    double x = fileInfo.Timestamps[i].ToOADate();

                    if (x >= xMin && x <= xMax)
                    {
                        foreach (var column in fileInfo.DataColumns)
                        {
                            if (i < column.Value.Count && !double.IsNaN(column.Value[i]))
                            {
                                double y = column.Value[i];

                                if (y >= yMin && y <= yMax)
                                {
                                    // 수정된 부분: 클래스 사용
                                    selectedPoints.Add(new SelectedPointData
                                    {
                                        X = x,
                                        Y = y,
                                        Label = $"{fileInfo.FileName}: {column.Key}",
                                        File = fileInfo.FileName,
                                        Column = column.Key
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ShowSelectedPointsInfo()
        {
            if (selectedPoints.Count == 0) return;

            var info = new System.Text.StringBuilder();
            info.AppendLine($"선택된 데이터 포인트: {selectedPoints.Count}개");
            info.AppendLine();

            // 수정된 부분: 프로퍼티 사용
            var grouped = selectedPoints.GroupBy(p => p.File)
                                       .ToDictionary(g => g.Key,
                                                    g => g.GroupBy(p => p.Column)
                                                          .ToDictionary(c => c.Key, c => c.ToList()));

            foreach (var file in grouped)
            {
                info.AppendLine($"파일: {file.Key}");
                foreach (var column in file.Value)
                {
                    var values = column.Value.Select(p => p.Y).ToList();  // 수정됨
                    info.AppendLine($"  {column.Key}:");
                    info.AppendLine($"    개수: {values.Count}");
                    info.AppendLine($"    최소: {values.Min():G6}");
                    info.AppendLine($"    최대: {values.Max():G6}");
                    info.AppendLine($"    평균: {values.Average():G6}");
                    info.AppendLine($"    표준편차: {CalculateStdDev(values):G6}");
                }
            }

            using (var form = new Form())
            {
                form.Text = "선택된 데이터 분석";
                form.Size = new Size(500, 600);
                form.StartPosition = FormStartPosition.CenterParent;

                var textBox = new TextBox
                {
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 10),
                    Text = info.ToString()
                };

                var btnExport = new Button
                {
                    Text = "CSV로 내보내기",
                    Dock = DockStyle.Bottom,
                    Height = 30
                };
                btnExport.Click += (s, e) => ExportSelectedPoints();

                form.Controls.Add(textBox);
                form.Controls.Add(btnExport);
                form.ShowDialog();
            }
        }

        private double CalculateStdDev(List<double> values)
        {
            if (values.Count <= 1) return 0;
            double avg = values.Average();
            double sum = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sum / (values.Count - 1));
        }

        private void ExportSelectedPoints()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV 파일|*.csv|모든 파일|*.*";
                sfd.Title = "선택된 데이터 내보내기";
                sfd.FileName = $"SelectedData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var lines = new List<string>();
                    lines.Add("Timestamp,File,Column,Value");

                    // 수정된 부분: 프로퍼티 사용
                    foreach (var point in selectedPoints.OrderBy(p => p.X))
                    {
                        DateTime time = DateTime.FromOADate(point.X);
                        lines.Add($"{time:yyyy-MM-dd HH:mm:ss},{point.File},{point.Column},{point.Y:G6}");
                    }

                    File.WriteAllLines(sfd.FileName, lines);
                    MessageBox.Show("데이터가 저장되었습니다.", "완료",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ShowPlotContextMenu(Point location)
        {
            var cm = new ContextMenuStrip();

            // 선택 영역이 있으면 관련 메뉴 추가
            if (selectedPoints.Count > 0 && selectionSpan != null)
            {
                var zoomToSelection = new ToolStripMenuItem("선택 영역으로 확대");
                zoomToSelection.Click += (s, ev) =>
                {
                    formsPlot.Plot.SetAxisLimits(selectionSpan.DragLimitMin, selectionSpan.DragLimitMax);
                    chkAutoScale.Checked = false;
                    formsPlot.Refresh();
                };
                cm.Items.Add(zoomToSelection);

                var clearSelection = new ToolStripMenuItem("선택 영역 제거");
                clearSelection.Click += (s, ev) =>
                {
                    formsPlot.Plot.Remove(selectionSpan);
                    selectionSpan = null;
                    selectedPoints.Clear();
                    formsPlot.Refresh();
                };
                cm.Items.Add(clearSelection);

                var showOnlySelection = new ToolStripMenuItem("선택 영역만 표시");
                showOnlySelection.Click += (s, ev) =>
                {
                    ShowOnlySelectedTimeRange(selectionSpan.DragLimitMin, selectionSpan.DragLimitMax);
                };
                cm.Items.Add(showOnlySelection);

                cm.Items.Add(new ToolStripSeparator());
            }

            var autoAxis = new ToolStripMenuItem("자동 축 조정");
            autoAxis.Click += (s, ev) => { formsPlot.Plot.AxisAuto(); formsPlot.Refresh(); };
            cm.Items.Add(autoAxis);

            var resetZoom = new ToolStripMenuItem("전체 데이터 보기");
            resetZoom.Click += (s, ev) =>
            {
                chkAutoScale.Checked = true;
                UpdatePlot();
            };
            cm.Items.Add(resetZoom);

            var logScale = new ToolStripMenuItem("Y축 로그 스케일");
            logScale.Checked = isLogScale;
            logScale.Click += (s, ev) => { ToggleLogScale(); };
            cm.Items.Add(logScale);

            cm.Items.Add(new ToolStripSeparator());

            var saveImage = new ToolStripMenuItem("이미지로 저장...");
            saveImage.Click += (s, ev) => SavePlotImage();
            cm.Items.Add(saveImage);

            var exportData = new ToolStripMenuItem("데이터 내보내기...");
            exportData.Click += (s, ev) => ExportPlotData();
            cm.Items.Add(exportData);

            cm.Items.Add(new ToolStripSeparator());

            var gridToggle = new ToolStripMenuItem("그리드 표시");
            gridToggle.Checked = true;
            gridToggle.Click += (s, ev) =>
            {
                formsPlot.Plot.Grid(!gridToggle.Checked);
                formsPlot.Refresh();
            };
            cm.Items.Add(gridToggle);

            var legendToggle = new ToolStripMenuItem("범례 표시");
            legendToggle.Checked = isLegendVisible;
            legendToggle.Click += (s, ev) =>
            {
                isLegendVisible = !isLegendVisible;
                UpdatePlot();
            };
            cm.Items.Add(legendToggle);

            cm.Show(formsPlot, location);
        }

        private void ShowOnlySelectedTimeRange(double xMin, double xMax)
        {
            DateTime startTime = DateTime.FromOADate(xMin);
            DateTime endTime = DateTime.FromOADate(xMax);

            formsPlot.Plot.SetAxisLimits(xMin, xMax);
            chkAutoScale.Checked = false;

            if (selectionSpan != null)
            {
                formsPlot.Plot.Remove(selectionSpan);
                selectionSpan = null;
            }

            formsPlot.Refresh();

            MessageBox.Show($"선택한 시간 범위로 확대되었습니다.\n" +
                            $"시작: {startTime:yyyy-MM-dd HH:mm:ss}\n" +
                            $"종료: {endTime:yyyy-MM-dd HH:mm:ss}",
                            "시간 범위 선택", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

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
                sfd.FileName = $"그래프_{DateTime.Now:yyyyMMdd_HHmmss}.png";

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

            string directory = Path.GetDirectoryName(filePath);
            fileInfo.Watcher = new FileSystemWatcher(directory)
            {
                Filter = fileInfo.FileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            fileInfo.Watcher.Changed += (s, e) => OnFileChanged(filePath);
            fileInfo.Watcher.EnableRaisingEvents = true;

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

                for (int i = 0; i < file.Headers.Count; i++)
                {
                    var columnNode = new TreeNode(file.Headers[i])
                    {
                        Tag = i,
                        Checked = file.SelectedColumns.Contains(file.Headers[i])
                    };

                    if (file.SelectedColumns.Count == 0 &&
                        (file.Headers[i].Contains("Pressure", StringComparison.OrdinalIgnoreCase) ||
                         file.Headers[i].Contains("Ion", StringComparison.OrdinalIgnoreCase) ||
                         file.Headers[i].Contains("Pirani", StringComparison.OrdinalIgnoreCase) ||
                         file.Headers[i].Contains("ATM", StringComparison.OrdinalIgnoreCase)))
                    {
                        columnNode.Checked = true;
                        file.SelectedColumns.Add(file.Headers[i]);
                    }

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
            if (e.Node.Level == 0)
            {
                foreach (TreeNode childNode in e.Node.Nodes)
                {
                    if (childNode.Tag.ToString() != "TIME_COLUMN")
                    {
                        childNode.Checked = e.Node.Checked;
                    }
                }
            }
            else if (e.Node.Level == 1)
            {
                string filePath = e.Node.Parent.Tag.ToString();
                if (csvFiles.ContainsKey(filePath))
                {
                    var fileInfo = csvFiles[filePath];

                    if (e.Node.Tag.ToString() == "TIME_COLUMN")
                    {
                        e.Node.Checked = false;
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
                    UpdateGraph(null);
                }
            }
        }

        private void LstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
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
            if (!isMonitoringEnabled && state != null) return;

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

                            DateTime timestamp;
                            if (fileInfo.TimeColumnIndex == -1)
                            {
                                var fileTime = File.GetLastWriteTime(fileInfo.FilePath);
                                timestamp = fileTime.AddSeconds(rowNumber);
                            }
                            else
                            {
                                if (fileInfo.TimeColumnIndex < values.Length)
                                {
                                    string timeStr = values[fileInfo.TimeColumnIndex].Trim();
                                    if (!DateTime.TryParse(timeStr, out timestamp))
                                    {
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

                            fileInfo.Timestamps.Add(timestamp);

                            foreach (string colName in fileInfo.SelectedColumns)
                            {
                                int colIndex = fileInfo.Headers.IndexOf(colName);
                                double value = double.NaN;

                                if (colIndex >= 0 && colIndex < values.Length)
                                {
                                    string valueStr = values[colIndex].Trim();

                                    if (!TryParseValue(valueStr, out value))
                                    {
                                        value = double.NaN;
                                    }

                                    if (!double.IsNaN(value) && fileInfo.Filters.ContainsKey(colName))
                                    {
                                        var filter = fileInfo.Filters[colName];
                                        if (!filter.PassesFilter(value))
                                        {
                                            value = double.NaN;
                                        }
                                    }
                                }

                                fileInfo.DataColumns[colName].Add(value);
                            }

                            rowNumber++;
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

        private bool TryParseValue(string valueStr, out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(valueStr))
                return false;

            valueStr = valueStr.Trim().Replace("\"", "");

            if (valueStr.Equals("NaN", StringComparison.OrdinalIgnoreCase) ||
                valueStr.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
                valueStr.Equals("-", StringComparison.OrdinalIgnoreCase))
            {
                value = double.NaN;
                return true;
            }

            if (valueStr.Equals("Inf", StringComparison.OrdinalIgnoreCase) ||
                valueStr.Equals("+Inf", StringComparison.OrdinalIgnoreCase))
            {
                value = double.PositiveInfinity;
                return true;
            }

            if (valueStr.Equals("-Inf", StringComparison.OrdinalIgnoreCase))
            {
                value = double.NegativeInfinity;
                return true;
            }

            valueStr = valueStr.Replace("E", "e").Replace("D", "e").Replace("d", "e");

            return double.TryParse(valueStr,
                NumberStyles.Float | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value);
        }

        private void UpdatePlot()
        {
            lock (dataLock)
            {
                var existingLimits = formsPlot.Plot.GetAxisLimits();
                bool hadValidLimits = !double.IsNaN(existingLimits.XMin) && !double.IsNaN(existingLimits.YMin);

                formsPlot.Plot.Clear();
                allPlots.Clear();

                crossHair = formsPlot.Plot.AddCrosshair(crossHair.X, crossHair.Y);
                crossHair.IsVisible = false;

                highlightMarker = formsPlot.Plot.AddMarker(0, 0);
                highlightMarker.MarkerShape = MarkerShape.openCircle;
                highlightMarker.MarkerSize = 15;
                highlightMarker.MarkerLineWidth = 2;
                highlightMarker.IsVisible = false;

                highlightText = formsPlot.Plot.AddText("", 0, 0);
                highlightText.FontBold = true;
                highlightText.BackgroundFill = true;
                highlightText.BackgroundColor = Color.FromArgb(200, Color.White);
                highlightText.IsVisible = false;

                var colors = new[] {
                    Color.Blue, Color.Red, Color.Green, Color.Orange,
                    Color.Purple, Color.Brown, Color.Pink, Color.Gray,
                    Color.Cyan, Color.Magenta, Color.DarkBlue, Color.DarkRed,
                    Color.DarkGreen, Color.DarkOrange, Color.Indigo, Color.Olive
                };

                int colorIndex = 0;
                int totalPoints = 0;
                int totalColumns = 0;

                // 단순화된 컬럼 그룹 클래스
                var columnGroups = new Dictionary<string, List<Tuple<string, string, List<double>, List<DateTime>>>>();

                foreach (var file in csvFiles)
                {
                    var fileInfo = file.Value;
                    if (fileInfo.Timestamps.Count == 0) continue;

                    foreach (var column in fileInfo.DataColumns)
                    {
                        if (column.Value.Count > 0)
                        {
                            string unit = GuessUnit(column.Key);

                            if (!columnGroups.ContainsKey(unit))
                                columnGroups[unit] = new List<Tuple<string, string, List<double>, List<DateTime>>>();

                            columnGroups[unit].Add(Tuple.Create(fileInfo.FileName, column.Key, column.Value, fileInfo.Timestamps));
                        }
                    }
                }

                int yAxisIndex = 0;
                foreach (var unitGroup in columnGroups)
                {
                    foreach (var item in unitGroup.Value)
                    {
                        List<List<double>> xSegments = new List<List<double>>();
                        List<List<double>> ySegments = new List<List<double>>();
                        List<double> currentXs = new List<double>();
                        List<double> currentYs = new List<double>();

                        for (int i = 0; i < item.Item4.Count && i < item.Item3.Count; i++)
                        {
                            if (!double.IsNaN(item.Item3[i]) && !double.IsInfinity(item.Item3[i]))
                            {
                                currentXs.Add(item.Item4[i].ToOADate());
                                currentYs.Add(item.Item3[i]);
                            }
                            else if (currentXs.Count > 0)
                            {
                                xSegments.Add(new List<double>(currentXs));
                                ySegments.Add(new List<double>(currentYs));
                                currentXs.Clear();
                                currentYs.Clear();
                            }
                        }

                        if (currentXs.Count > 0)
                        {
                            xSegments.Add(currentXs);
                            ySegments.Add(currentYs);
                        }

                        string shortFileName = Path.GetFileNameWithoutExtension(item.Item1);
                        string label = $"{shortFileName}: {item.Item2}";

                        bool isFirstSegment = true;
                        for (int segIdx = 0; segIdx < xSegments.Count; segIdx++)
                        {
                            if (xSegments[segIdx].Count > 0)
                            {
                                var signal = formsPlot.Plot.AddScatterLines(
                                    xSegments[segIdx].ToArray(),
                                    ySegments[segIdx].ToArray());

                                signal.Color = colors[colorIndex % colors.Length];
                                signal.LineWidth = 2;
                                signal.Label = isFirstSegment ? label : "";
                                signal.MarkerSize = 0;

                                if (yAxisIndex > 0 && yAxisIndex < 2)
                                {
                                    signal.YAxisIndex = yAxisIndex;
                                }

                                allPlots.Add(signal);
                                isFirstSegment = false;
                            }
                        }

                        colorIndex++;
                        totalColumns++;
                        totalPoints += item.Item3.Count(v => !double.IsNaN(v));
                    }

                    if (yAxisIndex == 0)
                    {
                        formsPlot.Plot.YAxis.Label(unitGroup.Key);
                        formsPlot.Plot.YAxis.Color(yAxisColors[0]);
                    }
                    else if (yAxisIndex == 1)
                    {
                        formsPlot.Plot.YAxis2.IsVisible = true;
                        formsPlot.Plot.YAxis2.Label(unitGroup.Key);
                        formsPlot.Plot.YAxis2.Color(yAxisColors[1]);
                    }

                    yAxisIndex++;
                    if (yAxisIndex >= 2) break;
                }

                formsPlot.Plot.XAxis.DateTimeFormat(true);

                if (!chkAutoScale.Checked && hadValidLimits)
                {
                    formsPlot.Plot.SetAxisLimits(existingLimits);
                }
                else if (chkSyncTimeAxis.Checked)
                {
                    DateTime? minTime = null;
                    DateTime? maxTime = null;

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

                    if (minTime.HasValue && maxTime.HasValue)
                    {
                        formsPlot.Plot.SetAxisLimits(
                            xMin: minTime.Value.ToOADate(),
                            xMax: maxTime.Value.ToOADate()
                        );
                    }
                }
                else
                {
                    formsPlot.Plot.AxisAuto();
                }

                if (totalColumns > 0 && isLegendVisible)
                {
                    var legend = formsPlot.Plot.Legend(true);
                    legend.Location = Alignment.UpperRight;
                    legend.FontSize = 10;
                }

                formsPlot.Refresh();

                string monitoringStatus = isMonitoringEnabled ? "모니터링 중" : "모니터링 중지";
                lblStatus.Text = $"{monitoringStatus}... (파일: {csvFiles.Count}개, " +
                               $"유효 데이터: {totalPoints}개, " +
                               $"표시 컬럼: {totalColumns}개, " +
                               $"Y축 그룹: {Math.Min(columnGroups.Count, 2)}개, " +
                               $"마지막 업데이트: {DateTime.Now:HH:mm:ss})";
                lblStatus.ForeColor = isMonitoringEnabled ? Color.Green : Color.Orange;
            }
        }

        private string GuessUnit(string columnName)
        {
            columnName = columnName.ToLower();

            if (columnName.Contains("pressure") || columnName.Contains("torr"))
                return "압력 (Torr)";
            else if (columnName.Contains("temperature") || columnName.Contains("temp"))
                return "온도 (°C)";
            else if (columnName.Contains("flow"))
                return "유량 (sccm)";
            else if (columnName.Contains("voltage") || columnName.Contains("volt"))
                return "전압 (V)";
            else if (columnName.Contains("current") || columnName.Contains("amp"))
                return "전류 (A)";
            else if (columnName.Contains("power"))
                return "전력 (W)";
            else
                return "값";
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

            var results = new System.Text.StringBuilder();
            results.AppendLine("=== 리크 테스트 결과 ===\n");

            foreach (var file in csvFiles)
            {
                results.AppendLine($"파일: {file.Value.FileName}");

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