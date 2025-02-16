using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static IGIEditor.QUtils;

namespace IGIEditor
{
    class QTask
    {
        internal static List<QScriptTask> GetQTaskList(bool fullQtaskList = false, bool distinct = false, bool fromBackup = false)
        {
            int level = QMemory.GetRunningLevel();
            if(level <= 0)
            {
                throw new Exception("Invalid selection game is not running");
            }

            string inputQscPath = cfgQscPath + level + "\\" + objectsQsc;
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "called with level : " + level + " fullList : " + fullQtaskList.ToString() + " distinct : " + distinct.ToString() + " backup : " + fromBackup);
            string qscData = fromBackup ? LoadFile(inputQscPath) : LoadFile();

            var qtaskList = fullQtaskList ? QObjects.ParseAllOjects(qscData) : QObjects.ParseObjects(qscData);
            if (distinct)
                qtaskList = qtaskList.GroupBy(p => p.model).Select(g => g.First()).ToList();
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "returned list count: " + qtaskList.Count);
            return qtaskList;
        }

        internal static List<QScriptTask> GetQTaskList(int level, bool fullQtaskList = false, bool distinct = false, bool fromBackup = false)
        {
            string inputQscPath = cfgQscPath + level + "\\" + objectsQsc;
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, " Qsc Path: '" + inputQscPath + "' level : " + level + " full List : " + fullQtaskList.ToString() + " distinct : " + distinct.ToString() + " backup : " + fromBackup);
            string qscData = fromBackup ? LoadFile(inputQscPath) : LoadFile();

            bool isBinary = qscData.HasBinaryContent();
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "isBinary: " + isBinary);

            var qtaskList = fullQtaskList ? QObjects.ParseAllOjects(qscData) : QObjects.ParseObjects(qscData);
            if (qtaskList.Count > 0)
                if (distinct) qtaskList = qtaskList.GroupBy(p => p.model).Select(g => g.First()).ToList();
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "task List count " + qtaskList.Count);
            return qtaskList;
        }

        internal static string GetUniqueQTaskId(string taskId)
        {
            int uniqueTaskId = 0;
            try
            {
                uniqueTaskId = Convert.ToInt32(taskId);
                uniqueTaskId = GetUniqueQTaskId(uniqueTaskId);

            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return uniqueTaskId.ToString();
        }

        internal static int GetUniqueQTaskId(int taskId)
        {
            //Generating New Id for Duplicate QTaskId.
            if (QUtils.qIdsList.Contains(taskId))
            {
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Duplicate TaskId: " + taskId + " Generating new TaskId");
                while (QUtils.qIdsList.Contains(taskId++)) ;
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Generated New TaskId: " + taskId);
            }
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Returned New TaskId: " + taskId);
            return taskId;
        }

        internal static int GenerateTaskID(bool minimalId = false, bool fromBackup = false)
        {
            try
            {
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Method called with parameters: minimalId=" + minimalId + ", fromBackup=" + fromBackup + ", Level=" + QUtils.gGameLevel);

                string inputQscPath = cfgQscPath + QUtils.gGameLevel + "\\" + objectsQsc;
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "inputQscPath: " + inputQscPath);

                string qscData = fromBackup ? LoadFile(inputQscPath) : LoadFile();
                qscData = qscData.Replace("\t", String.Empty);
                string[] qscDataSplit = qscData.Split('\n');

                List<int> qIdsList = new List<int>();
                foreach (var data in qscDataSplit)
                {
                    if (data.Contains(taskNew))
                    {
                        string[] taskNew = data.Split(',');
                        int qid = GetTaskIdFromData(taskNew);

                        if (qid > QUtils.LEVEL_FLOW_TASK_ID)
                            qIdsList.Add(qid);
                    }
                }

                qIdsList.Sort();
                int taskId = GetNextTaskId(qIdsList, minimalId);

                taskId = GetUniqueQTaskId(taskId);
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Unique task ID generated. Task ID: " + taskId);

                return taskId;
            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
                return 0;
            }
        }

        private static int GetTaskIdFromData(string[] taskNew)
        {
            int qid = -1;
            foreach (var task in taskNew)
            {
                int taskIndex = Array.IndexOf(taskNew, task);
                if (taskIndex == (int)QTASKINFO.QTASK_ID)
                {
                    var taskIdx = task.IndexOf('(');
                    if (taskIdx != -1 && int.TryParse(task.Substring(taskIdx + 1), out qid) && qid != -1)
                    {
                        break;
                    }
                }
            }
            return qid;
        }

        private static int GetNextTaskId(List<int> qIdsList, bool minimalId)
        {
            if (minimalId)
            {
                for (int index = 0; index < qIdsList.Count - 1; index++)
                {
                    int minVal = qIdsList[index];
                    int maxVal = qIdsList[index + 1];
                    int diffVal = Math.Abs(maxVal - minVal);

                    if (diffVal >= QUtils.MAX_MINIMAL_ID_DIFF)
                    {
                        return minVal + 1;
                    }
                }
            }
            else
            {
                return qIdsList[qIdsList.Count - 1] + 1;
            }

            return qIdsList[0] + 1;
        }




        internal static QScriptTask GetQTask(string taskName)
        {
            AddLog(MethodBase.GetCurrentMethod().Name, " taskName called");
            var qtaskList = GetQTaskList();

            foreach (var qtask in qtaskList)
            {
                if (qtask.model.Contains(taskName))
                {
                    AddLog(MethodBase.GetCurrentMethod().Name, "returned value for Model " + taskName);
                    return qtask;
                }
            }
            AddLog(MethodBase.GetCurrentMethod().Name, "returned : null");
            return null;
        }

        internal static QScriptTask GetQTask(int taskId)
        {
            AddLog(MethodBase.GetCurrentMethod().Name, " taskId called");
            var qtaskList = GetQTaskList();
            foreach (var qtask in qtaskList)
            {
                if (qtask.id == taskId)
                {
                    AddLog(MethodBase.GetCurrentMethod().Name, "returned value for Task_Id " + taskId);
                    return qtask;
                }
            }
            AddLog(MethodBase.GetCurrentMethod().Name, "returned : null");
            return null;
        }

    }
}
