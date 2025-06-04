using System;
using System.Windows.Forms;

namespace CsvTimeSeriesViewer
{
    /// <summary>
    /// 필터 범위 설정 다이얼로그
    /// </summary>
    public partial class FilterRangeDialog : Form
    {
        private DataFilter filter;

        public FilterRangeDialog(DataFilter dataFilter)
        {
            filter = dataFilter;
            InitializeComponent();
            LoadFilterValues();
        }

        private void LoadFilterValues()
        {
            txtMin.Text = filter.MinValue == double.MinValue ? "" : filter.MinValue.ToString();
            txtMax.Text = filter.MaxValue == double.MaxValue ? "" : filter.MaxValue.ToString();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 최소값 파싱
            if (string.IsNullOrWhiteSpace(txtMin.Text))
            {
                filter.MinValue = double.MinValue;
            }
            else if (double.TryParse(txtMin.Text, out double min))
            {
                filter.MinValue = min;
            }
            else
            {
                MessageBox.Show("최소값이 올바른 숫자 형식이 아닙니다.", "입력 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMin.Focus();
                txtMin.SelectAll();
                this.DialogResult = DialogResult.None;
                return;
            }

            // 최대값 파싱
            if (string.IsNullOrWhiteSpace(txtMax.Text))
            {
                filter.MaxValue = double.MaxValue;
            }
            else if (double.TryParse(txtMax.Text, out double max))
            {
                filter.MaxValue = max;
            }
            else
            {
                MessageBox.Show("최대값이 올바른 숫자 형식이 아닙니다.", "입력 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMax.Focus();
                txtMax.SelectAll();
                this.DialogResult = DialogResult.None;
                return;
            }

            // 범위 유효성 검사
            if (filter.MinValue > filter.MaxValue)
            {
                MessageBox.Show("최소값이 최대값보다 클 수 없습니다.", "범위 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            this.DialogResult = DialogResult.OK;
        }

        private void TxtMin_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 숫자, 소수점, 백스페이스, 음수 기호만 허용
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                e.KeyChar != '.' && e.KeyChar != '-')
            {
                e.Handled = true;
            }

            // 소수점은 하나만 허용
            if (e.KeyChar == '.' && (sender as TextBox).Text.Contains("."))
            {
                e.Handled = true;
            }

            // 음수 기호는 맨 앞에만 허용
            if (e.KeyChar == '-' && ((sender as TextBox).SelectionStart != 0 ||
                (sender as TextBox).Text.Contains("-")))
            {
                e.Handled = true;
            }
        }
    }
}