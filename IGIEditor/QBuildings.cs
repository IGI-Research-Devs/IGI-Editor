using System;
using System.Linq;
using System.Reflection;

namespace IGIEditor
{
    class QBuildings
    {

        internal static string AddBuilding(string model, Real64 position, bool checkModel = false, int taskId = -1, string taskNote = "")
        {
            string modelName = String.IsNullOrEmpty(taskNote) ? QObjects.FindModelName(model) : taskNote;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called checkModel : " + checkModel.ToString() + "  Building : " + modelName + "\"\tX : " + position.x + " Y : " + position.y + " Z : " + position.z + " Model : " + model + "\n");

            if (checkModel)
            {
                bool modelExist = QObjects.CheckModelExist(model);

                if (!modelExist)
                {
                    QLog.ShowError("Model " + model + " does not exist in current level");
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Model " + model + " does not exist in current level");
                    return null;
                }
            }

            //int taskId = QUtils.GenerateTaskID(true);
            return Building(taskId, modelName, position.x, position.y, position.z, 0, 0, 0, model);
        }

        internal static string AddBuilding(string modelId, Real64 position, Real32 orientation, bool checkModel = false, int taskId = -1, string taskNote = "")
        {
            string modelName = QObjects.FindModelName(modelId);
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called checkModel : " + checkModel.ToString() + "  Building : '" + modelName + "'\tX : " + position.x + " Y : " + position.y + " Z : " + position.z + "\tBeta : " + orientation.beta + " Gamma : " + orientation.gamma + ",Alpha : " + orientation.alpha + " Model : " + modelId + "\n");

            if (checkModel)
            {
                bool modelExist = QObjects.CheckModelExist(modelId);

                if (!modelExist)
                {
                    QLog.ShowError("Model " + modelId + " does not exist in current level");
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Model " + modelId + " does not exist in current level");
                    return null;
                }
            }
            //int taskId = QUtils.GenerateTaskID(true);
            return Building(taskId, modelName, position.x, position.y, position.z, orientation.alpha, orientation.beta, orientation.gamma, modelId);
        }

        internal static string Building(int taskId = -1, string taskNote = "", double x = 0.0f, double y = 0.0f, double z = 0.0f, float alpha = 0.0f, float beta = 0.0f, float gamma = 0.0f, string modelId = "")
        {
            if (modelId.Contains("\"")) modelId = modelId.Replace("\"", String.Empty);

            string qtaskBuilding = "Task_New(" + taskId + ",\"Building\",\"" + taskNote + "\"," + x + "," + y + "," + z + "," + alpha + "," + beta + "," + gamma + ",\"" + modelId + "\");" + "\n";
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with ID : " + QUtils.qtaskObjId + "  Building : '" + QObjects.GetModelName(modelId) + "'\tX : " + x + " Y : " + y + " Z : " + z + "\t Alpha : " + alpha + " Beta : " + beta + ",Gamma : " + gamma + " Model : " + modelId + "\n");
            return qtaskBuilding;
        }


        string RemoveBuilding(int qid)
        {
            string qscData = QUtils.LoadFile();
            //Remove all whitespaces.
            qscData = qscData.Replace("\t", String.Empty);
            bool isRecursive = false;//Because Task_ID is unique.
            return RemoveBuilding(qscData, qid, null, (int)QTASKINFO.QTASK_ID, isRecursive);
        }

        string RemoveBuilding(string model)
        {
            string qscData = QUtils.LoadFile();
            //Remove all whitespaces.
            qscData = qscData.Replace("\t", String.Empty);
            if (!String.IsNullOrEmpty(model))
                model = "\"" + model + "\"";
            bool isRecursive = QUtils.GetModelCount(model) > 1;

            return RemoveBuilding(qscData, -9999, model, (int)QTASKINFO.QTASK_MODEL, isRecursive);
        }



        string RemoveBuilding(string qscData, int qid, string model, int updateType, bool isRecursive)
        {

            string[] qscDataSplit = qscData.Split('\n');

            foreach (var data in qscDataSplit)
            {
                if (data.Contains(QUtils.taskNew))
                {
                    var startIndex = data.IndexOf(',', 14) + 1;
                    var taskName = (data.Slice(13, startIndex));

                    string[] taskNew = data.Split(',');
                    int taskIndex = 0;
                    if (QUtils.objTypeList.Any(o => o.Contains(taskName)))
                    {
                        foreach (var task in taskNew)
                        {
                            if (taskIndex == (int)QTASKINFO.QTASK_ID || taskIndex == (int)QTASKINFO.QTASK_MODEL)
                            {
                                int taskId = -3;
                                string qmodel = null;
                                if (updateType == (int)QTASKINFO.QTASK_ID)
                                {
                                    string taskIdStr = taskNew[(int)QTASKINFO.QTASK_ID].Remove(0, QUtils.taskNew.Length + 1).Replace("\"", String.Empty);
                                    taskId = Convert.ToInt32(taskIdStr);
                                }

                                if (updateType == (int)QTASKINFO.QTASK_MODEL)
                                {
                                    string qmodelStr = taskNew[(int)QTASKINFO.QTASK_MODEL];
                                    qmodel = taskNew[(int)QTASKINFO.QTASK_MODEL].Slice(0, qmodelStr.IndexOf('"', 3) + 1).Trim();
                                }

                                if (qid == taskId || model == qmodel && (!(String.IsNullOrEmpty(model))))
                                {
                                    var qidStr = taskNew[(int)QTASKINFO.QTASK_ID].Substring(task.IndexOf('(') + 1);
                                    var id = Convert.ToInt32(qidStr);
                                    string idModelIndex = "";

                                    if (qid == taskId)
                                        idModelIndex = "Task_New(" + id;
                                    else
                                        idModelIndex = model;

                                    int qtaskStartIndex = qscData.IndexOf(idModelIndex);
                                    int newLineIndex = qscData.IndexOf('\n', qtaskStartIndex) + 1;
                                    int newLineIndexLen = newLineIndex - qtaskStartIndex;

                                    int qtaskEndIndex = qscData.IndexOf(')', qtaskStartIndex, newLineIndexLen);
                                    int nestedCount = 0;

                                    if (qtaskEndIndex != -1)
                                    {
                                        qtaskEndIndex = newLineIndex;
                                    }

                                    else
                                    {
                                        //Remove nested objects.
                                        while (qtaskEndIndex == -1)
                                        {
                                            newLineIndex = qscData.IndexOf('\n', newLineIndex + 2);
                                            qtaskStartIndex = qscData.IndexOf("Task_New(", newLineIndex);
                                            newLineIndexLen = newLineIndex - qtaskStartIndex;

                                            var nestedStart = qscData.IndexOf('(', newLineIndex, newLineIndexLen);
                                            var nestedEnd = qscData.IndexOf(')', newLineIndex, newLineIndexLen);

                                            if (nestedStart != -1 && nestedEnd != -1 && qscData[nestedEnd + 1] != ',')
                                                nestedCount++;
                                            else
                                            {
                                                qtaskEndIndex = newLineIndex;
                                            }

                                        }
                                    }

                                    qscData = qscData.Remove(qtaskStartIndex, qtaskEndIndex - qtaskStartIndex).Trim();

                                    if (isRecursive)
                                    {
                                        QUtils.SaveFile(qscData);
                                        return RemoveBuilding(qscData, qid, model, updateType, isRecursive);
                                    }
                                    else
                                        return qscData;
                                }
                            }
                            taskIndex++;
                        }
                    }
                }
            }
            return qscData;
        }

    }
}
