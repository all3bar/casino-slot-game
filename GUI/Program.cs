using System;
using System.Windows.Forms;

namespace CasinoSlot.GUI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                MessageBox.Show($"심각한 오류가 발생했습니다:\n{e.ExceptionObject}", "실행 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show($"UI 오류가 발생했습니다:\n{e.Exception.Message}\n{e.Exception.StackTrace}", "UI 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
