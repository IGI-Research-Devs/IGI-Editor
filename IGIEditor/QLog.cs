using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace IGIEditor
{
    public enum DEBUG_TYPE
    {
        Debug,
        Error,
        Warning,
        Info
    }

    public static class QLog
    {
		internal static void AddLog(string methodName, string logMsg, DEBUG_TYPE type = DEBUG_TYPE.Debug)
		{
			// Check that logging is enabled and a valid log file is specified in QUtils
			if (QUtils.logEnabled && !string.IsNullOrWhiteSpace(QUtils.editorLogFile))
			{
			methodName = methodName.Replace("Btn_Click", string.Empty)
						   .Replace("_SelectedIndexChanged", string.Empty)
						   .Replace("_SelectedValueChanged", string.Empty);
			File.AppendAllText(QUtils.editorLogFile, "[" 
				+ DateTime.Now.ToString("yyyy-MM-dd - HH:mm:ss") + "] [" 
				+ type.ToString().ToUpper() + "] " 
				+ methodName + "(): " + logMsg + "\n");
			}
		}

        //UI-Dialogs and MessageBox.
        internal static void ShowWarning(string warnMsg, string caption = "WARNING")
        {
            DialogMsgBox.ShowBox(caption, warnMsg, MsgBoxButtons.Ok);
        }

        internal static void ShowError(string errMsg, string caption = "ERROR")
        {
            DialogMsgBox.ShowBox(caption, errMsg, MsgBoxButtons.Ok);
        }

        private static string FormatException(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Exception Details:");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"Source: {ex.Source}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                sb.AppendLine("Inner Exception:");
                sb.AppendLine($"Message: {ex.InnerException.Message}");
                sb.AppendLine($"StackTrace: {ex.InnerException.StackTrace}");
            }
            
            return sb.ToString();
        }

        internal static void LogException(string methodName, Exception ex)
        {
            methodName = methodName.Replace("Btn_Click", String.Empty)
                                 .Replace("_SelectedIndexChanged", String.Empty)
                                 .Replace("_SelectedValueChanged", String.Empty);
            
            string formattedError = FormatException(ex);
            AddLog(methodName, formattedError, DEBUG_TYPE.Error);
        }

        internal static void ShowException(string methodName, Exception ex)
        {
            string formattedError = FormatException(ex);
            ShowError(formattedError, $"{methodName} Exception - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        internal static void ShowLogException(string methodName, Exception ex)
        {
            methodName = methodName.Replace("Btn_Click", String.Empty).Replace("_SelectedIndexChanged", String.Empty).Replace("_SelectedValueChanged", String.Empty);
            //Show and Log exception for method name.
            ShowException(methodName, ex);
            LogException(methodName, ex);
        }

        internal static void ShowLogError(string methodName, string errMsg, string caption = "ERROR")
        {
            methodName = methodName.Replace("Btn_Click", String.Empty).Replace("_SelectedIndexChanged", String.Empty).Replace("_SelectedValueChanged", String.Empty);
            //Show and Log error for method name.
            ShowError(methodName + "(): " + errMsg, caption);
            QLog.AddLog(methodName, errMsg, DEBUG_TYPE.Error);
        }

        internal static void ShowLogStatus(string methodName, string logMsg)
        {
            IGIEditorUI.editorRef.SetStatusText(logMsg);
            QLog.AddLog(methodName, logMsg, DEBUG_TYPE.Info);
        }

        internal static void ShowLogInfo(string methodName, string logMsg)
        {
            ShowInfo(logMsg);
            QLog.AddLog(methodName, logMsg, DEBUG_TYPE.Info);
        }

        internal static void ShowLogWarning(string methodName, string logMsg)
        {
            ShowWarning(logMsg);
            QLog.AddLog(methodName, logMsg, DEBUG_TYPE.Warning);
        }

        internal static void ShowInfo(string infoMsg, string caption = "INFO")
        {
            DialogMsgBox.ShowBox(caption, infoMsg, MsgBoxButtons.Ok);
        }

        internal static DialogResult ShowDialog(string infoMsg, string caption = "INFO")
        {
            return DialogMsgBox.ShowBox(caption, infoMsg, MsgBoxButtons.YesNo);
        }

        internal static void ShowConfigError(string keyword)
        {
            ShowError("Config has invalid property for '" + keyword + "'", QUtils.CAPTION_CONFIG_ERR);
        }

        internal static void ShowSystemFatalError(string errMsg)
        {
            ShowError(errMsg, QUtils.CAPTION_FATAL_SYS_ERR);
            Environment.Exit(1);
        }

        internal static bool ShowEditModeDialog()
        {
            var editorDlg = ShowDialog("Edit Mode not enabled to edit the level\nDo you want to enable Edit mode now ?", QUtils.EDITOR_LEVEL_ERR);
            if (editorDlg == DialogResult.Yes)
                return true;
            return false;
        }
    }
}
