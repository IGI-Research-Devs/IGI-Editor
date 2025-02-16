using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace IGIEditor
{
    static class QClass
    {
        [STAThread]
        static void Main()
        {
            try
            {
                bool instanceCount = false;
                Mutex mutex = null;
                var projAppName = AppDomain.CurrentDomain.FriendlyName;
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnAppExit);

                mutex = new Mutex(true, projAppName, out instanceCount);
                if (instanceCount)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new IGIEditorUI());
                    mutex.ReleaseMutex();
                }
                else
                {
                    QLog.ShowError("IGI Editor is already running");
                }
            }
            catch (Exception ex)
            {
                QLog.ShowLogException("EditorMain", ex);
            }
        }

        private static void OnAppExit(object sender, EventArgs e)
        {
            //Update config on exit.
            if (QUtils.gamePathSet) QUtils.CreateConfig();

            //Move Logs and data to cache.
            if (File.Exists(QUtils.editorLogFile))
            {
                QUtils.FileIOCopy(QUtils.editorLogFile, QUtils.cachePathAppLogs + "\\"  + QUtils.editorLogFile);
                QUtils.FileIOMove(QUtils.qLibLogsFile, QUtils.cachePathAppLogs + "\\" + QUtils.qLibLogsFile);
            }
            QUtils.FileIOMove(QUtils.internalsLogPath, QUtils.cachePathAppLogs + "\\" + QUtils.internalsLogPath);

            //Cleanup A.I script files.
            QUtils.CleanUpAiFiles();

            //Detach internals on Exit.
            QUtils.DetachInternals();
        }
    }
}
