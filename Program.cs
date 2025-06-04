using System;
using System.Windows.Forms;

namespace CsvTimeSeriesViewer
{
    static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"애플리케이션 시작 오류:\n{ex.Message}\n\n{ex.StackTrace}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}