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
        private SplitContainer splitMain;
        private SplitContainer splitLeft;
        private Panel pnlTop;

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
            this.grpSettings = new System.Windows.Forms.GroupBox();
            this.lblUpdateInterval = new System.Windows.Forms.Label();
            this.nudUpdateInterval = new System.Windows.Forms.NumericUpDown();
            this.chkAutoScale = new System.Windows.Forms.CheckBox();
            this.lblSyncTime = new System.Windows.Forms.Label();
            this.chkSyncTimeAxis = new System.Windows.Forms.CheckBox();
            this.btnRefreshAll = new System.Windows.Forms.Button();
            this.lblMonitoring = new System.Windows.Forms.Label();
            this.chkEnableMonitoring = new System.Windows.Forms.CheckBox();
            this.btnAnalysis = new System.Windows.Forms.Button();
            this.contextMenuAnalysis = new System.Windows.Forms.ContextMenuStrip();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.splitLeft = new System.Windows.Forms.SplitContainer();
            this.grpFiles = new System.Windows.Forms.GroupBox();
            this.btnAddFile = new System.Windows.Forms.Button();
            this.btnRemoveFile = new System.Windows.Forms.Button();
            this.lstFiles = new System.Windows.Forms.ListBox();
            this.grpColumns = new System.Windows.Forms.GroupBox();
            this.trvColumns = new System.Windows.Forms.TreeView();
            this.btnFilter = new System.Windows.Forms.Button();
            this.formsPlot = new ScottPlot.FormsPlot();
            this.lblCrosshair = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();

            this.pnlTop.SuspendLayout();
            this.grpSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudUpdateInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitLeft)).BeginInit();
            this.splitLeft.Panel1.SuspendLayout();
            this.splitLeft.Panel2.SuspendLayout();
            this.splitLeft.SuspendLayout();
            this.grpFiles.SuspendLayout();
            this.grpColumns.SuspendLayout();
            this.SuspendLayout();

            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.grpSettings);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1500, 100);
            this.pnlTop.TabIndex = 0;

            // 
            // grpSettings
            // 
            this.grpSettings.Controls.Add(this.lblMonitoring);
            this.grpSettings.Controls.Add(this.chkEnableMonitoring);
            this.grpSettings.Controls.Add(this.lblUpdateInterval);
            this.grpSettings.Controls.Add(this.nudUpdateInterval);
            this.grpSettings.Controls.Add(this.chkAutoScale);
            this.grpSettings.Controls.Add(this.lblSyncTime);
            this.grpSettings.Controls.Add(this.chkSyncTimeAxis);
            this.grpSettings.Controls.Add(this.btnRefreshAll);
            this.grpSettings.Controls.Add(this.btnAnalysis);
            this.grpSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSettings.Location = new System.Drawing.Point(0, 0);
            this.grpSettings.Name = "grpSettings";
            this.grpSettings.Size = new System.Drawing.Size(1500, 100);
            this.grpSettings.TabIndex = 0;
            this.grpSettings.TabStop = false;
            this.grpSettings.Text = "설정";

            // 
            // lblMonitoring
            // 
            this.lblMonitoring.AutoSize = true;
            this.lblMonitoring.Location = new System.Drawing.Point(20, 25);
            this.lblMonitoring.Name = "lblMonitoring";
            this.lblMonitoring.Size = new System.Drawing.Size(81, 12);
            this.lblMonitoring.TabIndex = 0;
            this.lblMonitoring.Text = "실시간 모니터링:";

            // 
            // chkEnableMonitoring
            // 
            this.chkEnableMonitoring.AutoSize = true;
            this.chkEnableMonitoring.Checked = true;
            this.chkEnableMonitoring.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnableMonitoring.Location = new System.Drawing.Point(106, 25);
            this.chkEnableMonitoring.Name = "chkEnableMonitoring";
            this.chkEnableMonitoring.Size = new System.Drawing.Size(48, 16);
            this.chkEnableMonitoring.TabIndex = 1;
            this.chkEnableMonitoring.Text = "활성";
            this.chkEnableMonitoring.UseVisualStyleBackColor = true;
            this.chkEnableMonitoring.CheckedChanged += new System.EventHandler(this.ChkEnableMonitoring_CheckedChanged);

            // 
            // lblUpdateInterval
            // 
            this.lblUpdateInterval.AutoSize = true;
            this.lblUpdateInterval.Location = new System.Drawing.Point(180, 25);
            this.lblUpdateInterval.Name = "lblUpdateInterval";
            this.lblUpdateInterval.Size = new System.Drawing.Size(110, 12);
            this.lblUpdateInterval.TabIndex = 2;
            this.lblUpdateInterval.Text = "업데이트 간격(초):";

            // 
            // nudUpdateInterval
            // 
            this.nudUpdateInterval.DecimalPlaces = 1;
            this.nudUpdateInterval.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.nudUpdateInterval.Location = new System.Drawing.Point(295, 23);
            this.nudUpdateInterval.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.nudUpdateInterval.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudUpdateInterval.Name = "nudUpdateInterval";
            this.nudUpdateInterval.Size = new System.Drawing.Size(60, 21);
            this.nudUpdateInterval.TabIndex = 3;
            this.nudUpdateInterval.Value = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudUpdateInterval.ValueChanged += new System.EventHandler(this.NudUpdateInterval_ValueChanged);

            // 
            // chkAutoScale
            // 
            this.chkAutoScale.AutoSize = true;
            this.chkAutoScale.Checked = true;
            this.chkAutoScale.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoScale.Location = new System.Drawing.Point(380, 25);
            this.chkAutoScale.Name = "chkAutoScale";
            this.chkAutoScale.Size = new System.Drawing.Size(88, 16);
            this.chkAutoScale.TabIndex = 4;
            this.chkAutoScale.Text = "자동 스케일";
            this.chkAutoScale.UseVisualStyleBackColor = true;
            this.chkAutoScale.CheckedChanged += new System.EventHandler(this.ChkAutoScale_CheckedChanged);

            // 
            // lblSyncTime
            // 
            this.lblSyncTime.AutoSize = true;
            this.lblSyncTime.Location = new System.Drawing.Point(20, 55);
            this.lblSyncTime.Name = "lblSyncTime";
            this.lblSyncTime.Size = new System.Drawing.Size(81, 12);
            this.lblSyncTime.TabIndex = 5;
            this.lblSyncTime.Text = "시간축 동기화:";

            // 
            // chkSyncTimeAxis
            // 
            this.chkSyncTimeAxis.AutoSize = true;
            this.chkSyncTimeAxis.Location = new System.Drawing.Point(106, 55);
            this.chkSyncTimeAxis.Name = "chkSyncTimeAxis";
            this.chkSyncTimeAxis.Size = new System.Drawing.Size(116, 16);
            this.chkSyncTimeAxis.TabIndex = 6;
            this.chkSyncTimeAxis.Text = "동일 시간축 사용";
            this.chkSyncTimeAxis.UseVisualStyleBackColor = true;
            this.chkSyncTimeAxis.CheckedChanged += new System.EventHandler(this.ChkSyncTimeAxis_CheckedChanged);

            // 
            // btnRefreshAll
            // 
            this.btnRefreshAll.Location = new System.Drawing.Point(250, 50);
            this.btnRefreshAll.Name = "btnRefreshAll";
            this.btnRefreshAll.Size = new System.Drawing.Size(100, 30);
            this.btnRefreshAll.TabIndex = 7;
            this.btnRefreshAll.Text = "전체 새로고침";
            this.btnRefreshAll.UseVisualStyleBackColor = true;
            this.btnRefreshAll.Click += new System.EventHandler(this.BtnRefreshAll_Click);

            // 
            // btnAnalysis
            // 
            this.btnAnalysis.Location = new System.Drawing.Point(380, 50);
            this.btnAnalysis.Name = "btnAnalysis";
            this.btnAnalysis.Size = new System.Drawing.Size(100, 30);
            this.btnAnalysis.TabIndex = 8;
            this.btnAnalysis.Text = "압력 분석 ▼";
            this.btnAnalysis.UseVisualStyleBackColor = true;
            this.btnAnalysis.Click += new System.EventHandler(this.BtnAnalysis_Click);

            // 
            // contextMenuAnalysis
            // 
            this.contextMenuAnalysis.Name = "contextMenuAnalysis";
            this.contextMenuAnalysis.Size = new System.Drawing.Size(200, 26);

            // 
            // splitMain
            // 
            this.splitMain.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 100);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.splitLeft);
            this.splitMain.Panel1MinSize = 0;
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.formsPlot);
            this.splitMain.Panel2.Controls.Add(this.lblCrosshair);
            this.splitMain.Size = new System.Drawing.Size(1500, 695);
            this.splitMain.SplitterDistance = 600;
            this.splitMain.TabIndex = 1;

            // 
            // splitLeft
            // 
            this.splitLeft.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitLeft.Location = new System.Drawing.Point(0, 0);
            this.splitLeft.Name = "splitLeft";
            // 
            // splitLeft.Panel1
            // 
            this.splitLeft.Panel1.Controls.Add(this.grpFiles);
            this.splitLeft.Panel1MinSize = 0;
            // 
            // splitLeft.Panel2
            // 
            this.splitLeft.Panel2.Controls.Add(this.grpColumns);
            this.splitLeft.Panel2MinSize = 0;
            this.splitLeft.Size = new System.Drawing.Size(600, 695);
            this.splitLeft.SplitterDistance = 295;
            this.splitLeft.TabIndex = 0;

            // 
            // grpFiles
            // 
            this.grpFiles.Controls.Add(this.btnAddFile);
            this.grpFiles.Controls.Add(this.btnRemoveFile);
            this.grpFiles.Controls.Add(this.lstFiles);
            this.grpFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpFiles.Location = new System.Drawing.Point(0, 0);
            this.grpFiles.Name = "grpFiles";
            this.grpFiles.Size = new System.Drawing.Size(291, 691);
            this.grpFiles.TabIndex = 0;
            this.grpFiles.TabStop = false;
            this.grpFiles.Text = "CSV 파일 목록";

            // 
            // btnAddFile
            // 
            this.btnAddFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
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
            this.btnRemoveFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
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
            this.lstFiles.Size = new System.Drawing.Size(270, 616);
            this.lstFiles.TabIndex = 2;
            this.lstFiles.SelectedIndexChanged += new System.EventHandler(this.LstFiles_SelectedIndexChanged);

            // 
            // grpColumns
            // 
            this.grpColumns.Controls.Add(this.trvColumns);
            this.grpColumns.Controls.Add(this.btnFilter);
            this.grpColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpColumns.Location = new System.Drawing.Point(0, 0);
            this.grpColumns.Name = "grpColumns";
            this.grpColumns.Size = new System.Drawing.Size(297, 691);
            this.grpColumns.TabIndex = 0;
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
            this.trvColumns.Size = new System.Drawing.Size(277, 620);
            this.trvColumns.TabIndex = 0;
            this.trvColumns.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.TrvColumns_AfterCheck);

            // 
            // btnFilter
            // 
            this.btnFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFilter.Location = new System.Drawing.Point(10, 650);
            this.btnFilter.Name = "btnFilter";
            this.btnFilter.Size = new System.Drawing.Size(277, 30);
            this.btnFilter.TabIndex = 1;
            this.btnFilter.Text = "필터 설정";
            this.btnFilter.UseVisualStyleBackColor = true;
            this.btnFilter.Click += new System.EventHandler(this.BtnFilter_Click);

            // 
            // formsPlot
            // 
            this.formsPlot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.formsPlot.Location = new System.Drawing.Point(0, 0);
            this.formsPlot.Name = "formsPlot";
            this.formsPlot.Size = new System.Drawing.Size(892, 691);
            this.formsPlot.TabIndex = 0;

            // 
            // lblCrosshair
            // 
            this.lblCrosshair.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCrosshair.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.lblCrosshair.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCrosshair.Location = new System.Drawing.Point(600, 10);
            this.lblCrosshair.Name = "lblCrosshair";
            this.lblCrosshair.Padding = new System.Windows.Forms.Padding(5);
            this.lblCrosshair.Size = new System.Drawing.Size(280, 80);
            this.lblCrosshair.TabIndex = 1;
            this.lblCrosshair.Text = "데이터 포인트 정보";
            this.lblCrosshair.Visible = false;

            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.ForeColor = System.Drawing.Color.Blue;
            this.lblStatus.Location = new System.Drawing.Point(0, 795);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(1500, 20);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "대기 중...";

            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1500, 815);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.lblStatus);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "다중 CSV 실시간 시계열 데이터 뷰어";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);

            this.pnlTop.ResumeLayout(false);
            this.grpSettings.ResumeLayout(false);
            this.grpSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudUpdateInterval)).EndInit();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.splitLeft.Panel1.ResumeLayout(false);
            this.splitLeft.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitLeft)).EndInit();
            this.splitLeft.ResumeLayout(false);
            this.grpFiles.ResumeLayout(false);
            this.grpColumns.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}