using System.Drawing;
using System.Windows.Forms;

namespace CsvTimeSeriesViewer
{
    partial class PressureAnalysisForm
    {
        private System.ComponentModel.IContainer components = null;

        private DataGridView dgvAnalysis;
        private Button btnRefresh;
        private Button btnExport;
        private Label lblTitle;
        private Panel pnlTop;
        private Panel pnlBottom;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgvAnalysis = new System.Windows.Forms.DataGridView();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.pnlBottom = new System.Windows.Forms.Panel();

            ((System.ComponentModel.ISupportInitialize)(this.dgvAnalysis)).BeginInit();
            this.pnlTop.SuspendLayout();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();

            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.lblTitle);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1000, 50);
            this.pnlTop.TabIndex = 0;

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(20, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(200, 21);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "압력 데이터 종합 분석";

            // 
            // dgvAnalysis
            // 
            this.dgvAnalysis.AllowUserToAddRows = false;
            this.dgvAnalysis.AllowUserToDeleteRows = false;
            this.dgvAnalysis.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAnalysis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAnalysis.Location = new System.Drawing.Point(0, 50);
            this.dgvAnalysis.Name = "dgvAnalysis";
            this.dgvAnalysis.ReadOnly = true;
            this.dgvAnalysis.RowTemplate.Height = 23;
            this.dgvAnalysis.Size = new System.Drawing.Size(1000, 450);
            this.dgvAnalysis.TabIndex = 1;
            this.dgvAnalysis.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.DgvAnalysis_CellFormatting);

            // 컬럼 추가
            this.dgvAnalysis.Columns.Add("colFile", "파일명");
            this.dgvAnalysis.Columns.Add("colColumn", "컬럼");
            this.dgvAnalysis.Columns.Add("colMin", "최소 압력 (Torr)");
            this.dgvAnalysis.Columns.Add("colMax", "최대 압력 (Torr)");
            this.dgvAnalysis.Columns.Add("colAvg", "평균 압력 (Torr)");
            this.dgvAnalysis.Columns.Add("colStdDev", "표준편차");
            this.dgvAnalysis.Columns.Add("colVacuumLevel", "진공 레벨");
            this.dgvAnalysis.Columns.Add("colLeakRate", "리크율 (Torr/sec)");
            this.dgvAnalysis.Columns.Add("colSpikes", "스파이크 수");

            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.btnExport);
            this.pnlBottom.Controls.Add(this.btnRefresh);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 500);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(1000, 50);
            this.pnlBottom.TabIndex = 2;

            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(20, 10);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(100, 30);
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "새로고침";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);

            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(130, 10);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(100, 30);
            this.btnExport.TabIndex = 1;
            this.btnExport.Text = "내보내기";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.BtnExport_Click);

            // 
            // PressureAnalysisForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 550);
            this.Controls.Add(this.dgvAnalysis);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlTop);
            this.Name = "PressureAnalysisForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "압력 데이터 분석";

            ((System.ComponentModel.ISupportInitialize)(this.dgvAnalysis)).EndInit();
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.pnlBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}