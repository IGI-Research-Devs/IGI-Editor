﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IGIEditor
{

    public class HumanAi
    {
        public int aiCount { get; set; }
        public string aiType { get; set; }
        public int graphId { get; set; }
        public string weapon { get; set; }
        public string model { get; set; }
        public bool guardGenerator { get; set; }
        public int maxSpawns { get; set; }
        public bool friendly { get; set; }
        public bool invulnerability { get; set; }
        public bool advanceView { get; set; }
    }

    class AIModel
    {
        string modelName;
        string modelId;
        char option;
        List<int> levels;

        public string ModelName { get => modelName; set => modelName = value; }
        public string ModelId { get => modelId; set => modelId = value; }
        public char Option { get => option; set => option = value; }
        public List<int> Levels { get => levels; set => levels = value; }

        public AIModel Add(string modelName, string modelId, char option, List<int> levels)
        {
            this.ModelName = modelName;
            this.ModelId = modelId;
            this.Option = option;
            this.Levels = levels;
            return this;
        }
    }

    class QAI
    {

        private static List<AIModel> aiModelList = new List<AIModel>();

        internal static string AddHumanSoldier(string aiType, int aiScriptId, int graphId, Real64 position, float angle, string model, int team, bool addWeapon, string weapon, int ammo, bool guardGenerator)
        {
            if (QUtils.qtaskId == 0)
            {
                QUtils.qtaskId = QUtils.GenerateTaskID(true);
            }
            else
            {
                QUtils.qtaskId++;
            }

            if (position == null)
            {
                position = QHuman.GetHumanTaskList().qtask.position;
            }
            int boneHeirarchy = 1;
            if (model == "015_01_1" || model == "012_01_1")//HumanSoldierFemale.
                boneHeirarchy = GetBoneHeirarchy(model);

            return AddHumanSoldier(QUtils.qtaskId, "A.I", aiType, aiScriptId, graphId, position, angle, model, team, boneHeirarchy, -1, addWeapon, weapon, ammo, guardGenerator);
        }

        internal static string AddHumanSoldier(int taskId, string taskNote, string aiType, int aiScriptId, int graphId, Real64 position, float angle, string model, int team, int boneHeirachy, int standAnimation, bool addWeapon, string weapon, int ammo, bool guardGenerator)
        {
            //Add the A.I (Human soldier)
            string humanSoldierType = (model == "015_01_1" || model == "012_01_1") ? "HumanSoldierFemale" : "HumanSoldier";
            string qtaskSoldier = "\nTask_New(" + taskId + ",\"" + humanSoldierType + "\",\"" + taskNote + "\"," + position.x + "," + position.y + "," + position.z + "," + angle + ",\"" + model + "\"," + team + "," + boneHeirachy + "," + standAnimation + ",\n";
            QUtils.AddLog("AddHumanSoldier() called with Ai Type: '" + aiType + "' ID : " + taskId + "  HumanSoldier : " + taskNote + "\"," + position.x + "," + position.y + "," + position.z + "," + angle + ",\"" + model + "\"," + team + "," + boneHeirachy + "," + standAnimation + ",\n");

            //Add A.I type to status message.
            if (team == 0) QUtils.aiFriendTask += humanSoldierType + "_" + taskId + ".isDead && ";

            else QUtils.aiEnenmyTask += humanSoldierType + "_" + taskId + ".isDead && ";

            //Add the weapon.
            if (addWeapon)
                qtaskSoldier += QHuman.Weapon(weapon, ammo);

            //Add AI's script and graph data.
            qtaskSoldier += "Task_New(" + aiScriptId + ",\"HumanAI\",\"" + taskNote + "\",\"" + aiType + "\"," + graphId;
            qtaskSoldier += (!guardGenerator) ? "));" : ")));";
            return qtaskSoldier;
        }

        internal static string GuardGenerator(string taskNote = "AI Army", int maxSpawn = 10)
        {
            string qTaskGuardGen = "Task_New(-1, \"GuardGenerator\",\"" + taskNote + "\"," + "\"!HumanPlayer_0.isDead\"," + maxSpawn + ",";
            return qTaskGuardGen;
        }

        internal static string AddHumanSoldier(HumanAi humanAi, bool guardGenerator = false, int maxSpawns = 10, bool invulnerability = false, bool advanceView = false)
        {
            string aiType = null, aiWeapon = null, graphId = null, aiId = null, patrolId = null, modelId = null;
            int aiCount = 1, teamId = 0, aiAmmo = 999;
            string qscData = null;

            if (humanAi != null) aiCount = humanAi.aiCount;

            for (int i = 1; i <= aiCount; i++)
            {
                if (humanAi != null)
                {
                    aiType = humanAi.aiType;
                    graphId = humanAi.graphId.ToString();
                }

                aiId = QUtils.aiScriptId.ToString();
                bool aiIdExist = QGraphs.CheckIdExist(aiId, "AI", QUtils.gGameLevel, "AI Id " + aiId + " already exist for current level");

                patrolId = (QUtils.aiScriptId + 1).ToString();
                bool patrolIdExist = QGraphs.CheckIdExist(patrolId, "Patrol", QUtils.gGameLevel, "PatrolId " + patrolId + " already exist for current level");
                bool graphIdExist = true;//QGraphs.CheckIdExist(graphId, "Graph", QUtils.gGameLevel, "GraphId " + graphId + " doesn't exist for current level");

                if (!patrolIdExist && !aiIdExist && graphIdExist)
                {
                    int aiIdI = Convert.ToInt32(aiId);
                    int patrolIdI = Convert.ToInt32(patrolId);
                    int graphIdI = Convert.ToInt32(graphId);

                    Real64 aiPos = QGraphs.GetGraphPosition(graphId);
                    float aiAngle = QMemory.GetRealAngle();
                    aiPos.x += new Random().Next(1000, 100000);
                    aiPos.y += new Random().Next(1000, 100000);

                    if (humanAi != null)
                    {
                        modelId = humanAi.model;
                        aiWeapon = humanAi.weapon;
                        teamId = humanAi.friendly ? 0 : 1;
                        aiAmmo = 999;

                        //Remove after - TEST.
                        //bool modelExist = QUtils.CheckModelExist(modelId);

                        //if (!modelExist)
                        //{
                        //    QUtils.ShowError("Model '" + modelId + "' doesnt exist for current level");
                        //    return qscData;
                        //}
                    }

                    //Add GuardGenerator .
                    if (guardGenerator)
                        qscData += QAI.GuardGenerator("AI Army", maxSpawns);

                    //Add A.I HumanSoldier.
                    qscData += AddHumanSoldier(aiType, aiIdI, graphIdI, aiPos, aiAngle, modelId, teamId, true, aiWeapon, aiAmmo, guardGenerator);

                    //Add A.I Script to HumanSoldier.
                    var aiScriptData = AddAIScriptAndPath(aiType, graphId, aiId, patrolId, QUtils.gGameLevel, invulnerability, advanceView);
                    if (!String.IsNullOrEmpty(aiScriptData))
                        qscData += aiScriptData;
                }
                QUtils.aiScriptId += 3;
            }

            return qscData;
        }

        internal static string AddAIScriptAndPath(string aiType, string graphId, string aiId, string patrolId, int level, bool invulnerability = false, bool advanceView = false)
        {
            var inputAiPath = QUtils.cfgAiPath;
            string result = null;
            string patrolAlarmId = null;

            string[] fileArray = System.IO.Directory.GetFiles(inputAiPath);
            var aiTypeSplit = aiType.Split('_')[1].ToLower();

            foreach (var file in fileArray)
            {
                if (file.Contains(aiTypeSplit))
                {
                    if (file.Contains("script"))
                    {
                        string aiScriptData;
                        //If Custom A.I selected.
                        if (QUtils.customAiSelected)
                            aiScriptData = QUtils.LoadFile(QUtils.appdataPath + "\\" + QUtils.customScriptFileQEd);
                        else
                            aiScriptData = QCryptor.Decrypt(file);

                        //Add Idle patrol.
                        if (aiScriptData.Contains(QUtils.patroIdleMask))
                        {
                            aiScriptData = aiScriptData.ReplaceFirst(QUtils.patroIdleMask, patrolId);
                        }

                        //Add Alarm patrol.
                        if (aiScriptData.Contains(QUtils.patroAlarmMask))
                        {
                            aiScriptData = aiScriptData.ReplaceFirst(QUtils.patroAlarmMask, patrolAlarmId);
                        }

                        //Add Alarm control Id.
                        if (aiScriptData.Contains("(" + QUtils.alarmControlMask + ")"))
                        {
                            var aiPos = QGraphs.GetGraphPosition(graphId);
                            int alarmControlId = 0;
                            alarmControlId = QAI.GetNearestDynamicId(aiPos, QUtils.alarmControl);
                            if (alarmControlId == 0)
                                QUtils.ShowWarning("Couldn't find nearest alarm Id for AI : " + aiId + " on Graph : " + graphId);
                            aiScriptData = aiScriptData.ReplaceFirst(QUtils.alarmControlMask, alarmControlId.ToString());
                        }


                        //Add Gunner Id.
                        if (aiScriptData.Contains("(" + QUtils.gunnerIdMask + ")"))
                        {
                            var aiPos = QGraphs.GetGraphPosition(graphId);
                            int gunnerId = 0;
                            gunnerId = QAI.GetNearestDynamicId(aiPos, QUtils.stationaryGun);
                            if (gunnerId == 0)
                                QUtils.ShowWarning("Couldn't find nearest Gunner Id for AI : " + aiId + " on Graph : " + graphId);
                            aiScriptData = aiScriptData.ReplaceFirst(QUtils.gunnerIdMask, gunnerId.ToString()).ReplaceFirst(QUtils.viewGammaMask, "180");//Set View Gamma to 180.
                        }

                        //Add invulnerability if opted for.
                        if (invulnerability)
                        {
                            string invulnerabilityMode = "AIFunction_DefaultHandler();\n" +
                                "AIFunction_SetEventPriority(AIEVENT_COMBAT);\n" +
                                "AIFunction_SetInstantDeath(FALSE);\n" +
                                "AIFunction_SetInvulnerability(TRUE);\n";
                            aiScriptData = aiScriptData.ReplaceFirst("AIFunction_DefaultHandler();", invulnerabilityMode);
                        }

                        //Add advance View if opted for.
                        if (advanceView)
                        {
                            string advanceViewLengthMode = "AIFunction_DefaultHandler();\n" +
                                "AIFunction_SetViewGamma(90000);\n" +
                                " AIFunction_SetSecondaryViewGamma(90000);\n" +
                                "AIFunction_SetSecondaryViewAlpha(90000);\n" +
                                "AIFunction_SetViewLength(90000);\n" +
                                "AIFunction_SetSecondaryViewLength(90000);\n";
                            aiScriptData = aiScriptData.ReplaceFirst("AIFunction_DefaultHandler();", advanceViewLengthMode);
                        }


                        string aiFileName = aiId + ".qsc";
                        QUtils.aiScriptFiles.Add(aiId + ".qvm");
                        var outputAiPath = QUtils.cfgGamePath + level + @"\ai\";

                        QUtils.SaveFile(aiFileName, aiScriptData);
                        QCompiler.Compile(aiFileName, outputAiPath, 0x0);
                        System.IO.File.Delete(aiFileName);
                    }

                    else if (file.Contains("path"))
                    {
                        string aiPathData;
                        //If Custom A.I selected.
                        if (QUtils.customAiSelected)
                            aiPathData = QUtils.LoadFile(QUtils.appdataPath + "\\" + QUtils.customAiPathFileQEd);
                        else
                            aiPathData = QCryptor.Decrypt(file);

                        bool graphExist = false;
                        //var nodesList = QGraphs.GetAllNodes4mGraph(Convert.ToInt32(graphId));
                        var nodesList = QGraphs.GetNodesForGraph(Convert.ToInt32(graphId));

                        if (nodesList == null)
                        {
                            var qTaskGraphList = QGraphs.GetQTaskGraphList(true, true, level);

                            foreach (var qTaskGraph in qTaskGraphList)
                            {
                                if (qTaskGraph.id.ToString() == graphId)
                                {
                                    graphExist = true;
                                    break;
                                }
                            }

                            if (graphExist)
                            {
                                QUtils.AddLog("AddAIScript() AI Patrol Updated to static for aiId : " + aiId + "\tgraphId : " + graphId);
                                return AddAIScriptAndPath("AITYPE_STATIC", graphId, aiId, patrolId, level);
                            }
                            else
                            {
                                QUtils.ShowError("Invalid GraphId " + graphId + "provided.");
                                return null;
                            }
                        }

                        if (aiPathData.Contains("xxxx"))
                        {
                            aiPathData = aiPathData.Replace("xxxx", patrolId);
                            aiPathData = aiPathData.Replace(")),", "));");
                            result = aiPathData;

                            //Add Alarm path to selected A.I.
                            if (file.Contains("idle"))
                            {
                                QUtils.aiScriptId++;
                                patrolAlarmId = (QUtils.aiScriptId + 1).ToString();
                                string alarmPathFile = file.Replace("idle", "alarm");

                                string aiAlarmPathData = QCryptor.Decrypt(alarmPathFile);

                                aiAlarmPathData = aiAlarmPathData.Replace("xxxx", patrolAlarmId);
                                aiAlarmPathData = aiAlarmPathData.Replace(")),", "));");
                                result = aiPathData + "\n" + aiAlarmPathData;
                            }
                        }

                        int index = 0;
                        if (nodesList.Count <= 2 && !aiType.Contains("AITYPE_SECURITY_PATROL"))
                        {
                            QUtils.AddLog("AddAIScript() AI Patrol Updated to security for aiId : " + aiId + "\tgraphId : " + graphId);
                            result = AddAIScriptAndPath("AITYPE_SECURITY_PATROL", graphId, aiId, patrolId, level);
                        }
                        else if (aiType.Contains("AITYPE_SECURITY_PATROL"))
                        {
                            for (char c = 'x'; c <= 'y'; c++)
                            {
                                int randIndex = new Random().Next(0, nodesList.Count - 1);
                                string pattern = @"\b" + c + @"\b";
                                string replace = nodesList[index++].ToString();
                                result = Regex.Replace(result, pattern, replace);
                            }
                        }

                        else
                        {
                            var nIdsList = new List<char>() { 'a', 'c', 'b', 'd','x','y','z'};
                            foreach (var nId in nIdsList)
                            {
                                int randIndex = new Random().Next(0, nodesList.Count - 1);
                                if (nodesList.Count >= 20) index = randIndex;
                                if (index >= nodesList.Count) break;
                                string pattern = @"\b" + nId + @"\b";
                                string replace = nodesList[index++].ToString();
                                result = Regex.Replace(result, pattern, replace);
                            }
                        }
                    }
                }
            }

            return "\n" + result + "\n";
        }


        internal static string AddAiTaskDetection(string qscData)
        {
            //Add A.I detection.
            string statusMsg = null;
            statusMsg += "\nTask_New(-1,\"Container\", \"StatusMessages\"," + "\n";

            if (!String.IsNullOrEmpty(QUtils.aiFriendTask))
            {
                var varStringFriendly = QUtils.aiFriendTask.ReplaceLast("&&", string.Empty).Trim();

                var varStringSplit = QUtils.aiFriendTask.Replace("&&", "#").Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var varString in varStringSplit)
                {
                    statusMsg += QUtils.AddStatusMsg(-1, "Friendly man down! Watch out", varString);
                }

                QUtils.anyaTeamTaskId = QUtils.GenerateTaskID();
                statusMsg += QUtils.AddStatusMsg(QUtils.anyaTeamTaskId, "Anya team down. Mission Failed", varStringFriendly);
                QUtils.aiFriendTask = null;
            }

            if (!String.IsNullOrEmpty(QUtils.aiEnenmyTask))
            {
                var varStringEnemy = QUtils.aiEnenmyTask.ReplaceLast("&&", string.Empty).Trim();

                var varStringSplit = QUtils.aiEnenmyTask.Replace("&&", "#").Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var varString in varStringSplit)
                {
                    statusMsg += QUtils.AddStatusMsg(-1, "Enemy man down! Great", varString);
                }

                QUtils.ekkTeamTaskId = QUtils.GenerateTaskID();
                statusMsg += QUtils.AddStatusMsg(QUtils.ekkTeamTaskId, "Ekk team down. Mission Completed", varStringEnemy);
                QUtils.aiEnenmyTask = null;
            }

            qscData += statusMsg.ReplaceLast("),", "));");
            return qscData;
        }

        internal static string AddPatrolTask(int patrolId, string taskNote = "Patrol Path")
        {
            string qtaskPatrolPath = "Task_New(" + patrolId + ",\"PatrolPath\",\"" + taskNote + "\"," + "\n";
            return qtaskPatrolPath;
        }

        internal static string AddPatrolCommand(string patrolTask, string taskNote, PATROLACTIONS pathCmd, int pathParam, bool lastCmd = false)
        {
            patrolTask += "Task_New(-1,\"PatrolPathCommand\"," + taskNote + pathCmd + "," + pathParam + ")";
            patrolTask += (lastCmd) ? ")," : ",";
            return patrolTask;
        }

        internal static string RemoveHumanSoldier(string qscData, string model)
        {
            int startIndex = 0, endIndex = 0, lcount = 0, rcount = 0;
            bool startRun = false;
            QUtils.AddLog("RemoveHumanSoldier() called with model : " + model + "\n");

            if (String.IsNullOrEmpty(qscData) || String.IsNullOrEmpty(model))
            {
                QUtils.ShowError("RemoveHumanSoldier : Input is empty");
                return null;
            }

            qscData = qscData.Trim();
            var qscDataSplit = qscData.Split('\n');
            string qscTmp = String.Copy(qscData);

            foreach (var data in qscDataSplit)
            {
                if (data.Contains(QUtils.taskNew))
                {
                    if (data.Contains(model) && data.Contains("HumanSoldier"))
                    {
                        if (data.Contains("Task_New(-1,"))
                        {
                            startRun = false;
                        }
                        else
                        {
                            if (data.Contains('('))
                                lcount += data.Count(o => o == '(');

                            startIndex = qscData.IndexOf(data);
                            if (startIndex == -1)
                            {
                                QUtils.ShowError("RemoveHumanSoldier : Data couldn't be found in QData file");
                                QUtils.AddLog("RemoveHumanSoldier() : Data couldn't be found in Substring\n");
                                QUtils.AddLog(data);
                                QUtils.SaveFile("objectsTmp.txt", qscData);
                                return qscTmp;
                            }
                            endIndex += data.Length + 1;
                            startRun = true;
                            continue;
                        }
                    }
                    if (startRun)
                    {
                        if (lcount >= 1)
                            endIndex += data.Length + 1;

                        if (data.Contains('('))
                            lcount += data.Count(o => o == '(');

                        if (data.Contains(')'))
                            rcount += data.Count(o => o == ')');

                        if (lcount == rcount)
                        {
                            startRun = false;
                            var aiSub = qscData.Substring(startIndex, endIndex);
                            qscData = qscData.Replace(aiSub, String.Empty);
                            startIndex = endIndex = lcount = rcount = 0;
                        }
                    }
                }
            }

            QUtils.AddLog("RemoveHumanSoldier() start index : " + startIndex + "  end index : " + endIndex + "\n");
            return qscData;
        }

        internal static List<string> GetAiModelIds(int level)
        {
            string inputQscPath = QUtils.cfgQscPath + level + "\\" + QUtils.objectsQsc;
            QUtils.AddLog("GetAiModels() level : called with level : " + level);
            string qscData = QCryptor.Decrypt(inputQscPath);
            List<string> aiModelIdsList = new List<string>();
            var modelRegex = @"\d{3}_\d{2}_\d{1}";
            var dataLines = qscData.Split('\n');

            foreach (var data in dataLines)
            {
                if (data.Contains("HumanSoldier"))
                {
                    string model = Regex.Match(data, modelRegex).Value;
                    //Friendly A.I exclude.
                    if (model == "015_01_1" || model == "020_01_1" || model == "021_01_1"
                        || model == "022_01_1" || model == "009_01_1" || model == "000_01_1")
                        continue;
                    else
                        aiModelIdsList.Add(model);
                }
            }

            aiModelIdsList = aiModelIdsList.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            return aiModelIdsList;
        }

        internal static List<KeyValuePair<int, Real64>> GetDynamicIds4AI(string dynamicType, bool fromBackup = true)
        {
            string qscData = null;
            if (fromBackup)
            {
                string qscBackupPath = QUtils.cfgQscPath + QUtils.gGameLevel + "\\" + QUtils.objectsQsc;
                qscData = QCryptor.Decrypt(qscBackupPath);
            }

            if (!fromBackup)
                qscData = QUtils.LoadFile();

            var qscDataLines = qscData.Split('\n');
            var dynamiclIdsList = new List<KeyValuePair<int, Real64>>();

            QUtils.AddLog("GetDynamicIds4AI() caled with level : " + QUtils.gGameLevel + " with type : " + dynamicType);

            foreach (var dataLine in qscDataLines)
            {
                if (dataLine.Contains(QUtils.taskNew) && dataLine.Contains(dynamicType))
                {
                    var dynamicTypeData = dataLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    QUtils.AddLog("GetDynamicIds4AI() dynamicType[1] : " + dynamicTypeData[1]);

                    if (dynamicTypeData[1].Trim() == "\"" + dynamicType + "\"")
                    {
                        QUtils.AddLog("GetDynamicIds4AI() dynamicType Data : " + dynamicTypeData);

                        int dynamicTypeId = Int32.Parse(Regex.Match(dynamicTypeData[0], @"\d+").Value);
                        QUtils.AddLog("GetDynamicIds4AI() dynamicTypeId : " + dynamicTypeId);

                        Double xPos = Double.Parse(dynamicTypeData[3].Trim());
                        Double yPos = Double.Parse(dynamicTypeData[4].Trim());
                        Double zPos = Double.Parse(dynamicTypeData[5].Trim());

                        Real64 alarmControlPos = new Real64(xPos, yPos, zPos);
                        dynamiclIdsList.Add(new KeyValuePair<int, Real64>(dynamicTypeId, alarmControlPos));
                    }
                }
            }
            QUtils.AddLog("GetDynamicIds4AI() returned list count : " + dynamiclIdsList.Count);
            return dynamiclIdsList;
        }

        internal static int GetNearestDynamicId(Real64 aiPos, string dynamicType)
        {
            QUtils.AddLog("GetNearestDynamicId() called with pos X : " + aiPos.x + " Y : " + aiPos.y + " Z : " + aiPos.z + " dynamicType : " + dynamicType);

            var dynamicIds = GetDynamicIds4AI(dynamicType, false);
            var diffPosList = new List<KeyValuePair<int, Real64>>();
            int nearestDynamicId = 0;

            if (dynamicIds.Count > 1)
            {
                foreach (var alarmControlId in dynamicIds)
                {
                    var alarmPos = alarmControlId.Value;
                    var diffPos = GetPosDiff(aiPos, alarmPos);
                    diffPosList.Add(new KeyValuePair<int, Real64>(alarmControlId.Key, diffPos));
                }
                diffPosList = diffPosList.OrderBy(o => o.Value.x).ToList();
                nearestDynamicId = diffPosList[0].Key;
            }
            else if (dynamicIds.Count == 1)
            {
                nearestDynamicId = dynamicIds[0].Key;
            }
            QUtils.AddLog("GetNearestDynamicId() returned : " + nearestDynamicId);
            return nearestDynamicId;
        }

        internal static Real64 GetPosDiff(Real64 pos1, Real64 pos2)
        {
            QUtils.AddLog("GetNearestAlarmId() called");
            Real64 diffPos = new Real64();

            diffPos.x = Math.Abs(pos1.x - pos2.x);
            diffPos.y = Math.Abs(pos1.y - pos2.y);
            diffPos.z = Math.Abs(pos1.z - pos2.z);

            QUtils.AddLog("GetNearestAlarmId() returned");
            return diffPos;
        }

        private static int GetBoneHeirarchy(string model)
        {
            int boneHeirarchy = 1;
            int level = QUtils.gGameLevel;
            string inputQscPath = QUtils.cfgQscPath + level + "\\" + QUtils.objectsQsc;
            QUtils.AddLog("GetBoneHeirarchy() level : called with level : " + level + " model : " + model);
            string qscData = QCryptor.Decrypt(inputQscPath);
            List<string> aiModelsList = new List<string>();
            var modelRegex = @"\d{3}_\d{2}_\d{1}";
            var dataLines = qscData.Split('\n');

            foreach (var data in dataLines)
            {
                if (data.Contains("HumanSoldier") || data.Contains("HumanSoldierFemale"))
                {
                    string modelData = Regex.Match(data, modelRegex, RegexOptions.RightToLeft).Value;
                    if (model == modelData)
                    {
                        var dataSplit = data.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        string heirarchy = dataSplit[(dataSplit.Length - 1) - 2];
                        boneHeirarchy = Convert.ToInt32(heirarchy);
                        break;
                    }
                }
            }
            QUtils.AddLog("GetBoneHeirarchy() returned boneHeirarchy : " + boneHeirarchy);
            return boneHeirarchy;
        }

        private static void InitAiModelList()
        {
            var allLevelsList = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
            var frieldyForcesList = new List<int>() { 4, 6, 12, 13, 14 };
            aiModelList.Add(new AIModel().Add("ANYA", "015_01_1", 'e', new List<int>() { 7, 8 }));
            aiModelList.Add(new AIModel().Add("EKK", "012_01_1", 'i', new List<int>() { 12, 14 }));
            aiModelList.Add(new AIModel().Add("JONES", "000_01_1", 'i', allLevelsList));
            aiModelList.Add(new AIModel().Add("SNIPER_01", "001_01_1", 'i', new List<int>() { 1, 3, 9, 13 }));
            aiModelList.Add(new AIModel().Add("SNIPER_02", "001_02_1", 'i', new List<int>() { 7, 8 }));
            aiModelList.Add(new AIModel().Add("GUNNER_406", "011_01_1", 'i', new List<int>() { 6 }));
            aiModelList.Add(new AIModel().Add("GUNNER_407", "011_03_1", 'i', new List<int>() { 6 }));
            aiModelList.Add(new AIModel().Add("SCIENTIST", "011_02_1", 'i', new List<int>() { 6, 9 }));
            aiModelList.Add(new AIModel().Add("PATROL_AK_01", "003_01_1", 'e', new List<int>() { 6, 8, 11, 12, 13, 14 }));
            aiModelList.Add(new AIModel().Add("PATROL_AK_02", "003_02_1", 'i', new List<int>() { 7, 8, 11 }));
            aiModelList.Add(new AIModel().Add("PATROL_AK_03", "006_01_1", 'i', new List<int>() { 7, 11 }));
            aiModelList.Add(new AIModel().Add("SECURITY_PATROL_SPAS", "019_01_1", 'i', new List<int>() { 1, 3, 5, 7, 8, 9, 11, 14 }));
            aiModelList.Add(new AIModel().Add("GUNNER", "013_01_1", 'i', new List<int>() { 2, 4, 7, 8, 9, 10, 12, 13 }));
            aiModelList.Add(new AIModel().Add("SOLDIER", "008_01_1", 'i', new List<int>() { 3, 5, 9, 11, 12 }));
            aiModelList.Add(new AIModel().Add("HARRISON", "022_01_1", 'i', new List<int>() { 4, 6, 13 }));
            aiModelList.Add(new AIModel().Add("FRIENDLY_SOLDIER_1", "021_01_1", 'i', frieldyForcesList));
            aiModelList.Add(new AIModel().Add("FRIENDLY_SOLDIER_2", "020_01_1", 'i', frieldyForcesList));
            aiModelList.Add(new AIModel().Add("JOSEP_PRIBOI", "009_02_1", 'i', new List<int>() { 4 }));
            aiModelList.Add(new AIModel().Add("MAFIA_GUARD", "014_01_1", 'i', new List<int>() { 6 }));
            aiModelList.Add(new AIModel().Add("MAFIA_PATROL", "014_02_1", 'i', new List<int>() { 6 }));
            aiModelList.Add(new AIModel().Add("PRIBOI", "009_01_1", 'i', new List<int>() { 6, 7, 9, 10 }));
            aiModelList.Add(new AIModel().Add("GUARD_AK", "004_02_1", 'i', new List<int>() { 9, 10 }));
            aiModelList.Add(new AIModel().Add("SPETNAZ_GUARD_AK", "018_01_1", 'i', new List<int>() { 9, 10, 12, 13, 14 }));
        }

        internal static List<string> GetAiModelNamesList(int level)
        {
            if (aiModelList.Count == 0)
                InitAiModelList();

            var aiModelNamesList = new List<string>();
            foreach (var aiModel in aiModelList)
            {
                if (aiModel.Option == 'i')
                {
                    if (aiModel.Levels.Contains(level))
                        aiModelNamesList.Add(aiModel.ModelName);
                }
                else if (aiModel.Option == 'e')
                {
                    if (!aiModel.Levels.Contains(level))
                        aiModelNamesList.Add(aiModel.ModelName);
                }
            }
            return aiModelNamesList;
        }


        internal static string GetAiModelIdForName(string aiModelName)
        {
            if (aiModelList.Count == 0)
                InitAiModelList();
            foreach (var aiModel in aiModelList)
            {
                if (aiModelName == aiModel.ModelName)
                {
                    QUtils.AddLog("GetAiModelIdForName(): model name '" + aiModelName + "' Returned model id : " + aiModel.ModelId);
                    return aiModel.ModelId;
                }
            }
            return null;
        }

        internal static string GetAiImageId(string aiModelName)
        {
            string imageId = null;
            aiModelName = aiModelName.Replace("_", "-");

            foreach (var aiModel in QUtils.aiModelDict)
            {
                if (aiModel.Value == aiModelName)
                {
                    imageId = aiModel.Key;
                    break;
                }
            }
            QUtils.AddLog("GetAiImageId(): aiModelName " + aiModelName + " Returned imageId : " + imageId);
            return imageId;
        }

        internal static List<string> GetAiTypes()
        {
            return QUtils.aiTypes;
        }
    }

}
