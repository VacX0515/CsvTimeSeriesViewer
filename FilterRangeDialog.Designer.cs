using System.Drawing;
using System.Windows.Forms;

namespace CsvTimeSeriesViewer
{
    partial class FilterRangeDialog
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private Label lblMin;
        private Label lblMax;
        private TextBox txtMin;
        private TextBox txtMax;
        private Button btnOK;
        private Button btnCancel;
        private Label lblDescription;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblMin = new System.Windows.Forms.Label();
            this.lblMax = new System.Windows.Forms.Label();
            this.txtMin = new System.Windows.Forms.TextBox();
            this.txtMax = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblDescription = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblDescription.Location = new System.Drawing.Point(20, 20);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(240, 12);
            this.lblDescription.TabIndex = 0;
            this.lblDescription.Text = "필터링할 데이터의 범위를 입력하세요.";

            // 
            // lblMin
            // 
            this.lblMin.AutoSize = true;
            this.lblMin.Location = new System.Drawing.Point(20, 50);
            this.lblMin.Name = "lblMin";
            this.lblMin.Size = new System.Drawing.Size(53, 12);
            this.lblMin.TabIndex = 1;
            this.lblMin.Text = "최소값:";

            // 
            // txtMin
            // 
            this.txtMin.Location = new System.Drawing.Point(80, 47);
            this.txtMin.Name = "txtMin";
            this.txtMin.Size = new System.Drawing.Size(180, 21);
            this.txtMin.TabIndex = 2;
            this.txtMin.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtMin_KeyPress);

            // 
            // lblMax
            // 
            this.lblMax.AutoSize = true;
            this.lblMax.Location = new System.Drawing.Point(20, 90);
            this.lblMax.Name = "lblMax";
            this.lblMax.Size = new System.Drawing.Size(53, 12);
            this.lblMax.TabIndex = 3;
            this.lblMax.Text = "최대값:";

            // 
            // txtMax
            // 
            this.txtMax.Location = new System.Drawing.Point(80, 87);
            this.txtMax.Name = "txtMax";
            this.txtMax.Size = new System.Drawing.Size(180, 21);
            this.txtMax.TabIndex = 4;
            this.txtMax.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtMin_KeyPress);

            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(80, 130);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 30);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "확인";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);

            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(180, 130);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "취소";
            this.btnCancel.UseVisualStyleBackColor = true;

            // 
            // FilterRangeDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(284, 181);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtMax);
            this.Controls.Add(this.lblMax);
            this.Controls.Add(this.txtMin);
            this.Controls.Add(this.lblMin);
            this.Controls.Add(this.lblDescription);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FilterRangeDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "필터 범위 설정";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}