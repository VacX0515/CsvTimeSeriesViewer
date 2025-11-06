using System.Drawing;
using System.Windows.Forms;

namespace CsvTimeSeriesViewer
{
    partial class MainForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // Controls
        private ScottPlot.FormsPlot formsPlot;
        private Button btnAddFile;
        private Button btnRemoveFile;
        private ListBox lstFiles;
        private Label lblStatus;
        private NumericUpDown nudUpdateInterval;
        private Label lblUpdateInterval;
        private NumericUpDown nudTimeWindow;
        private Label lblTimeWindow;
        private CheckBox chkAutoScale;
        private GroupBox grpSettings;
        private GroupBox grpFiles;
        private GroupBox grpColumns;
        private TreeView trvColumns;
        private Button btnRefreshAll;
        private CheckBox chkSyncTimeAxis;
        private Label lblSyncTime;
        private CheckBox chkEnableMonitoring;
        private Label lblMonitoring;
        private Button btnFilter;
        private Label lblCrosshair;
        private Button btnAnalysis;
        private ContextMenuStrip contextMenuAnalysis;
        private Panel pnlTop;
        private Panel pnlBottom;
        private Button btnToggleBottom;
        private Button btnToggleRight;
        private Button btnMonitoring;
        private Splitter splitterBottom;
        private Panel pnlMain;
        private GroupBox grpTimeRange;
        private DateTimePicker dtpStartTime;
        private DateTimePicker dtpEndTime;
        private Label lblStartTime;
        private Label lblEndTime;
        private Button btnApplyTimeRange;
        private CheckBox chkEnableTimeRange;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.grpTimeRange = new System.Windows.Forms.GroupBox();
            this.chkEnableTimeRange = new System.Windows.Forms.CheckBox();
            this.lblStartTime = new System.Windows.Forms.Label();
            this.dtpStartTime = new System.Windows.Forms.DateTimePicker();
            this.lblEndTime = new System.Windows.Forms.Label();
            this.dtpEndTime = new System.Windows.Forms.DateTimePicker();
            this.btnApplyTimeRange = new System.Windows.Forms.Button();
            this.grpSettings = new System.Windows.Forms.GroupBox();
            this.btnMonitoring = new System.Windows.Forms.Button();
            this.btnToggleBottom = new System.Windows.Forms.Button();
            this.lblUpdateInterval = new System.Windows.Forms.Label();
            this.nudUpdateInterval = new System.Windows.Forms.NumericUpDown();
            this.lblTimeWindow = new System.Windows.Forms.Label();
            this.nudTimeWindow = new System.Windows.Forms.NumericUpDown();
            this.chkAutoScale = new System.Windows.Forms.CheckBox();
            this.lblSyncTime = new System.Windows.Forms.Label();
            this.chkSyncTimeAxis = new System.Windows.Forms.CheckBox();
            this.btnRefreshAll = new System.Windows.Forms.Button();
            this.btnAnalysis = new System.Windows.Forms.Button();
            this.contextMenuAnalysis = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.pnlMain = new System.Windows.Forms.Panel();
            this.formsPlot = new ScottPlot.FormsPlot();
            this.lblCrosshair = new System.Windows.Forms.Label();
            this.splitterBottom = new System.Windows.Forms.Splitter();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.grpColumns = new System.Windows.Forms.GroupBox();
            this.trvColumns = new System.Windows.Forms.TreeView();
            this.btnFilter = new System.Windows.Forms.Button();
            this.grpFiles = new System.Windows.Forms.GroupBox();
            this.btnAddFile = new System.Windows.Forms.Button();
            this.btnRemoveFile = new System.Windows.Forms.Button();
            this.lstFiles = new System.Windows.Forms.ListBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblMonitoring = new System.Windows.Forms.Label();
            this.chkEnableMonitoring = new System.Windows.Forms.CheckBox();

            this.pnlTop.SuspendLayout();
            this.grpTimeRange.SuspendLayout();
            this.grpSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudUpdateInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTimeWindow)).BeginInit();
            this.pnlMain.SuspendLayout();
            this.pnlBottom.SuspendLayout();
            this.grpColumns.SuspendLayout();
            this.grpFiles.SuspendLayout();
            this.SuspendLayout();

            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.grpTimeRange);
            this.pnlTop.Controls.Add(this.grpSettings);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1500, 120);
            this.pnlTop.TabIndex = 0;

            // 
            // grpSettings
            // 
            this.grpSettings.Controls.Add(this.btnMonitoring);
            this.grpSettings.Controls.Add(this.btnToggleBottom);
            this.grpSettings.Controls.Add(this.lblUpdateInterval);
            this.grpSettings.Controls.Add(this.nudUpdateInterval);
            this.grpSettings.Controls.Add(this.lblTimeWindow);
            this.grpSettings.Controls.Add(this.nudTimeWindow);
            this.grpSettings.Controls.Add(this.chkAutoScale);
            this.grpSettings.Controls.Add(this.lblSyncTime);
            this.grpSettings.Controls.Add(this.chkSyncTimeAxis);
            this.grpSettings.Controls.Add(this.btnRefreshAll);
            this.grpSettings.Controls.Add(this.btnAnalysis);
            this.grpSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpSettings.Location = new System.Drawing.Point(0, 0);
            this.grpSettings.Name = "grpSettings";
            this.grpSettings.Size = new System.Drawing.Size(1500, 60);
            this.grpSettings.TabIndex = 0;
            this.grpSettings.TabStop = false;
            this.grpSettings.Text = "제어 패널";

            // 
            // btnMonitoring
            // 
            this.btnMonitoring.BackColor = System.Drawing.Color.LightGreen;
            this.btnMonitoring.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnMonitoring.Location = new System.Drawing.Point(20, 20);
            this.btnMonitoring.Name = "btnMonitoring";
            this.btnMonitoring.Size = new System.Drawing.Size(120, 35);
            this.btnMonitoring.TabIndex = 0;
            this.btnMonitoring.Text = "🟢 모니터링 ON";
            this.btnMonitoring.UseVisualStyleBackColor = false;
            this.btnMonitoring.Click += new System.EventHandler(this.BtnMonitoring_Click);

            // 
            // btnToggleBottom
            // 
            this.btnToggleBottom.Location = new System.Drawing.Point(150, 20);
            this.btnToggleBottom.Name = "btnToggleBottom";
            this.btnToggleBottom.Size = new System.Drawing.Size(100, 35);
            this.btnToggleBottom.TabIndex = 1;
            this.btnToggleBottom.Text = "▼ 패널 숨기기";
            this.btnToggleBottom.UseVisualStyleBackColor = true;
            this.btnToggleBottom.Click += new System.EventHandler(this.BtnToggleBottom_Click);

            // 
            // btnToggleRight (새로 추가)
            // 
            var btnToggleRight = new System.Windows.Forms.Button();
            btnToggleRight.Location = new System.Drawing.Point(1100, 20);
            btnToggleRight.Name = "btnToggleRight";
            btnToggleRight.Size = new System.Drawing.Size(100, 35);
            btnToggleRight.TabIndex = 11;
            btnToggleRight.Text = "◀ 정보 패널";
            btnToggleRight.UseVisualStyleBackColor = true;
            btnToggleRight.Click += new System.EventHandler(this.BtnToggleRight_Click);
            this.grpSettings.Controls.Add(btnToggleRight);


            // 
            // lblUpdateInterval
            // 
            this.lblUpdateInterval.AutoSize = true;
            this.lblUpdateInterval.Location = new System.Drawing.Point(270, 30);
            this.lblUpdateInterval.Name = "lblUpdateInterval";
            this.lblUpdateInterval.Size = new System.Drawing.Size(95, 12);
            this.lblUpdateInterval.TabIndex = 2;
            this.lblUpdateInterval.Text = "업데이트 간격(초):";

            // 
            // nudUpdateInterval
            // 
            this.nudUpdateInterval.DecimalPlaces = 1;
            this.nudUpdateInterval.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.nudUpdateInterval.Location = new System.Drawing.Point(370, 27);
            this.nudUpdateInterval.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.nudUpdateInterval.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudUpdateInterval.Name = "nudUpdateInterval";
            this.nudUpdateInterval.Size = new System.Drawing.Size(60, 21);
            this.nudUpdateInterval.TabIndex = 3;
            this.nudUpdateInterval.Value = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudUpdateInterval.ValueChanged += new System.EventHandler(this.NudUpdateInterval_ValueChanged);

            // 
            // lblTimeWindow
            // 
            this.lblTimeWindow.AutoSize = true;
            this.lblTimeWindow.Location = new System.Drawing.Point(450, 30);
            this.lblTimeWindow.Name = "lblTimeWindow";
            this.lblTimeWindow.Size = new System.Drawing.Size(83, 12);
            this.lblTimeWindow.TabIndex = 4;
            this.lblTimeWindow.Text = "표시 시간(분):";

            // 
            // nudTimeWindow
            // 
            this.nudTimeWindow.Location = new System.Drawing.Point(540, 27);
            this.nudTimeWindow.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.nudTimeWindow.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudTimeWindow.Name = "nudTimeWindow";
            this.nudTimeWindow.Size = new System.Drawing.Size(60, 21);
            this.nudTimeWindow.TabIndex = 5;
            this.nudTimeWindow.Value = new decimal(new int[] { 5, 0, 0, 0 });

            // 
            // chkAutoScale
            // 
            this.chkAutoScale.AutoSize = true;
            this.chkAutoScale.Checked = true;
            this.chkAutoScale.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoScale.Location = new System.Drawing.Point(620, 30);
            this.chkAutoScale.Name = "chkAutoScale";
            this.chkAutoScale.Size = new System.Drawing.Size(88, 16);
            this.chkAutoScale.TabIndex = 6;
            this.chkAutoScale.Text = "자동 스케일";
            this.chkAutoScale.UseVisualStyleBackColor = true;
            this.chkAutoScale.CheckedChanged += new System.EventHandler(this.ChkAutoScale_CheckedChanged);

            // 
            // lblSyncTime
            // 
            this.lblSyncTime.AutoSize = true;
            this.lblSyncTime.Location = new System.Drawing.Point(720, 30);
            this.lblSyncTime.Name = "lblSyncTime";
            this.lblSyncTime.Size = new System.Drawing.Size(81, 12);
            this.lblSyncTime.TabIndex = 7;
            this.lblSyncTime.Text = "시간축 동기화:";

            // 
            // chkSyncTimeAxis
            // 
            this.chkSyncTimeAxis.AutoSize = true;
            this.chkSyncTimeAxis.Location = new System.Drawing.Point(810, 30);
            this.chkSyncTimeAxis.Name = "chkSyncTimeAxis";
            this.chkSyncTimeAxis.Size = new System.Drawing.Size(48, 16);
            this.chkSyncTimeAxis.TabIndex = 8;
            this.chkSyncTimeAxis.Text = "동기";
            this.chkSyncTimeAxis.UseVisualStyleBackColor = true;
            this.chkSyncTimeAxis.CheckedChanged += new System.EventHandler(this.ChkSyncTimeAxis_CheckedChanged);

            // 
            // btnRefreshAll
            // 
            this.btnRefreshAll.Location = new System.Drawing.Point(880, 20);
            this.btnRefreshAll.Name = "btnRefreshAll";
            this.btnRefreshAll.Size = new System.Drawing.Size(100, 35);
            this.btnRefreshAll.TabIndex = 9;
            this.btnRefreshAll.Text = "전체 새로고침";
            this.btnRefreshAll.UseVisualStyleBackColor = true;
            this.btnRefreshAll.Click += new System.EventHandler(this.BtnRefreshAll_Click);

            // 
            // btnAnalysis
            // 
            this.btnAnalysis.Location = new System.Drawing.Point(990, 20);
            this.btnAnalysis.Name = "btnAnalysis";
            this.btnAnalysis.Size = new System.Drawing.Size(100, 35);
            this.btnAnalysis.TabIndex = 10;
            this.btnAnalysis.Text = "압력 분석 ▼";
            this.btnAnalysis.UseVisualStyleBackColor = true;
            this.btnAnalysis.Click += new System.EventHandler(this.BtnAnalysis_Click);

            // 
            // contextMenuAnalysis
            // 
            this.contextMenuAnalysis.Name = "contextMenuAnalysis";
            this.contextMenuAnalysis.Size = new System.Drawing.Size(200, 26);

            // 
            // grpTimeRange
            // 
            this.grpTimeRange.Controls.Add(this.chkEnableTimeRange);
            this.grpTimeRange.Controls.Add(this.lblStartTime);
            this.grpTimeRange.Controls.Add(this.dtpStartTime);
            this.grpTimeRange.Controls.Add(this.lblEndTime);
            this.grpTimeRange.Controls.Add(this.dtpEndTime);
            this.grpTimeRange.Controls.Add(this.btnApplyTimeRange);
            this.grpTimeRange.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTimeRange.Location = new System.Drawing.Point(0, 60);
            this.grpTimeRange.Name = "grpTimeRange";
            this.grpTimeRange.Size = new System.Drawing.Size(1500, 60);
            this.grpTimeRange.TabIndex = 1;
            this.grpTimeRange.TabStop = false;
            this.grpTimeRange.Text = "시간 범위 설정";

            // 
            // chkEnableTimeRange
            // 
            this.chkEnableTimeRange.AutoSize = true;
            this.chkEnableTimeRange.Location = new System.Drawing.Point(20, 30);
            this.chkEnableTimeRange.Name = "chkEnableTimeRange";
            this.chkEnableTimeRange.Size = new System.Drawing.Size(100, 16);
            this.chkEnableTimeRange.TabIndex = 0;
            this.chkEnableTimeRange.Text = "시간 범위 사용";
            this.chkEnableTimeRange.UseVisualStyleBackColor = true;
            this.chkEnableTimeRange.CheckedChanged += new System.EventHandler(this.ChkEnableTimeRange_CheckedChanged);

            // 
            // lblStartTime
            // 
            this.lblStartTime.AutoSize = true;
            this.lblStartTime.Location = new System.Drawing.Point(150, 30);
            this.lblStartTime.Name = "lblStartTime";
            this.lblStartTime.Size = new System.Drawing.Size(57, 12);
            this.lblStartTime.TabIndex = 1;
            this.lblStartTime.Text = "시작 시간:";

            // 
            // dtpStartTime
            // 
            this.dtpStartTime.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.dtpStartTime.Enabled = false;
            this.dtpStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpStartTime.Location = new System.Drawing.Point(210, 26);
            this.dtpStartTime.Name = "dtpStartTime";
            this.dtpStartTime.Size = new System.Drawing.Size(180, 21);
            this.dtpStartTime.TabIndex = 2;

            // 
            // lblEndTime
            // 
            this.lblEndTime.AutoSize = true;
            this.lblEndTime.Location = new System.Drawing.Point(420, 30);
            this.lblEndTime.Name = "lblEndTime";
            this.lblEndTime.Size = new System.Drawing.Size(57, 12);
            this.lblEndTime.TabIndex = 3;
            this.lblEndTime.Text = "종료 시간:";

            // 
            // dtpEndTime
            // 
            this.dtpEndTime.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.dtpEndTime.Enabled = false;
            this.dtpEndTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpEndTime.Location = new System.Drawing.Point(480, 26);
            this.dtpEndTime.Name = "dtpEndTime";
            this.dtpEndTime.Size = new System.Drawing.Size(180, 21);
            this.dtpEndTime.TabIndex = 4;

            // 
            // btnApplyTimeRange
            // 
            this.btnApplyTimeRange.Enabled = false;
            this.btnApplyTimeRange.Location = new System.Drawing.Point(690, 20);
            this.btnApplyTimeRange.Name = "btnApplyTimeRange";
            this.btnApplyTimeRange.Size = new System.Drawing.Size(100, 35);
            this.btnApplyTimeRange.TabIndex = 5;
            this.btnApplyTimeRange.Text = "시간 범위 적용";
            this.btnApplyTimeRange.UseVisualStyleBackColor = true;
            this.btnApplyTimeRange.Click += new System.EventHandler(this.BtnApplyTimeRange_Click);

            // 빠른 시간 설정 버튼들
            var btnToday = new System.Windows.Forms.Button();
            btnToday.Location = new System.Drawing.Point(820, 20);
            btnToday.Name = "btnToday";
            btnToday.Size = new System.Drawing.Size(60, 35);
            btnToday.TabIndex = 6;
            btnToday.Text = "오늘";
            btnToday.UseVisualStyleBackColor = true;
            btnToday.Click += (s, ev) => {
                if (chkEnableTimeRange.Checked)
                {
                    dtpStartTime.Value = DateTime.Today;
                    dtpEndTime.Value = DateTime.Today.AddDays(1).AddSeconds(-1);
                    BtnApplyTimeRange_Click(s, ev);
                }
            };
            this.grpTimeRange.Controls.Add(btnToday);

            var btnYesterday = new System.Windows.Forms.Button();
            btnYesterday.Location = new System.Drawing.Point(890, 20);
            btnYesterday.Name = "btnYesterday";
            btnYesterday.Size = new System.Drawing.Size(60, 35);
            btnYesterday.TabIndex = 7;
            btnYesterday.Text = "어제";
            btnYesterday.UseVisualStyleBackColor = true;
            btnYesterday.Click += (s, ev) => {
                if (chkEnableTimeRange.Checked)
                {
                    dtpStartTime.Value = DateTime.Today.AddDays(-1);
                    dtpEndTime.Value = DateTime.Today.AddSeconds(-1);
                    BtnApplyTimeRange_Click(s, ev);
                }
            };
            this.grpTimeRange.Controls.Add(btnYesterday);

            var btnLastWeek = new System.Windows.Forms.Button();
            btnLastWeek.Location = new System.Drawing.Point(960, 20);
            btnLastWeek.Name = "btnLastWeek";
            btnLastWeek.Size = new System.Drawing.Size(60, 35);
            btnLastWeek.TabIndex = 8;
            btnLastWeek.Text = "1주일";
            btnLastWeek.UseVisualStyleBackColor = true;
            btnLastWeek.Click += (s, ev) => {
                if (chkEnableTimeRange.Checked)
                {
                    dtpStartTime.Value = DateTime.Today.AddDays(-7);
                    dtpEndTime.Value = DateTime.Now;
                    BtnApplyTimeRange_Click(s, ev);
                }
            };
            this.grpTimeRange.Controls.Add(btnLastWeek);

            // 
            // pnlMain
            // 
            this.pnlMain.Controls.Add(this.formsPlot);
            this.pnlMain.Controls.Add(this.lblCrosshair);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(0, 120);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(1500, 415);
            this.pnlMain.TabIndex = 1;

            // 
            // formsPlot
            // 
            this.formsPlot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.formsPlot.Location = new System.Drawing.Point(0, 0);
            this.formsPlot.Name = "formsPlot";
            this.formsPlot.Size = new System.Drawing.Size(1500, 415);
            this.formsPlot.TabIndex = 0;

            // 
            // lblCrosshair
            // 
            this.lblCrosshair.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCrosshair.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.lblCrosshair.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCrosshair.Location = new System.Drawing.Point(1200, 10);
            this.lblCrosshair.Name = "lblCrosshair";
            this.lblCrosshair.Padding = new System.Windows.Forms.Padding(5);
            this.lblCrosshair.Size = new System.Drawing.Size(280, 80);
            this.lblCrosshair.TabIndex = 1;
            this.lblCrosshair.Text = "데이터 포인트 정보";
            this.lblCrosshair.Visible = false;

            // 
            // splitterBottom
            // 
            this.splitterBottom.BackColor = System.Drawing.SystemColors.ControlDark;
            this.splitterBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitterBottom.Location = new System.Drawing.Point(0, 535);
            this.splitterBottom.Name = "splitterBottom";
            this.splitterBottom.Size = new System.Drawing.Size(1500, 3);
            this.splitterBottom.TabIndex = 2;
            this.splitterBottom.TabStop = false;

            // 
            // pnlBottom
            // 
            this.pnlBottom.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnlBottom.Controls.Add(this.grpColumns);
            this.pnlBottom.Controls.Add(this.grpFiles);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 538);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(1500, 260);
            this.pnlBottom.TabIndex = 3;

            // 
            // grpColumns
            // 
            this.grpColumns.Controls.Add(this.trvColumns);
            this.grpColumns.Controls.Add(this.btnFilter);
            this.grpColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpColumns.Location = new System.Drawing.Point(300, 0);
            this.grpColumns.Name = "grpColumns";
            this.grpColumns.Size = new System.Drawing.Size(1196, 256);
            this.grpColumns.TabIndex = 1;
            this.grpColumns.TabStop = false;
            this.grpColumns.Text = "데이터 컬럼 선택";

            // 
            // trvColumns
            // 
            this.trvColumns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trvColumns.CheckBoxes = true;
            this.trvColumns.Location = new System.Drawing.Point(10, 20);
            this.trvColumns.Name = "trvColumns";
            this.trvColumns.Size = new System.Drawing.Size(1100, 195);
            this.trvColumns.TabIndex = 0;
            this.trvColumns.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.TrvColumns_AfterCheck);

            // 
            // btnFilter
            // 
            this.btnFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFilter.Location = new System.Drawing.Point(1120, 185);
            this.btnFilter.Name = "btnFilter";
            this.btnFilter.Size = new System.Drawing.Size(70, 30);
            this.btnFilter.TabIndex = 1;
            this.btnFilter.Text = "필터 설정";
            this.btnFilter.UseVisualStyleBackColor = true;
            this.btnFilter.Click += new System.EventHandler(this.BtnFilter_Click);

            // 
            // grpFiles
            // 
            this.grpFiles.Controls.Add(this.btnAddFile);
            this.grpFiles.Controls.Add(this.btnRemoveFile);
            this.grpFiles.Controls.Add(this.lstFiles);
            this.grpFiles.Dock = System.Windows.Forms.DockStyle.Left;
            this.grpFiles.Location = new System.Drawing.Point(0, 0);
            this.grpFiles.Name = "grpFiles";
            this.grpFiles.Size = new System.Drawing.Size(300, 256);
            this.grpFiles.TabIndex = 0;
            this.grpFiles.TabStop = false;
            this.grpFiles.Text = "CSV 파일 목록";

            // 
            // btnAddFile
            // 
            this.btnAddFile.Location = new System.Drawing.Point(10, 20);
            this.btnAddFile.Name = "btnAddFile";
            this.btnAddFile.Size = new System.Drawing.Size(130, 30);
            this.btnAddFile.TabIndex = 0;
            this.btnAddFile.Text = "파일 추가";
            this.btnAddFile.UseVisualStyleBackColor = true;
            this.btnAddFile.Click += new System.EventHandler(this.BtnAddFile_Click);

            // 
            // btnRemoveFile
            // 
            this.btnRemoveFile.Location = new System.Drawing.Point(150, 20);
            this.btnRemoveFile.Name = "btnRemoveFile";
            this.btnRemoveFile.Size = new System.Drawing.Size(130, 30);
            this.btnRemoveFile.TabIndex = 1;
            this.btnRemoveFile.Text = "선택 파일 제거";
            this.btnRemoveFile.UseVisualStyleBackColor = true;
            this.btnRemoveFile.Click += new System.EventHandler(this.BtnRemoveFile_Click);

            // 
            // lstFiles
            // 
            this.lstFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstFiles.FormattingEnabled = true;
            this.lstFiles.HorizontalScrollbar = true;
            this.lstFiles.ItemHeight = 12;
            this.lstFiles.Location = new System.Drawing.Point(10, 60);
            this.lstFiles.Name = "lstFiles";
            this.lstFiles.Size = new System.Drawing.Size(280, 184);
            this.lstFiles.TabIndex = 2;
            this.lstFiles.SelectedIndexChanged += new System.EventHandler(this.LstFiles_SelectedIndexChanged);

            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.ForeColor = System.Drawing.Color.Blue;
            this.lblStatus.Location = new System.Drawing.Point(0, 798);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(1500, 20);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "대기 중...";

            // 숨겨진 컨트롤들 (호환성 유지)
            this.lblMonitoring = new System.Windows.Forms.Label();
            this.lblMonitoring.Visible = false;
            this.chkEnableMonitoring = new System.Windows.Forms.CheckBox();
            this.chkEnableMonitoring.Visible = false;
            this.chkEnableMonitoring.Checked = true;

            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1500, 818);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.splitterBottom);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.lblStatus);
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "다중 CSV 실시간 시계열 데이터 뷰어";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);

            this.pnlTop.ResumeLayout(false);
            this.grpTimeRange.ResumeLayout(false);
            this.grpTimeRange.PerformLayout();
            this.grpSettings.ResumeLayout(false);
            this.grpSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudUpdateInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTimeWindow)).EndInit();
            this.pnlMain.ResumeLayout(false);
            this.pnlBottom.ResumeLayout(false);
            this.grpColumns.ResumeLayout(false);
            this.grpFiles.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}