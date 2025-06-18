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
        private Crosshair crossHair;
        private MarkerPlot highlightMarker;
        private Text highlightText;
        private List<SignalPlotXY> allPlots = new List<SignalPlotXY>();

        // 드래그 선택 관련
        private bool isDragging = false;
        private Point dragStart;
        private Rectangle dragRect;
        private List<SelectedPointData> selectedPoints;
        private VSpan selectionSpan;
        private double selectionXMin;
        private double selectionXMax;

        // 다중 Y축 관련
        private Dictionary<string, int> columnToYAxisIndex;
        private List<Color> yAxisColors;

        // 기타 플래그
        private bool isLogScale = false;
        private bool isLegendVisible = true;
        private bool isBottomPanelVisible = true;
        private bool isTimeRangeEnabled = false;
        private DateTime? customStartTime = null;
        private DateTime? customEndTime = null;

        // 오른쪽 정보 패널
        private Panel pnlRightInfo;
        private RichTextBox rtbDataInfo;
        private DataGridView dgvCurrentValues;
        private Label lblInfoTitle;

        // 데이터 타입 감지
        private Dictionary<string, bool> columnIsNumeric;

        // 줌/팬 상태 추적
        private bool isUserZooming = false;
        private AxisLimits lastAxisLimits;
        private bool isProgrammaticChange = false;



        public MainForm()
        {
            try
            {
                InitializeComponent();
                InitializeRightPanel();
                csvFiles = new Dictionary<string, CsvFileInfo>();
                selectedPoints = new List<SelectedPointData>();
                columnToYAxisIndex = new Dictionary<string, int>();
                yAxisColors = new List<Color> { Color.Black, Color.Blue, Color.Red, Color.Green, Color.Purple };
                columnIsNumeric = new Dictionary<string, bool>();
                selectionXMin = 0;
                selectionXMax = 0;
                isBottomPanelVisible = true;
                SetupSplitContainers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"폼 초기화 오류:\n{ex.Message}\n\n{ex.StackTrace}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeRightPanel()
        {
            // 오른쪽 정보 패널 생성
            pnlRightInfo = new Panel
            {
                Dock = DockStyle.Right,
                Width = 400,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.Control
            };

            // 제목 라벨
            lblInfoTitle = new Label
            {
                Text = "실시간 데이터 정보",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("맑은 고딕", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White
            };

            // 현재 값 표시 DataGridView
            dgvCurrentValues = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 300,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // 컬럼 설정
            dgvCurrentValues.Columns.Add("File", "파일");
            dgvCurrentValues.Columns.Add("Column", "컬럼");
            dgvCurrentValues.Columns.Add("Value", "현재값");
            dgvCurrentValues.Columns.Add("Unit", "단위");
            dgvCurrentValues.Columns.Add("Status", "상태");

            dgvCurrentValues.Columns["File"].Width = 100;
            dgvCurrentValues.Columns["Column"].Width = 100;
            dgvCurrentValues.Columns["Value"].Width = 80;
            dgvCurrentValues.Columns["Unit"].Width = 60;
            dgvCurrentValues.Columns["Status"].Width = 60;

            // 데이터 정보 텍스트박스
            rtbDataInfo = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.None
            };

            // 스플리터
            var splitter = new Splitter
            {
                Dock = DockStyle.Top,
                Height = 3,
                BackColor = SystemColors.ControlDark
            };

            // 패널에 컨트롤 추가
            pnlRightInfo.Controls.Add(rtbDataInfo);
            pnlRightInfo.Controls.Add(splitter);
            pnlRightInfo.Controls.Add(dgvCurrentValues);
            pnlRightInfo.Controls.Add(lblInfoTitle);

            // 메인 폼에 추가
            this.Controls.Add(pnlRightInfo);
            pnlRightInfo.BringToFront();
        }

        private void SetupSplitContainers()
        {
            // 하단 패널 초기 설정
            pnlBottom.Height = 260;
            isBottomPanelVisible = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializePlot();
            StartUpdateTimer();
            InitializeAnalysisMenu();

            // 초기값 설정
            if (dtpStartTime != null && dtpEndTime != null)
            {
                dtpStartTime.Value = DateTime.Now.AddHours(-1);
                dtpEndTime.Value = DateTime.Now;
            }
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
            formsPlot.Plot.Title("압력 데이터 실시간 모니터링");
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
            formsPlot.AxesChanged += FormsPlot_AxesChanged;
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

            formsPlot.Render();
        }

        private void FormsPlot_AxesChanged(object sender, EventArgs e)
        {
            // 프로그램에 의한 변경이면 무시
            if (isProgrammaticChange) return;

            // 시간 범위가 활성화되어 있으면 무시
            if (isTimeRangeEnabled) return;

            // 사용자가 줌/팬을 했을 때
            isUserZooming = true;

            // 자동 스케일 체크박스 해제
            if (chkAutoScale.Checked)
            {
                chkAutoScale.Checked = false;
            }

            // Y축 자동 조정
            AutoScaleYAxis();

            // 현재 축 상태 저장
            lastAxisLimits = formsPlot.Plot.GetAxisLimits();
        }

        private void AutoScaleYAxis()
        {
            var limits = formsPlot.Plot.GetAxisLimits();
            double xMin = limits.XMin;
            double xMax = limits.XMax;

            double yMin = double.MaxValue;
            double yMax = double.MinValue;
            bool hasVisibleData = false;

            // 모든 플롯의 현재 보이는 영역 데이터 확인
            foreach (var plot in allPlots)
            {
                if (plot.Xs == null || plot.Xs.Length == 0) continue;

                for (int i = 0; i < plot.Xs.Length; i++)
                {
                    if (plot.Xs[i] >= xMin && plot.Xs[i] <= xMax)
                    {
                        if (!double.IsNaN(plot.Ys[i]) && !double.IsInfinity(plot.Ys[i]))
                        {
                            yMin = Math.Min(yMin, plot.Ys[i]);
                            yMax = Math.Max(yMax, plot.Ys[i]);
                            hasVisibleData = true;
                        }
                    }
                }
            }

            if (hasVisibleData)
            {
                // Y축 여백 추가 (10%)
                double yPadding = (yMax - yMin) * 0.1;
                if (yPadding == 0) yPadding = Math.Abs(yMax) * 0.1;
                if (yPadding == 0) yPadding = 1;

                isProgrammaticChange = true;
                try
                {
                    formsPlot.Plot.SetAxisLimits(
                        xMin: xMin,
                        xMax: xMax,
                        yMin: yMin - yPadding,
                        yMax: yMax + yPadding
                    );
                }
                finally
                {
                    isProgrammaticChange = false;
                }
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

                formsPlot.Render();
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
                UpdateHoverInfo(e);
            }
        }

        private void UpdateHoverInfo(MouseEventArgs e)
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

                // 하이라이트 텍스트
                highlightText.Label = $"{nearestValue:G4}";
                highlightText.X = nearestPlotX;
                highlightText.Y = nearestPlotY;
                highlightText.Alignment = Alignment.LowerLeft;
                highlightText.Color = nearestColor;
                highlightText.IsVisible = true;

                // 정보 패널 업데이트
                UpdateInfoPanel(nearestFile, nearestColumn, nearestTime, nearestValue);
            }
            else
            {
                crossHair.IsVisible = false;
                highlightMarker.IsVisible = false;
                highlightText.IsVisible = false;
            }

            formsPlot.Render();
        }

        private void UpdateInfoPanel(string file, string column, DateTime time, double value)
        {
            string unit = GuessUnit(column);
            string info = $"파일: {Path.GetFileNameWithoutExtension(file)}\n" +
                         $"컬럼: {column}\n" +
                         $"시간: {time:yyyy-MM-dd HH:mm:ss}\n" +
                         $"값: {FormatValue(value, unit)}";

            if (column.Contains("Pressure", StringComparison.OrdinalIgnoreCase) ||
                column.Contains("Torr", StringComparison.OrdinalIgnoreCase))
            {
                var vacuumLevel = PressureAnalysisTools.GetVacuumLevel(value);
                info += $"\n진공 레벨: {vacuumLevel}";
            }

            // 시간 범위 정보 추가
            if (isTimeRangeEnabled && customStartTime.HasValue && customEndTime.HasValue)
            {
                info += $"\n\n[시간 범위 설정됨]\n";
                info += $"시작: {customStartTime.Value:yyyy-MM-dd HH:mm:ss}\n";
                info += $"종료: {customEndTime.Value:yyyy-MM-dd HH:mm:ss}";
            }

            if (rtbDataInfo.InvokeRequired)
            {
                rtbDataInfo.BeginInvoke((MethodInvoker)delegate { rtbDataInfo.Text = info; });
            }
            else
            {
                rtbDataInfo.Text = info;
            }
        }

        private string FormatValue(double value, string unit)
        {
            if (unit.Contains("Torr") && value < 1e-3)
                return $"{value:E2} {unit}";
            else if (Math.Abs(value) < 0.01 || Math.Abs(value) > 10000)
                return $"{value:E2} {unit}";
            else
                return $"{value:F2} {unit}";
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
                    selectionXMin = 0;
                    selectionXMax = 0;
                }
                selectedPoints.Clear();
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
                    formsPlot.Render();
                    return;
                }

                // Shift 키를 누른 상태면 선택 영역으로 줌
                if (ModifierKeys.HasFlag(Keys.Shift))
                {
                    isProgrammaticChange = true;
                    try
                    {
                        // 선택 영역으로 즉시 줌
                        formsPlot.Plot.SetAxisLimits(xMin: xMin, xMax: xMax, yMin: yMin, yMax: yMax);
                        chkAutoScale.Checked = false; // 자동 스케일 해제
                        isUserZooming = true;
                    }
                    finally
                    {
                        isProgrammaticChange = false;
                    }
                    formsPlot.Render();
                }
                else
                {
                    // 일반 선택 - 영역 내 데이터 분석
                    SelectPointsInRegion(xMin, xMax, yMin, yMax);

                    if (selectedPoints.Count > 0)
                    {
                        selectionSpan = formsPlot.Plot.AddVerticalSpan(xMin, xMax);
                        selectionSpan.Color = Color.FromArgb(50, Color.Blue);
                        selectionXMin = xMin;
                        selectionXMax = xMax;
                        ShowSelectedPointsInfo();
                    }
                }

                formsPlot.Render();
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
                isProgrammaticChange = true;
                try
                {
                    formsPlot.Plot.AxisAuto();
                    isUserZooming = false;
                    chkAutoScale.Checked = true; // 자동 스케일 다시 켜기
                }
                finally
                {
                    isProgrammaticChange = false;
                }
                formsPlot.Render();
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
                    selectionXMin = 0;
                    selectionXMax = 0;
                    formsPlot.Render();
                }
            }
            // R 키로 전체 보기
            else if (e.KeyCode == Keys.R && !e.Control && !e.Shift)
            {
                chkAutoScale.Checked = true;
                isUserZooming = false;
                UpdatePlot();
            }
            // T 키로 실시간 추적 토글
            else if (e.KeyCode == Keys.T && !e.Control && !e.Shift)
            {
                if (isMonitoringEnabled)
                {
                    chkAutoScale.Checked = !chkAutoScale.Checked;
                }
            }
            // Space 키로 일시정지/재개
            else if (e.KeyCode == Keys.Space && !e.Control && !e.Shift)
            {
                BtnMonitoring_Click(null, null);
            }
            // D 키로 시간 범위 토글
            else if (e.KeyCode == Keys.D && !e.Control && !e.Shift)
            {
                if (chkEnableTimeRange != null)
                {
                    chkEnableTimeRange.Checked = !chkEnableTimeRange.Checked;
                }
            }
            // Ctrl+T로 시간 범위 설정 창으로 포커스
            else if (e.KeyCode == Keys.T && e.Control && !e.Shift)
            {
                if (chkEnableTimeRange != null && !chkEnableTimeRange.Checked)
                {
                    chkEnableTimeRange.Checked = true;
                }
                if (dtpStartTime != null)
                {
                    dtpStartTime.Focus();
                }
            }
        }

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

            var grouped = selectedPoints.GroupBy(p => p.File)
                                       .ToDictionary(g => g.Key,
                                                    g => g.GroupBy(p => p.Column)
                                                          .ToDictionary(c => c.Key, c => c.ToList()));

            foreach (var file in grouped)
            {
                info.AppendLine($"파일: {file.Key}");
                foreach (var column in file.Value)
                {
                    var values = column.Value.Select(p => p.Y).ToList();
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
                    // X축을 선택 영역으로 설정
                    var currentLimits = formsPlot.Plot.GetAxisLimits();
                    formsPlot.Plot.SetAxisLimits(
                        xMin: selectionXMin,
                        xMax: selectionXMax,
                        yMin: currentLimits.YMin,
                        yMax: currentLimits.YMax
                    );

                    // Y축 자동 조정
                    AutoScaleYAxis();

                    chkAutoScale.Checked = false;
                    isUserZooming = true;
                    formsPlot.Render();
                };
                cm.Items.Add(zoomToSelection);

                var clearSelection = new ToolStripMenuItem("선택 영역 제거");
                clearSelection.Click += (s, ev) =>
                {
                    formsPlot.Plot.Remove(selectionSpan);
                    selectionSpan = null;
                    selectedPoints.Clear();
                    selectionXMin = 0;
                    selectionXMax = 0;
                    formsPlot.Render();
                };
                cm.Items.Add(clearSelection);

                cm.Items.Add(new ToolStripSeparator());
            }

            var resumeTracking = new ToolStripMenuItem("실시간 추적 재개");
            resumeTracking.Click += (s, ev) =>
            {
                chkAutoScale.Checked = true;
                isUserZooming = false;
                UpdatePlot();
            };
            resumeTracking.Enabled = isMonitoringEnabled && (!chkAutoScale.Checked || isUserZooming);
            cm.Items.Add(resumeTracking);

            var autoAxis = new ToolStripMenuItem("자동 축 조정");
            autoAxis.Click += (s, ev) =>
            {
                isProgrammaticChange = true;
                try
                {
                    formsPlot.Plot.AxisAuto();
                    isUserZooming = false;
                }
                finally
                {
                    isProgrammaticChange = false;
                }
                formsPlot.Render();
            };
            cm.Items.Add(autoAxis);

            var resetZoom = new ToolStripMenuItem("전체 데이터 보기");
            resetZoom.Click += (s, ev) =>
            {
                chkAutoScale.Checked = true;
                isUserZooming = false;
                UpdatePlot();
            };
            cm.Items.Add(resetZoom);

            cm.Items.Add(new ToolStripSeparator());

            // 시간 범위 메뉴
            var timeRangeMenu = new ToolStripMenuItem("시간 범위");

            var customTimeRange = new ToolStripMenuItem("사용자 정의 시간 범위...");
            customTimeRange.Click += (s, ev) =>
            {
                chkEnableTimeRange.Checked = true;
                dtpStartTime.Focus();
            };
            timeRangeMenu.DropDownItems.Add(customTimeRange);

            timeRangeMenu.DropDownItems.Add(new ToolStripSeparator());

            var last1Hour = new ToolStripMenuItem("최근 1시간");
            last1Hour.Click += (s, ev) => SetTimeRange(1);
            timeRangeMenu.DropDownItems.Add(last1Hour);

            var last3Hours = new ToolStripMenuItem("최근 3시간");
            last3Hours.Click += (s, ev) => SetTimeRange(3);
            timeRangeMenu.DropDownItems.Add(last3Hours);

            var last24Hours = new ToolStripMenuItem("최근 24시간");
            last24Hours.Click += (s, ev) => SetTimeRange(24);
            timeRangeMenu.DropDownItems.Add(last24Hours);

            var allData = new ToolStripMenuItem("전체 데이터");
            allData.Click += (s, ev) =>
            {
                chkEnableTimeRange.Checked = false;
                chkSyncTimeAxis.Checked = true;
                UpdatePlot();
            };
            timeRangeMenu.DropDownItems.Add(allData);

            cm.Items.Add(timeRangeMenu);

            cm.Items.Add(new ToolStripSeparator());

            var saveImage = new ToolStripMenuItem("이미지로 저장...");
            saveImage.Click += (s, ev) => SavePlotImage();
            cm.Items.Add(saveImage);

            var exportData = new ToolStripMenuItem("데이터 내보내기...");
            exportData.Click += (s, ev) => ExportPlotData();
            cm.Items.Add(exportData);

            cm.Show(formsPlot, location);
        }

        private void SetTimeRange(int hours)
        {
            double now = DateTime.Now.ToOADate();
            double start = DateTime.Now.AddHours(-hours).ToOADate();

            isProgrammaticChange = true;
            try
            {
                formsPlot.Plot.SetAxisLimits(xMin: start, xMax: now);
                isUserZooming = true;
                AutoScaleYAxis();
            }
            finally
            {
                isProgrammaticChange = false;
            }

            formsPlot.Render();
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
                            if (fileInfo.SelectedColumns.Contains(col.Key) &&
                                i < col.Value.Count && !double.IsNaN(col.Value[i]))
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

        private void ChkEnableTimeRange_CheckedChanged(object sender, EventArgs e)
        {
            isTimeRangeEnabled = chkEnableTimeRange.Checked;
            dtpStartTime.Enabled = isTimeRangeEnabled;
            dtpEndTime.Enabled = isTimeRangeEnabled;
            btnApplyTimeRange.Enabled = isTimeRangeEnabled;

            if (!isTimeRangeEnabled)
            {
                // 시간 범위를 사용하지 않으면 전체 데이터 표시
                customStartTime = null;
                customEndTime = null;
                chkAutoScale.Checked = true;

                // 데이터 다시 읽기 (시간 필터링 해제)
                UpdateGraph(null);
            }
            else
            {
                // 시간 범위 사용 시 자동 스케일 해제
                chkAutoScale.Checked = false;
                chkSyncTimeAxis.Checked = false;

                // 현재 로드된 데이터의 시간 범위로 초기값 설정
                SetDefaultTimeRange();
            }
        }

        private void BtnApplyTimeRange_Click(object sender, EventArgs e)
        {
            if (dtpStartTime.Value >= dtpEndTime.Value)
            {
                MessageBox.Show("종료 시간은 시작 시간보다 이후여야 합니다.", "시간 범위 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            customStartTime = dtpStartTime.Value;
            customEndTime = dtpEndTime.Value;

            // 데이터 다시 읽기 (시간 범위 필터링 적용)
            UpdateGraph(null);

            // 사용자 정의 시간 범위 적용
            isProgrammaticChange = true;
            try
            {
                formsPlot.Plot.SetAxisLimits(
                    xMin: customStartTime.Value.ToOADate(),
                    xMax: customEndTime.Value.ToOADate()
                );

                isUserZooming = true;
                chkAutoScale.Checked = false;
                AutoScaleYAxis();
            }
            finally
            {
                isProgrammaticChange = false;
            }

            formsPlot.Render();
        }

        private void SetDefaultTimeRange()
        {
            DateTime? minTime = null;
            DateTime? maxTime = null;

            foreach (var file in csvFiles.Values)
            {
                if (file.Timestamps.Count > 0)
                {
                    var fileMin = file.Timestamps.Min();
                    var fileMax = file.Timestamps.Max();

                    if (!minTime.HasValue || fileMin < minTime.Value)
                        minTime = fileMin;
                    if (!maxTime.HasValue || fileMax > maxTime.Value)
                        maxTime = fileMax;
                }
            }

            if (minTime.HasValue && maxTime.HasValue)
            {
                dtpStartTime.Value = minTime.Value;
                dtpEndTime.Value = maxTime.Value;

                // 시간 범위 정보 표시
                lblStatus.Text = $"데이터 시간 범위: {minTime.Value:yyyy-MM-dd HH:mm:ss} ~ {maxTime.Value:yyyy-MM-dd HH:mm:ss}";
            }
            else
            {
                dtpStartTime.Value = DateTime.Now.AddHours(-1);
                dtpEndTime.Value = DateTime.Now;
            }
        }

        private void BtnMonitoring_Click(object sender, EventArgs e)
        {
            isMonitoringEnabled = !isMonitoringEnabled;

            if (isMonitoringEnabled)
            {
                btnMonitoring.Text = "🟢 모니터링 ON";
                btnMonitoring.BackColor = Color.LightGreen;
                StartUpdateTimer();
                lblStatus.Text = "실시간 모니터링 활성화됨";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                btnMonitoring.Text = "🔴 모니터링 OFF";
                btnMonitoring.BackColor = Color.LightCoral;
                StopUpdateTimer();
                lblStatus.Text = "실시간 모니터링 중지됨";
                lblStatus.ForeColor = Color.Orange;
            }
        }

        private void BtnToggleBottom_Click(object sender, EventArgs e)
        {
            isBottomPanelVisible = !isBottomPanelVisible;

            if (isBottomPanelVisible)
            {
                pnlBottom.Visible = true;
                splitterBottom.Visible = true;
                btnToggleBottom.Text = "▼ 패널 숨기기";
            }
            else
            {
                pnlBottom.Visible = false;
                splitterBottom.Visible = false;
                btnToggleBottom.Text = "▲ 패널 보이기";
            }
        }

        private void ChkEnableMonitoring_CheckedChanged(object sender, EventArgs e)
        {
            // 이전 버전 호환성을 위해 유지
            isMonitoringEnabled = chkEnableMonitoring.Checked;

            if (btnMonitoring != null)
            {
                if (isMonitoringEnabled)
                {
                    btnMonitoring.Text = "🟢 모니터링 ON";
                    btnMonitoring.BackColor = Color.LightGreen;
                }
                else
                {
                    btnMonitoring.Text = "🔴 모니터링 OFF";
                    btnMonitoring.BackColor = Color.LightCoral;
                }
            }

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

                    // 시간 범위가 활성화되어 있고 첫 파일 추가시 기본값 설정
                    if (chkEnableTimeRange != null && chkEnableTimeRange.Checked && csvFiles.Count > 0)
                    {
                        SetDefaultTimeRange();
                    }
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

                        // 숫자 컬럼 자동 감지
                        DetectNumericColumns(fileInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"헤더 읽기 오류 ({fileInfo.FileName}): {ex.Message}",
                              "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DetectNumericColumns(CsvFileInfo fileInfo)
        {
            try
            {
                using (var fs = new FileStream(fileInfo.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    reader.ReadLine(); // Skip header

                    // 최대 10개 행을 읽어서 데이터 타입 확인
                    int sampleRows = 0;
                    var numericColumns = new Dictionary<string, int>();

                    foreach (var header in fileInfo.Headers)
                    {
                        numericColumns[header] = 0;
                    }

                    string line;
                    while ((line = reader.ReadLine()) != null && sampleRows < 10)
                    {
                        string[] values = line.Split(',');

                        for (int i = 0; i < Math.Min(values.Length, fileInfo.Headers.Count); i++)
                        {
                            string value = values[i].Trim();
                            double numValue;

                            // 숫자로 변환 가능한지 확인
                            if (TryParseValue(value, out numValue) && !double.IsNaN(numValue))
                            {
                                numericColumns[fileInfo.Headers[i]]++;
                            }
                        }

                        sampleRows++;
                    }

                    // 50% 이상이 숫자인 컬럼을 숫자 컬럼으로 판단
                    foreach (var header in fileInfo.Headers)
                    {
                        bool isNumeric = numericColumns[header] >= sampleRows / 2;
                        columnIsNumeric[header] = isNumeric;

                        // 자동으로 숫자 컬럼 선택 (시간 컬럼 제외)
                        if (isNumeric && fileInfo.Headers.IndexOf(header) != fileInfo.TimeColumnIndex)
                        {
                            // 압력 관련 컬럼 우선 선택
                            if (header.Contains("Pressure", StringComparison.OrdinalIgnoreCase) ||
                                header.Contains("Ion", StringComparison.OrdinalIgnoreCase) ||
                                header.Contains("Pirani", StringComparison.OrdinalIgnoreCase) ||
                                header.Contains("ATM", StringComparison.OrdinalIgnoreCase))
                            {
                                fileInfo.SelectedColumns.Add(header);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"숫자 컬럼 감지 오류: {ex.Message}");
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

                // 숫자 컬럼들을 먼저 추가
                var numericColumns = new List<TreeNode>();
                var textColumns = new List<TreeNode>();

                for (int i = 0; i < file.Headers.Count; i++)
                {
                    if (i == file.TimeColumnIndex) continue; // 시간 컬럼은 제외

                    var header = file.Headers[i];
                    var columnNode = new TreeNode(header)
                    {
                        Tag = i,
                        Checked = file.SelectedColumns.Contains(header)
                    };

                    // 숫자/텍스트 구분하여 표시
                    if (columnIsNumeric.ContainsKey(header) && columnIsNumeric[header])
                    {
                        columnNode.ForeColor = Color.DarkGreen;
                        columnNode.Text = $"📊 {header}";
                        numericColumns.Add(columnNode);
                    }
                    else
                    {
                        columnNode.ForeColor = Color.Gray;
                        columnNode.Text = $"📝 {header}";
                        textColumns.Add(columnNode);
                    }

                    // 필터 표시
                    if (file.Filters.ContainsKey(header) && file.Filters[header].Enabled)
                    {
                        columnNode.ForeColor = Color.Red;
                        columnNode.Text += " [필터]";
                    }
                }

                // 숫자 컬럼 먼저, 그 다음 텍스트 컬럼 추가
                foreach (var node in numericColumns)
                    fileNode.Nodes.Add(node);
                foreach (var node in textColumns)
                    fileNode.Nodes.Add(node);

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
                    if (childNode.Tag.ToString() != "TIME_COLUMN" &&
                        childNode.Text.StartsWith("📊")) // 숫자 컬럼만
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

                        // 텍스트 컬럼은 선택 불가
                        if (!columnIsNumeric.ContainsKey(columnName) || !columnIsNumeric[columnName])
                        {
                            e.Node.Checked = false;
                            MessageBox.Show("숫자 데이터가 아닌 컬럼은 선택할 수 없습니다.",
                                          "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

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

                this.BeginInvoke((MethodInvoker)delegate
                {
                    UpdatePlot();
                    UpdateCurrentValues();
                });
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblStatus.Text = $"오류: {ex.Message}";
                    lblStatus.ForeColor = Color.Red;
                });
            }
        }

        private void UpdateCurrentValues()
        {
            if (dgvCurrentValues == null || dgvCurrentValues.IsDisposed) return;

            dgvCurrentValues.Rows.Clear();

            foreach (var file in csvFiles)
            {
                var fileInfo = file.Value;
                if (fileInfo.Timestamps.Count == 0) continue;

                // 마지막 데이터 인덱스
                int lastIdx = fileInfo.Timestamps.Count - 1;

                foreach (var column in fileInfo.DataColumns)
                {
                    if (!fileInfo.SelectedColumns.Contains(column.Key)) continue;
                    if (lastIdx >= column.Value.Count) continue;

                    double value = column.Value[lastIdx];
                    if (double.IsNaN(value)) continue;

                    string unit = GuessUnit(column.Key);
                    string status = "정상";
                    Color statusColor = Color.Green;

                    // 압력 데이터 상태 판단
                    if (column.Key.Contains("Pressure", StringComparison.OrdinalIgnoreCase) ||
                        column.Key.Contains("Torr", StringComparison.OrdinalIgnoreCase))
                    {
                        var vacuumLevel = PressureAnalysisTools.GetVacuumLevel(value);

                        if (vacuumLevel == PressureAnalysisTools.VacuumLevel.Atmospheric)
                        {
                            status = "대기압";
                            statusColor = Color.Red;
                        }
                        else if (vacuumLevel == PressureAnalysisTools.VacuumLevel.RoughVacuum)
                        {
                            status = "저진공";
                            statusColor = Color.Orange;
                        }
                        else if (vacuumLevel == PressureAnalysisTools.VacuumLevel.MediumVacuum ||
                                vacuumLevel == PressureAnalysisTools.VacuumLevel.HighVacuum)
                        {
                            status = "진공";
                            statusColor = Color.Green;
                        }
                        else
                        {
                            status = "고진공";
                            statusColor = Color.Blue;
                        }
                    }

                    int rowIdx = dgvCurrentValues.Rows.Add(
                        Path.GetFileNameWithoutExtension(fileInfo.FileName),
                        column.Key,
                        FormatValue(value, unit),
                        unit,
                        status
                    );

                    dgvCurrentValues.Rows[rowIdx].Cells["Status"].Style.ForeColor = statusColor;
                    dgvCurrentValues.Rows[rowIdx].Cells["Status"].Style.Font =
                        new Font(dgvCurrentValues.Font, FontStyle.Bold);
                }
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

                            // 시간 범위 필터링 (읽기 단계에서 필터링하여 메모리 효율성 향상)
                            if (isTimeRangeEnabled && customStartTime.HasValue && customEndTime.HasValue)
                            {
                                if (timestamp < customStartTime.Value || timestamp > customEndTime.Value)
                                {
                                    rowNumber++;
                                    continue;
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

                // 단위별로 그룹화
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
                                var signal = formsPlot.Plot.AddSignalXY(
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

                // 축 설정 로직
                isProgrammaticChange = true;
                try
                {
                    if (isTimeRangeEnabled && customStartTime.HasValue && customEndTime.HasValue)
                    {
                        // 사용자 정의 시간 범위가 설정된 경우
                        formsPlot.Plot.SetAxisLimits(
                            xMin: customStartTime.Value.ToOADate(),
                            xMax: customEndTime.Value.ToOADate()
                        );
                        AutoScaleYAxis();
                    }
                    else if (hadValidLimits)
                    {
                        // 이전 축 설정이 있는 경우
                        if (isUserZooming || !chkAutoScale.Checked)
                        {
                            // 사용자가 줌/팬을 했거나 자동 스케일이 꺼져있으면 이전 설정 유지
                            formsPlot.Plot.SetAxisLimits(existingLimits);
                        }
                        else if (isMonitoringEnabled && chkAutoScale.Checked)
                        {
                            // 실시간 모니터링 중이고 자동 스케일이 켜져 있을 때만 최신 데이터 추적
                            ApplyRealtimeTracking();
                        }
                        else if (chkSyncTimeAxis.Checked)
                        {
                            // 시간축 동기화
                            ApplySyncTimeAxis();
                        }
                        else
                        {
                            // 그 외의 경우 이전 설정 유지
                            formsPlot.Plot.SetAxisLimits(existingLimits);
                        }
                    }
                    else
                    {
                        // 처음 로드시
                        if (chkSyncTimeAxis.Checked)
                        {
                            ApplySyncTimeAxis();
                        }
                        else
                        {
                            formsPlot.Plot.AxisAuto();
                        }
                    }
                }
                finally
                {
                    isProgrammaticChange = false;
                }

                if (totalColumns > 0 && isLegendVisible)
                {
                    var legend = formsPlot.Plot.Legend(true);
                    legend.Location = Alignment.UpperRight;
                    legend.FontSize = 10;
                }

                formsPlot.Render();

                string monitoringStatus = isMonitoringEnabled ? "모니터링 중" : "모니터링 중지";
                string zoomStatus = isUserZooming ? " [수동 줌]" : "";
                string trackingStatus = (isMonitoringEnabled && chkAutoScale.Checked && !isUserZooming)
                    ? " [실시간 추적]" : "";
                string timeRangeStatus = isTimeRangeEnabled && customStartTime.HasValue && customEndTime.HasValue
                    ? $" [{customStartTime.Value:MM-dd HH:mm} ~ {customEndTime.Value:MM-dd HH:mm}]" : "";
                string shortcutHint = isUserZooming ? " (R: 실시간 추적 재개)" : "";

                lblStatus.Text = $"{monitoringStatus}{zoomStatus}{trackingStatus}{timeRangeStatus}{shortcutHint} | " +
                               $"파일: {csvFiles.Count}개, " +
                               $"데이터: {totalPoints}개, " +
                               $"컬럼: {totalColumns}개, " +
                               $"Y축: {Math.Min(columnGroups.Count, 2)}개 | " +
                               $"업데이트: {DateTime.Now:HH:mm:ss}";
                lblStatus.ForeColor = isMonitoringEnabled ? Color.Green : Color.Orange;
            }
        }

        private void ApplyRealtimeTracking()
        {
            // 시간 범위가 설정되어 있으면 그 범위 유지
            if (isTimeRangeEnabled && customStartTime.HasValue && customEndTime.HasValue)
            {
                bool wasAlreadyProgrammatic = isProgrammaticChange;
                if (!wasAlreadyProgrammatic) isProgrammaticChange = true;

                try
                {
                    formsPlot.Plot.SetAxisLimits(
                        xMin: customStartTime.Value.ToOADate(),
                        xMax: customEndTime.Value.ToOADate()
                    );

                    AutoScaleYAxis();
                }
                finally
                {
                    if (!wasAlreadyProgrammatic) isProgrammaticChange = false;
                }
                return;
            }

            DateTime? maxTime = null;
            DateTime? minTime = null;

            foreach (var fileInfo in csvFiles.Values)
            {
                if (fileInfo.Timestamps.Count > 0)
                {
                    var fileMax = fileInfo.Timestamps.Max();
                    maxTime = maxTime == null ? fileMax : (fileMax > maxTime ? fileMax : maxTime);
                }
            }

            if (maxTime.HasValue)
            {
                double windowMinutes = nudTimeWindow != null ? (double)nudTimeWindow.Value : 5.0;
                DateTime windowStart = maxTime.Value.AddMinutes(-windowMinutes);

                foreach (var fileInfo in csvFiles.Values)
                {
                    if (fileInfo.Timestamps.Count > 0)
                    {
                        var fileMin = fileInfo.Timestamps.Min();
                        minTime = minTime == null ? fileMin : (fileMin < minTime ? fileMin : minTime);
                    }
                }

                bool wasAlreadyProgrammatic = isProgrammaticChange;
                if (!wasAlreadyProgrammatic) isProgrammaticChange = true;

                try
                {
                    if (minTime.HasValue && (maxTime.Value - minTime.Value).TotalMinutes < windowMinutes)
                    {
                        formsPlot.Plot.SetAxisLimits(
                            xMin: minTime.Value.ToOADate(),
                            xMax: maxTime.Value.ToOADate()
                        );
                    }
                    else
                    {
                        formsPlot.Plot.SetAxisLimits(
                            xMin: windowStart.ToOADate(),
                            xMax: maxTime.Value.ToOADate()
                        );
                    }

                    AutoScaleYAxis();
                }
                finally
                {
                    if (!wasAlreadyProgrammatic) isProgrammaticChange = false;
                }
            }
        }

        private void ApplySyncTimeAxis()
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
                bool wasAlreadyProgrammatic = isProgrammaticChange;
                if (!wasAlreadyProgrammatic) isProgrammaticChange = true;

                try
                {
                    formsPlot.Plot.SetAxisLimits(
                        xMin: minTime.Value.ToOADate(),
                        xMax: maxTime.Value.ToOADate()
                    );
                }
                finally
                {
                    if (!wasAlreadyProgrammatic) isProgrammaticChange = false;
                }
            }
        }

        private string GuessUnit(string columnName)
        {
            columnName = columnName.ToLower();

            if (columnName.Contains("pressure") || columnName.Contains("torr"))
                return "압력 (Torr)";
            else if (columnName.Contains("kpa"))
                return "압력 (kPa)";
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
            // 시간 범위가 활성화되어 있으면 자동 스케일 비활성화
            if (isTimeRangeEnabled && chkAutoScale.Checked)
            {
                chkAutoScale.Checked = false;
                MessageBox.Show("시간 범위가 설정되어 있을 때는 자동 스케일을 사용할 수 없습니다.",
                    "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (chkAutoScale.Checked)
            {
                // 자동 스케일을 켜면 수동 줌 상태 해제
                isUserZooming = false;

                // 실시간 모니터링 중이면 최신 데이터로 이동
                if (isMonitoringEnabled)
                {
                    isProgrammaticChange = true;
                    try
                    {
                        ApplyRealtimeTracking();
                    }
                    finally
                    {
                        isProgrammaticChange = false;
                    }
                    formsPlot.Render();
                }
            }

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
                    h.Contains("Pressure", StringComparison.OrdinalIgnoreCase) ||
                    h.Contains("Torr", StringComparison.OrdinalIgnoreCase)).ToList();

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
                    h.Contains("Pressure", StringComparison.OrdinalIgnoreCase) ||
                    h.Contains("Torr", StringComparison.OrdinalIgnoreCase)).ToList();

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

    /// <summary>
    /// 선택된 데이터 포인트를 나타내는 클래스
    /// </summary>
    public class SelectedPointData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Label { get; set; }
        public string File { get; set; }
        public string Column { get; set; }
    }
}