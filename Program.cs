using System;
using System.Windows.Forms;

namespace CsvTimeSeriesViewer
{
    static class Program
    {
        /// <summary>
        /// �ش� ���ø����̼��� �� �������Դϴ�.
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
                MessageBox.Show($"���ø����̼� ���� ����:\n{ex.Message}\n\n{ex.StackTrace}",
                    "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}