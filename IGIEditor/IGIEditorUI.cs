using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QLibc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using QUxLib.UX;
using static IGIEditor.QUtils;
using static IGIEditor.QServer;
using static System.Drawing.Color;
using static System.Drawing.FontStyle;
using Timer = System.Windows.Forms.Timer;

namespace IGIEditor
{
    public partial class IGIEditorUI : Form
    {
        #region variables
        //Variables section.
        private bool compileStatus = true;
        private List<QUtils.QScriptTask> qtaskList = new List<QUtils.QScriptTask>();
        private int buildingsCount = 0, rigidObjCount = 0;
        private static bool isBuildingDD = false, isObjectDD = true, internalsAttached = false;
        string inputQvmPath, inputQscPath;
        private static int gameLevel = 1, aiGraphId = 1, nodeId = 1;
        private Real64 graphPos, nodeRealPos;
        private QGraphs.GraphNode graphNode;
        private int graphTotalNodes;
        private QServer.QMissionsData missionData;
        public delegate void EnableTimers(bool enable);
        private static FOpenIO fopenIO;
        private string[] texFiles;
        private int texIndex = 0;
        private string textureSelectedPath = null;
        #endregion

        #region timers
        static internal IGIEditorUI editorRef;
        BackgroundWorker graphNodesAddWorker, graphLinksAddWorker, graphTraverseWorker, nodesTraverseWorker, downloadMissionWorker, downloadUpdaterWorker, uploadMissionWorker, addAiSoldierWorker;
        Timer updateCheckerTimer = new Timer();
        Timer internalsAttachTimer = new Timer();
        Timer levelRunTimer = new Timer();
        #endregion

        //Main-Start - Ctr.
        public IGIEditorUI()
        {
            try
            {
                var updatePositionTimer = new Timer();

                InitializeComponent();
                UXWorker formMover = new UXWorker();
                editorRef = this;
                this.KeyPreview = true;
                QUtils.appEditorSubVersion = devVersionTxt.Text = ParseEditorVersion();
                QUtils.shortcutExist = CheckShortcutExist();
#if DEV_MODE
                appSupportBtn.Visible = true;
                GAME_MAX_LEVEL = 14;
                versionLbl.Text = "DEV";
                editorTabs.TabPages.RemoveByKey("objectEditor");
                editorTabs.TabPages.RemoveByKey("threeDEditor");
#elif DEBUG
                //appSupportBtn.Visible = false;
                GAME_MAX_LEVEL = 14;
                versionLbl.Text = "DBG";
                editorTabs.TabPages.RemoveByKey("threeDEditor");
                editorTabs.TabPages.RemoveByKey("devMode");
                editorTabs.TabPages.RemoveByKey("objectEditor");
                gameItemsLbl.Visible = true;
#else
                GAME_MAX_LEVEL = 14;
                editorTabs.TabPages.RemoveByKey("threeDEditor");
                editorTabs.TabPages.RemoveByKey("objectEditor");
                versionLbl.Text = appEditorSubVersion;
                editorTabs.TabPages.RemoveByKey("devMode");
                gameItemsLbl.Visible = false;
#endif
                #region Timers
                //Start Position timer.
                updatePositionTimer.Tick += new EventHandler(UpdatePositionTimer);
                updatePositionTimer.Interval = 500;

                //Internals Attach/Detach timer.
                internalsAttachTimer.Tick += new EventHandler(InternalsAttachedTimer);
                internalsAttachTimer.Interval = 30000;//30 Seconds.

                //Level runner timer.
                levelRunTimer.Tick += new EventHandler(LevelRunnerTimer);
                levelRunTimer.Interval = 30000;//30 Seconds.

                //Update checker timer.
                updateCheckerTimer.Tick += new EventHandler(UpdateCheckerTimer);
                #endregion

                #region WorkerThreads
#if DEV_MODE
#else
                //Adding Background Worker threads
                graphNodesAddWorker = new BackgroundWorker();
                graphLinksAddWorker = new BackgroundWorker();
                graphTraverseWorker = new BackgroundWorker();
                nodesTraverseWorker = new BackgroundWorker();
                downloadMissionWorker = new BackgroundWorker();
                uploadMissionWorker = new BackgroundWorker();
                downloadUpdaterWorker = new BackgroundWorker();
                addAiSoldierWorker = new BackgroundWorker();

                graphNodesAddWorker.DoWork += AddGraphNodesBackground;
                graphLinksAddWorker.DoWork += AddGraphLinksBackground;
                graphTraverseWorker.DoWork += GraphTraverseBackground;
                nodesTraverseWorker.DoWork += NodeTraverseBackground;
                downloadMissionWorker.DoWork += DownloadMissionBackground;
                downloadUpdaterWorker.DoWork += DownloadUpdaterBackground;
                uploadMissionWorker.DoWork += UploadMissionBackground;
                addAiSoldierWorker.DoWork += AddAiSoldierBackground;

                //Thread Support Cancellation
                graphTraverseWorker.WorkerSupportsCancellation = nodesTraverseWorker.WorkerSupportsCancellation = true;
#endif
                #endregion

                //Disabling Errors and Warnings.
                GT.GT_SuppressErrors(true);
                GT.GT_SuppressWarnings(true);

                //Get Game level from start.
                gameLevel = Convert.ToInt32(levelStartTxt.Text.ToString());

                //Initialize application data and paths.
                InitEditorApp();
                InitEditorPaths(gameLevel);

                if (QMemory.FindGame())
                {
                    QUtils.gameFound = true;
                    gameLevel = QUtils.gGameLevel = QMemory.GetRunningLevel();
                    if (gameLevel <= 0 || gameLevel > GAME_MAX_LEVEL) gameLevel = 1;
                }
                else
                {
                    // Game is not running here | SECTION.
                    QUtils.gameFound = false;
                }

                // Copy file data if game found.
                if (QUtils.gameFound)
                {
                    string inputQscPath = QUtils.cfgQscPath + gameLevel + "\\" + QUtils.objectsQsc;
                    QUtils.graphsPath = QUtils.cfgGamePath + gameLevel + "\\" + "graphs";

                    if (!File.Exists(QUtils.objectsQsc) && File.Exists(inputQscPath))
                    {
                        QUtils.FileIOCopy(inputQscPath, QUtils.objectsQsc);
                    }
                    else if (!File.Exists(inputQscPath))
                    {
                        throw new Exception("File 'objects.qsc' is missing from path '" + QUtils.cfgQscPath + gameLevel + "\\'");
                    }
                }

                if (!File.Exists(QUtils.weaponConfigQSC) && File.Exists(QUtils.weaponsCfgQscPath))
                {
                    QUtils.FileIOCopy(weaponsCfgQscPath, QUtils.weaponConfigQSC);
                }

                // Reset section.
                if (gameReset)
                {
                    QUtils.ResetScriptFile(gameLevel);
                }
                QUtils.CleanUpTmpFiles();

                if (appLogs)
                {
                    QUtils.EnableLogs();
                    //GT.GT_EnableLogs(); //Disabling Library logs as they are increasing huge in size.
                }

                //Get current level selected.
                if (QUtils.gameFound)
                {
                    gameLevel = QMemory.GetRunningLevel();
                    if (gameLevel <= 0 || gameLevel > GAME_MAX_LEVEL) gameLevel = 1;
                    levelStartTxt.Text = gameLevel.ToString();
                    SetStatusText("Game is running...");
                    LoadLevelDetails(gameLevel);

                    //Init Dropdown,List UI components.
                    InitUIComponents(gameLevel);
                    //QUtils.AttachInternals();
                }
                else
                {
                    //Init Dropdown,List UI components.
                    QUtils.gGameLevel = gameLevel = Convert.ToInt32(levelStartTxt.Value);
                    InitUIComponents(gameLevel);
                    SetStatusText("Game is not running...");
                }


                //Start all timers after app gets configures properly.
                EnableTimers enableTimers = delegate (bool enable)
                {
#if DEV_MODE
                    if (enable)
                    {
                        levelRunTimer.Start();
                        internalsAttachTimer.Start();
                    }
#else
                    if (enable)
                    {
                        updatePositionTimer.Start();
                        if (QUtils.gameRefresh)
                        {
                            internalsAttachTimer.Start();
                            levelRunTimer.Start();
                        }
                    }
#endif
                };

                //Enbale editor timer's only if game found.
                enableTimers(QUtils.gameFound);
                //Delete previous Updater from Cache.
                QUtils.DirectoryIODelete(QUtils.editorUpdaterAbsDir);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void AddAiSoldierBackground(object sender, DoWorkEventArgs e)
        {
            try
            {
                string aiModelName = null, aiModelId = null, aiWeaponMode = null, aiType = null, aiWeaponModel = null;
                int aiCount = 1, maxSpawns = 1, teamId = TEAM_ID_ENEMY;

                Invoke((Action)(() =>
                {
                    aiModelName = QAI.GetAiModelNamesList(gameLevel)[aiModelSelectDD.SelectedIndex];
                    aiModelId = QAI.GetAiModelId4Name(aiModelName);
                    aiWeaponModel = QUtils.weaponId + QUtils.weaponList[aiWeaponDD.SelectedIndex].Keys.ElementAt(0);
                    aiGraphId = QUtils.aiGraphIdStr[aiGraphIdDD.SelectedIndex];
                    aiType = QUtils.aiTypes[aiTypeDD.SelectedIndex];
                    aiCount = Convert.ToInt32(aiCountTxt.Text);
                    maxSpawns = Convert.ToInt32(maxSpawnsTxt.Text);
                    teamId = Convert.ToInt32(teamIdText.Text);
                }));

                //Set human A.I properties.
                var humanAi = new HumanAi();
                humanAi.model = aiModelId;
                humanAi.weapon = aiWeaponModel;
                humanAi.graphId = aiGraphId;
                humanAi.aiType = aiType;
                humanAi.aiCount = aiCount;
                humanAi.guardGenerator = guardGeneratorCb.Checked;
                humanAi.maxSpawns = maxSpawns;
                humanAi.invincible = aiInvincibleCb.Checked;
                humanAi.advanceView = aiAdvanceViewCb.Checked;
                humanAi.teamId = teamId;

                string configOut = "";
                configOut += "A.I Count : " + humanAi.aiCount + "\n";
                configOut += "AI Type : " + humanAi.aiType + "\n";
                configOut += "Graph Id : " + humanAi.graphId + "\n";
                configOut += "Weapon : " + humanAi.weapon + "\n";
                configOut += "Model : " + humanAi.model + "\n";
                configOut += "Team Id : " + humanAi.teamId + "\n";
                configOut += "Guard Generator : " + humanAi.guardGenerator + "\n";
                configOut += "Spawns : " + humanAi.maxSpawns + "\n";
                configOut += "Invincible : " + humanAi.invincible + "\n";
                configOut += "Advance View : " + humanAi.advanceView + "\n";

                var dlgResult = QUtils.ShowDialog("You are about to Add A.I confirm ?\n" + configOut);

                if (dlgResult == DialogResult.Yes)
                {
                    QUtils.AddLog("AddHumanSoldier", "Level " + gameLevel + ", Model Name: " + QObjects.FindModelName(humanAi.model) + ", " + configOut.Replace("\n", ", "));
                    var qscData = QAI.AddHumanSoldier(humanAi);

                    if (String.IsNullOrEmpty(qscData))
                    {
                        QUtils.ShowLogStatus("AddHumanSoldier", "Error: Adding " + aiModelName + " A.I to level '" + gameLevel + "'");
                        QUtils.aiScriptId = QUtils.aiScriptId > QUtils.LEVEL_FLOW_TASK_ID ? (QUtils.aiScriptId - 3) : QUtils.aiScriptId;//Reset scriptId on error.
                        return;
                    }
                    //Add task detection only if selected.
                    if (taskDetectionAiCb.Checked) qscData += QAI.AddAiTaskDetection(qscData);

                    if (!String.IsNullOrEmpty(qscData)) compileStatus = QCompiler.Compile(qscData, QUtils.gamePath, true);

                    if (compileStatus) SetStatusText("AI " + aiModelName + " Added successfully");
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                SetStatusText("A.I data cannot be empty.");
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        //protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        //{
        //    return base.ProcessCmdKey(ref msg, keyData);
        //}

        private static string ParseEditorVersion()
        {
            try
            {
                if (!File.Exists(QUtils.versionFileName + QUtils.FileExtensions.Text))
                {
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Editor version file is missing from directory, couldn't get correct version");
                    QUtils.SaveFile(QUtils.versionFileName.ToUpper() + QUtils.FileExtensions.Text, appEditorSubVersion);
                    return appEditorSubVersion;
                }

                string subVersion = QUtils.LoadFile(QUtils.versionFileName + QUtils.FileExtensions.Text);
                var versionCount = subVersion.Count(c => c == '.');

                if (versionCount != 3 || subVersion != appEditorSubVersion)
                {
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Editor version file has incorrect format.");
                    QUtils.SaveFile(QUtils.versionFileName.ToUpper() + QUtils.FileExtensions.Text, appEditorSubVersion);
                    return appEditorSubVersion;
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return appEditorSubVersion;
        }

        private void UpdateCheckerTimer(object sender, EventArgs e)
        {
            try
            {
                updateCheckerTimer.Stop();//Dont check for new updates while updating - 'Macht keinen Sense oder?'
                string updateName = QUtils.editorUpdaterDir;
                string updateNameAbs = QUtils.cachePath + "\\" + updateName + QUtils.FileExtensions.Zip;
                string tmpUpdateName = QUtils.GenerateRandStr(6);
                string updaterVersion = null, updaterMask = "Updater-";

                long updateSize = 0, tmpUpdateSize = 0;
                bool updateAvailable = false, downgradeAvailable = false;

                var dirData = QServer.GetDirList(QServer.updateDir, false, new List<string>() { QUtils.FileExtensions.Zip, QUtils.FileExtensions.Text });
                foreach (var dir in dirData)
                {
                    if (dir.FileName.Contains("Updater-"))
                    {
                        updaterVersion = dir.FileName.Replace(updaterMask, String.Empty).Replace(QUtils.FileExtensions.Text, String.Empty);
                    }

                    else if (Path.GetFileName(dir.FileName) == QUtils.editorUpdaterDir + QUtils.FileExtensions.Zip)
                    {
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Server: File '" + dir.FileName + "' size: " + Convert.ToInt32(dir.FileSize) / 1024 + "Kb");
                        tmpUpdateSize = Convert.ToInt32(dir.FileSize);
                    }
                }

                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Updater file Size is " + updateSize / 1024 + "Kb");
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "TmpUpdater file Size is " + tmpUpdateSize / 1024 + "Kb");


                var result = CheckEditorVersion(updaterVersion, appEditorSubVersion);
                if (result > 0)
                {
                    updateAvailable = true;
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Update available Updater 'v" + updaterVersion + "' Editor: 'v" + appEditorSubVersion + "'");
                }
                else if (result < 0)
                {
                    downgradeAvailable = true;
                    QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Downgrade available Updater 'v" + updaterVersion + "' Editor: 'v" + appEditorSubVersion + "'");
                }
                else
                {
                    updateAvailable = downgradeAvailable = false;
                    QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Editor already on latest version 'v" + updaterVersion + "' Editor: 'v" + appEditorSubVersion + "'");
                }
                if (updateAvailable || downgradeAvailable)
                {
                    string updateMsg = "A New version of Editor is avaliable to Download\nDo you wish to update ?";
                    if (downgradeAvailable) updateMsg = "An Old version of Editor is avaliable to Download\nDo you wish to downgrade ?";

                    var dlgResult = QUtils.ShowDialog(updateMsg);
                    if (dlgResult == DialogResult.Yes)
                    {
                        //Replace old version with new version.
                        QUtils.FileIODelete(updateNameAbs);
                        //QUtils.FileIOMove(tmpUpdateNameAbs, updateNameAbs);
                        QUtils.EditorUpdater(updateName, UPDATE_ACTION.UPDATE);
                    }
                    else QUtils.FileIODelete(tmpUpdateName);

                }
                updateCheckerTimer.Start();//Resume the time again.
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void DownloadUpdaterBackground(object sender, DoWorkEventArgs e)
        {
            //QUtils.EditorUpdater(QUtils.editorUpdaterDir, UPDATE_ACTION.UPDATE);
            UpdateCheckerTimer(sender, e);
        }

        private void AddGraphLinksBackground(object sender, DoWorkEventArgs e)
        {
            try
            {
                var graphIdList = QUtils.aiGraphIdStr;
                var qscData = QUtils.LoadFile();
                int graphWorkTotal = graphIdList.Count, graphWorkCount = 1, graphWorkPercent = 1;
				int level = QUtils.gGameLevel;

				if (level <= 0 || level > QUtils.GAME_MAX_LEVEL)
				    level = QMemory.GetRunningLevel();

                if (graphsAllCb.Checked)
                {
                    foreach (var graphId in graphIdList)
                    {
                        var graphNodes = QGraphs.GetNodesForGraph(graphId, level);
                        if (graphNodes.Count <= 300)
                        {
                            var graphPos = QGraphs.GetGraphPosition(graphId);
                            qscData += QGraphs.ShowNodeLinksVisual(graphId, ref graphPos) + "\n";
                            graphWorkPercent = (int)Math.Round((double)(100 * graphWorkCount) / graphWorkTotal);
                            SetStatusText("Graph#" + graphId + " Links added, Completed " + graphWorkPercent + "%");
                            graphWorkCount++;
                            var gameItemsCount = QUtils.GameitemsCount();
                            if (gameItemsCount >= 500) break;//512Kb.
                        }
                    }
                    QUtils.SwitchEditorUI();
                }
                else
                {
                    qscData += QGraphs.ShowNodeLinksVisual(aiGraphId, ref graphPos);
                }

                if (!String.IsNullOrEmpty(qscData))
                {
                    QCompiler.CompileEx(qscData);
                    SetStatusText("Graph Links Visualisation added successfully");
                }
                else SetStatusText("Graph Links Visualisation error.");
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void LevelRunnerTimer(object sender, EventArgs e)
        {
            if (!internalsAttached) SetInternalsStatus(false);

            //Init game path every time game found.
            QUtils.gameFound = QMemory.FindGame();
            if (gameFound)
            {
                int currLevel = QMemory.GetRunningLevel();
                if (currLevel != gameLevel)
                {
                    refreshGame_Click(sender, e);
                }
                else
                {
                    InitEditorPaths(currLevel);
                }
            }

            //Start Game if not found.
            if (!gameFound && QUtils.CheckShortcutExist())
            {
                SetStatusText("Game not running... starting");
                startGameBtn_Click(sender, e);
            }
        }

        private void InternalsAttachedTimer(object sender, EventArgs e)
        {

            //Check internals already attached.
            internalsAttached = QUtils.CheckInternalsAttached();

            if (internalsAttached)
            {
                SetInternalsStatus(true);

                //Load game data after internals.
                LoadGameProfile();
            }
            else
            {
                SetStatusText("Internals not attached Attaching now....");
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Internals were not attached with the game Attaching internals now.");
                if (!QUtils.gameFound) return;

                QUtils.attachStatus = QUtils.AttachInternals();
                if (QUtils.attachStatus)
                {
                    SetStatusText("Internals attached success....");
                    SetInternalsStatus(true);
                }
            }
        }

        private void UploadMissionBackground(object sender, DoWorkEventArgs e)
        {
            try
            {
                OpenFileDialog fileDlg = new OpenFileDialog();
                fileDlg.Filter = "Mission file (*.igimsf)|*.igimsf|All files (*.*)|*.*";
                fileDlg.RestoreDirectory = true;
                fileDlg.Title = "Select IGI Mission file";
                fileDlg.DefaultExt = ".igimsf";
                fileDlg.Multiselect = false;
                fileDlg.CheckFileExists = fileDlg.RestoreDirectory = fileDlg.AddExtension = true;
                string missionNamePath = null, missionName = null;

                DialogResult dlgResult = DialogResult.OK;
                Invoke((Action)(() => { dlgResult = fileDlg.ShowDialog(); }));

                if (dlgResult == DialogResult.OK)
                {
                    QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Mission Uploading...");
                    missionNamePath = fileDlg.FileName;
                    missionName = Path.GetFileName(missionNamePath);

                    string missionUrl = "/" + QServer.serverBaseURL + QServer.missionDir + "/" + missionName;
                    //ShowInfo("Mission Path: " + missionUrl + "\nmissionNamePath: " + missionNamePath);

                    var missionAuthor = QUtils.GetCurrentUserName().Replace(" ", "-");
                    var missionFullName = Path.GetFileNameWithoutExtension(missionName) + "@" + missionAuthor + "#" + gameLevel + QUtils.FileExtensions.Mission;
                    missionFullName = HttpUtility.UrlEncode(missionFullName);
                    string missionUrlPath = "/" + QServer.missionDir + "/" + missionFullName;

                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Url: '" + missionUrl + "' file '" + missionNamePath + "'");
                    var status = QServer.Upload(missionUrlPath, missionNamePath);
                    if (status) QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Mission was uploaded successfully.");
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void DownloadMissionBackground(object sender, DoWorkEventArgs e)
        {
            try
            {
                SetStatusText("Mission Downloading...");
                string missionUrl = QServer.serverBaseURL + QServer.missionDir;
                string missionName = missionData.MissionName + "@" + missionData.MissionAuthor + "#" + missionData.MissionLevel;

                string missionEscapeUri = HttpUtility.UrlEncode(missionName) + QUtils.FileExtensions.Mission;
                missionUrl = missionUrl + "/" + missionEscapeUri;

                string missionNamePath = null;
                string missionUrlPath = "/" + QServer.missionDir + "/" + missionEscapeUri;


                Invoke((Action)(() =>
                {
                    missionNamePath = missionsOnlineDD.Text + QUtils.FileExtensions.Mission;
                }));

                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Url: '" + missionUrl + "' file '" + missionNamePath + "'");
                var status = QServer.Download(missionUrlPath, missionNamePath, QUtils.qMissionsPath);
                if (status) SetStatusText("Mission was downloaded successfully.");
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void GraphTraverseBackground(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (((BackgroundWorker)sender).CancellationPending && ((BackgroundWorker)sender).IsBusy)
                {
                    e.Cancel = true;
                    return;
                }
                SetStatusText("Traversing Graphs now...");
                foreach (var graphId in aiGraphIdStr)
                {
                    if (autoTeleportGraphCb.Checked)
                    {
                        var graphPos = QGraphs.GetGraphPosition(graphId);
                        QUtils.UpdateViewPort(graphPos);
                        string graphArea = QGraphs.GetGraphArea(graphId);
                        QInternals.StatusMessageShow("Graph Area " + graphArea);
                        QUtils.Sleep(10);
                    }
                    else break;
                }
                QInternals.StatusMessageShow("Graph #" + aiGraphId + " nodes traversed.");
                viewPortCameraEnableCb.Checked = false;
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void NodeTraverseBackground(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (((BackgroundWorker)sender).CancellationPending && ((BackgroundWorker)sender).IsBusy)
                {
                    e.Cancel = true;
                    return;
                }
                string graphArea = null;
                SetStatusText("Traversing Nodes now...");
                foreach (var nodeId in QUtils.aiGraphNodeIdStr)
                {
                    if (autoTeleportNodeCb.Checked)
                    {
                        var graphNodeData = QGraphs.GetGraphNodeData(aiGraphId, nodeId);
                        //Get Node Real Position.
                        var nodePos = new Real64();
                        nodePos.x = graphPos.x + graphNodeData.MGraphVertexNodes.LastOrDefault().NodePos.x;
                        nodePos.y = graphPos.y + graphNodeData.MGraphVertexNodes.LastOrDefault().NodePos.y;
                        nodePos.z = graphPos.z + graphNodeData.MGraphVertexNodes.LastOrDefault().NodePos.z + QUtils.viewPortDelta;

                        QUtils.UpdateViewPort(nodePos);
                        graphArea = QGraphs.GetGraphArea(aiGraphId);
                        QInternals.StatusMessageShow("Graph " + graphArea + " Node #" + nodeId);
                        QUtils.Sleep(8.5f);
                    }
                    else break;
                }
                QInternals.StatusMessageShow("Graph #" + aiGraphId + " Area " + graphArea + " traversed.");
                viewPortCameraEnableCb.Checked = false;
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void AddGraphNodesBackground(object sender, DoWorkEventArgs e)
        {
            try
            {
                int gameItemsCount = QUtils.GameitemsCount();

                if (((BackgroundWorker)sender).CancellationPending && ((BackgroundWorker)sender).IsBusy || gameItemsCount >= 500)
                {
                    e.Cancel = true;
                    return;
                }

                string qscData = null;

                const string woodenCrateModel = "301_01_1", packageModel = "219_01_1";
                string objModel = QObjects.CheckModelExist(woodenCrateModel) ? woodenCrateModel : packageModel;

                int graphWorkTotal = 1, graphWorkCount = 1, graphWorkPercent = 1;
                if (nodesObjectsCb.Checked || nodesInfoCb.Checked)
                {
                    if (graphsMarkCb.Checked)
                    {
                        graphWorkTotal = QUtils.graphdIdsMarked.Count;
                        foreach (var graphId in QUtils.graphdIdsMarked)
                        {
                            qscData += QGraphs.ShowGraphNodesVisual(graphId, GRAPH_VISUAL.OBJECTS, nodesInfoCb.Checked, objModel) + "\n";
                            graphWorkPercent = (int)Math.Round((double)(100 * graphWorkCount) / graphWorkTotal);
                            SetStatusText("Graph#" + graphId + " Nodes added, Completed " + graphWorkPercent + "%");
                            QUtils.Sleep(2.5f);
                            graphWorkCount++;
                        }
                        QUtils.SwitchEditorUI();
                        QUtils.graphdIdsMarked.Clear();//Clear marked GraphIds.
                        graphsMarkCb.Checked = false;
                    }

                    else if (graphsAllCb.Checked)
                    {
                        var graphIdList = QUtils.aiGraphIdStr;//QGraphs.GetGraphIds(gameLevel);
                        graphWorkTotal = graphIdList.Count;
                        foreach (var graphId in graphIdList)
                        {
                            // var objModel = "301_01_1"; //QUtils.objectRigidList[objectSelectDD.SelectedIndex].Values.ElementAt(0);
                            qscData += QGraphs.ShowGraphNodesVisual(graphId, GRAPH_VISUAL.OBJECTS, nodesInfoCb.Checked, objModel) + "\n";
                            graphWorkPercent = (int)Math.Round((double)(100 * graphWorkCount) / graphWorkTotal);
                            SetStatusText("Graph#" + graphId + " Nodes added, Completed " + graphWorkPercent + "%");
                            QUtils.Sleep(2.5f);
                            graphWorkCount++;
                        }
                        QUtils.SwitchEditorUI();
                    }
                    else
                    {
                        //var objModel = "301_01_1"; //QUtils.objectRigidList[objectSelectDD.SelectedIndex].Values.ElementAt(0);
                        qscData = QGraphs.ShowGraphNodesVisual(aiGraphId, GRAPH_VISUAL.OBJECTS, nodesInfoCb.Checked, objModel);
                    }
                }
                else if (nodesHilightCb.Checked)
                {
                    qscData = QGraphs.ShowGraphNodesVisual(aiGraphId, GRAPH_VISUAL.HILIGHT);
                }

                qscData = QUtils.LoadFile() + "\n" + qscData;
                compileStatus = QCompiler.CompileEx(qscData);

                if (compileStatus) SetStatusText("Graph Nodes Visualisation added successfully");
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void FileIntegrityCheckerTimer(object sender, EventArgs e)
        {
            try
            {
                FileIntegrity.RunFileIntegrityCheck(null, new List<string> { QUtils.cfgQFilesPath, QUtils.cfgVoidPath, QUtils.qedAiScriptPath, QUtils.qedAiPatrolPath });
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void UpdatePositionTimer(object sender, EventArgs e)
        {
            try
            {
                internalsAttached = QUtils.CheckInternalsAttached();
                SetInternalsStatus(internalsAttached);

                if (QUtils.gameFound)
                {
                    if (posMetersCb.Checked)
                    {
                        var meterPos = (editorModeCb.Checked) ? QUtils.GetViewPortPos() : QHuman.GetPositionInMeter(false);
                        xPosLbl.Text = meterPos.x.ToString("0.0");
                        yPosLbl.Text = meterPos.y.ToString("0.0");
                        zPosLbl.Text = meterPos.z.ToString("0.0");
                    }
                    else if (posCoordCb.Checked)
                    {
                        var realPos = QHuman.GetPositionCoord(false);
                        xPosLbl.Text = realPos.alpha.ToString("0.0000");
                        yPosLbl.Text = realPos.beta.ToString("0.0000");
                        zPosLbl.Text = realPos.gamma.ToString("0.0000");
                    }
                }
            }
            catch (Exception) { }
        }

        private void SetInternalsStatus(bool internalsAttached)
        {
            try
            {
                if (internalsAttached)
                {
                    internalsStatusLbl.Text = "Attached";
                    internalsStatusLbl.ForeColor = SpringGreen;
                }
                else
                {
                    internalsStatusLbl.Text = "Detached";
                    internalsStatusLbl.ForeColor = Tomato;
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void InitUIComponents(int level, bool initialInit = true, bool objItems = true, bool aiItems = true, bool missionItems = true, bool weaponAdvanceData = true)
        {
            try
            {
                if (aiItems)
                {
                    //Init Weapons list.
                    weaponDD.Items.Clear();
                    weaponCfgDD.Items.Clear();
                    aiWeaponDD.Items.Clear();
                    QUtils.weaponDataList = QWeapon.GetWeaponTaskList(weaponAdvanceData);
                    QUtils.weaponList = QHuman.GetWeaponsList();
                    QUtils.weaponSFXList = QWeapon.GetWeaponSFXList();

                    try
                    {
                        //Adding Weapon list.
                        foreach (var weapon in QUtils.weaponList)
                        {
                            var weaponName = weapon.Keys.ElementAt(0);
                            weaponDDList.Add(weaponName);
                            weaponDD.Items.Add(weaponName);
                            weaponCfgDD.Items.Add(weaponName);
                            aiWeaponDD.Items.Add(weaponName);
                        }
                    }
                    catch (Exception ex)
                    {
                        QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
                    }

                    //Adding Weapon SFX list.
                    foreach (var weaponSfx in QUtils.weaponSFXList)
                    {
                        weaponSfx1DD.Items.Add(weaponSfx);
                        weaponSfx2DD.Items.Add(weaponSfx);
                    }

                    try
                    {
                        //Init AI model list.
                        QUtils.aiModelsListStr.Clear();
                        var aiModelNamesList = QAI.GetAiModelNamesList(level);
                        foreach (var aiModelName in aiModelNamesList)
                        {
                            aiModelsListStr.Add(aiModelName);
                            if (initialInit)
                                aiModelSelectDD.Items.Add(aiModelName);
                        }
                    }
                    catch (Exception ex)
                    {
                        QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
                    }

                    try
                    {
                        //Init AI types list.
                        aiTypeDD.Items.Clear();
                        var aiTypesList = QAI.GetAiTypes();
                        foreach (var aiType in aiTypesList)
                        {
                            aiTypeDD.Items.Add(aiType.Replace("AITYPE_", String.Empty).Replace("_AK", String.Empty).Replace("_UZI", String.Empty));
                        }
                    }
                    catch (Exception ex)
                    {
                        QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
                    }

                    //Init AI Graph list.
                    try
                    {
                        QGraphs.GraphLevelInit(level, showAllGraphsCb.Checked, initialInit, ref aiGraphIdDD);
                    }
                    catch (Exception ex)
                    {
                        QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
                    }
                }

                if (objItems)
                {
                    //Init Buildings list.
                    QUtils.buildingListStr.Clear();
                    QUtils.buildingList = QObjects.GetObjectList(level, QTYPES.BUILDING, true, true);
                    foreach (var building in QUtils.buildingList)
                    {
                        QUtils.buildingListStr.Add(building.Keys.ElementAt(0));
                        if (initialInit)
                            buildingSelectDD.Items.Add(building.Keys.ElementAt(0));
                    }

                    //Init 3D-Rigid Objects list.
                    QUtils.objectRigidListStr.Clear();
                    QUtils.objectRigidList = QObjects.GetObjectList(level, QTYPES.RIGID_OBJ, true, true);
                    foreach (var rigidObj in QUtils.objectRigidList)
                    {
                        QUtils.objectRigidListStr.Add(rigidObj.Keys.ElementAt(0));
                        if (initialInit)
                        {
                            objectSelectDD.Items.Add(rigidObj.Keys.ElementAt(0));
                        }
                    }
                    if (buildingSelectDD.Items.Count > 0 && objectSelectDD.Items.Count > 0 && aiGraphIdDD.Items.Count > 0)
                        buildingSelectDD.SelectedIndex = objectSelectDD.SelectedIndex = aiGraphIdDD.SelectedIndex = 0;
                }
                // Init Online Missions list.
                if (missionItems) InitMissionsOnline(initialInit, true, false);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        internal void InitMissionsOnline(bool initialInit, bool useCache = true, bool showWarning = true)
        {
            try
            {
                //Init Online Missions list.
                if (!QUtils.editorOnline)
                {
                    if (showWarning) QUtils.ShowWarning("Editor is not in online mode to get missions list from server.");
                    downloadMissionBtn.Enabled = uploadMissionBtn.Enabled = false;
                    return;
                }

                //if (missionNameListStr.Count == 0) return;

                missionNameListStr.Clear();
                List<QServer.QMissionsData> missionsData;
                if (File.Exists(QUtils.missionListFile) && useCache)
                    missionsData = QSerialize.ReadFromBinaryFile<List<QServer.QMissionsData>>(QUtils.missionListFile);
                else
                    missionsData = QServer.GetMissionsData(useCache);

                foreach (var mission in missionsData)
                {
                    missionNameListStr.Add(mission.MissionName);
                    if (initialInit) missionsOnlineDD.Items.Add(mission.MissionName);
                }
                if (!File.Exists(QUtils.missionListFile) || useCache) QSerialize.WriteToBinaryFile(QUtils.missionListFile, missionsData);
                QUtils.qServerMissionDataList = missionsData;
                if (buildingSelectDD.Items.Count > 0)
                    buildingSelectDD.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void InitEditorApp()
        {
            try
            {
                qIniParser = new QIniParser(iniCfgFile);
                QUtils.gameAbsPath = qIniParser.Read("game_path", PATH_SECTION);
                QUtils.editorOnline = QUtils.IsNetworkAvailable();

                //Initialize app data for QEditor.
                var status = QUtils.InitEditorAppData();

                string userName = QUtils.GetCurrentUserName();
                userName = userName.Replace("-", " ").Replace("_", " ");

                string welcomeMsg = "Welcome " + userName + " to IGI 1 Editor";

                bool initUser = !String.IsNullOrEmpty(userName);

                if (initUser)
                {
                    //QUtils.ShowLogInfo(MethodBase.GetCurrentMethod().Name, welcomeMsg);
                }

                else
                {
                    QUtils.ShowError("Error occurred while initializing user data. (Error: SERVER_ERR)");
                }

                //Show Game set path dialog.
                /*if (!File.Exists(QUtils.iniCfgFile))
                    QUtils.gamePathSet = QUtils.ShowGamePathDialog() == DialogResult.OK;
                else QUtils.gamePathSet = true;*/
                QUtils.gamePathSet = false;

                //Start parsing data from Config file.
                QUtils.ParseConfig();

                //Initialize app data for QEditor.
                if (!status) QUtils.InitEditorAppData();

                //Setting Options from Config.
                appLogsCb.Checked = appLogs;
                autoResetCb.Checked = gameReset;
                autoRefreshGameCb.Checked = gameRefresh;
                editorOnlineCb.Checked = editorOnline;
                updateIntervalTxt.Text = updateTimeInterval.ToString();
                updateCheckerAutomaticOption.Checked = editorUpdateCheck;
                internalCompilerCb.Checked = internalCompiler;
                externalCompilerCb.Checked = externalCompiler;

                //Settings Options for Game config.
                enableMusicCb.Checked = gameMusicEnabled;
                aiIdleCb.Checked = gameAiIdleMode;
                debugModeCb.Checked = gameDebugMode;
                disableWarningsCb.Checked = gameDisableWarns;
                framesTxt.Text = gameFPS.ToString();

                //Genrate scriptId according to Level A.I.
                GenerateAIScriptId(true);
                QUtils.aiScriptFiles.Clear();

                //Check if Notepad++ is installed.
                QUtils.nppInstalled = QUtils.CheckAppInstalled("notepad++.exe", "--help");
                if (!QUtils.nppInstalled) QUtils.nppInstalled = QUtils.LocateExecutable("notepad++.exe") != null;

            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }
        delegate void SetTextCallback(string text);

        internal void SetStatusText(string text)
        {
            try
            {
                if (this.statusTxt.InvokeRequired)
                {
                    var statusCallBack = new SetTextCallback(SetStatusText);
                    this.Invoke(statusCallBack, new object[] { text });
                }
                else
                {
                    statusTxt.Text = null;
                    statusTxt.Text = text;
                }
            }
            catch (Exception)
            {
                statusTxt.Text = text;
            }
        }

        private static HumanAi ReadHumanAiJSON(string fileName)
        {
            HumanAi humanAi = new HumanAi();
            try
            {
                humanAi = JsonConvert.DeserializeObject<HumanAi>(File.ReadAllText(fileName));
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Additional text encountered"))
                    QUtils.ShowError("Error occurred while reading HumanAI data from JSON file");
                else
                    QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return humanAi;
        }


        private void InitEditorPaths(int level)
        {
            try
            {
                if (level <= 0)
                {
                    QUtils.AddLog("IGIEditorUI.InitEditorPaths", $"Invalid level [{level}] received, defaulting to 1");
                    level = 1;
                }
                QUtils.gamePath = QUtils.cfgGamePath + level;
                inputQvmPath = QUtils.cfgQvmPath + level + "\\" + QUtils.objectsQvm;
                inputQscPath = QUtils.cfgQscPath + level + "\\" + QUtils.objectsQsc;
                QUtils.graphsPath = QUtils.cfgGamePath + level + "\\" + "graphs";
                QUtils.gGameLevel = level;
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "level: " + level + " Game Path: '" + gamePath + "' QvmPath: '" + inputQvmPath + "' QscPath: '" + inputQscPath + "' Graphs Path: '" + graphsPath + "'");
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void LoadGameProfile()
        {
            try
            {
                if (!QUtils.gameProfileLoaded)
                {
                    string playerActiveName = QInternals.Player_ActiveName();
                    string playerActiveMission = QInternals.Player_ActiveMission();

                    string gameProfileText = "Gamer: " + playerActiveName + " Mission: " + playerActiveMission;
                    gameProfileNameLbl.Text = playerActiveName;
                    gameProfileMissionLbl.Text = playerActiveMission;
                    QUtils.gameProfileLoaded = true;
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void LoadLevelDetails(int level)
        {
            try
            {
                //load level Description.
                levelNameLbl.Text = QMission.GetMissionInfo(level);
                var imgPath = "mission" + level + QUtils.FileExtensions.Png;
                var imgTmpPath = QUtils.cachePathAppImages + "\\" + imgPath;

                //Load level image from Cache.
                if (File.Exists(imgTmpPath))
                {
                    using (var bmpTemp = new Bitmap(imgTmpPath))
                    {
                        levelImgBox.Image = new Bitmap(bmpTemp);
                    }
                }

                //Load level image from Web.
                else
                {
                    QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Downloading resource please wait...");
                    var imgUrl = "/" + QServer.resourceDir + "/" + "mission_" + level + QUtils.FileExtensions.Jpg;
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Downloading resource URL: '" + imgUrl + "'");
                    QServer.Download(imgUrl, imgPath, imgTmpPath);
                    //LoadImgBoxWeb(imgUrl, levelImgBox);
                    levelImgBox.Refresh();
                    QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Downloading resource done");
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void addWeaponBtn_Click(object sender, EventArgs e)
        {
            string weaponName = null, weaponModel = null, weaponStatus = null;
            int weaponIndex = 0;
            try
            {
                if (liveEditorCb.Checked)
                {
                    weaponIndex = weaponList[weaponDD.SelectedIndex].Values.ElementAt(0);
                    weaponName = weaponList[weaponDD.SelectedIndex].Keys.ElementAt(0);
                    QInternals.WeaponPickup(weaponIndex.ToString());
                    weaponStatus = "Weapon " + weaponName + " added successfully.";
                }
                else
                {
                    var weaponsSize = 1;
                    if (allWeaponsCb.Checked) weaponsSize = weaponList.Count;
                    else if (markWeaponsCb.Checked) weaponsSize = weaponMarkedIds.Count;

                    string qscData = null;
                    for (int idx = 0; idx < weaponsSize; ++idx)
                    {
                        try
                        {
                            idx = (weaponsSize == 1) ? weaponDD.SelectedIndex : idx;
                            int index = (markWeaponsCb.Checked) ? weaponMarkedIds[idx] : idx;

                            weaponModel = QUtils.weaponId + weaponList[index].Keys.ElementAt(0);
                            weaponName = weaponList[index].Keys.ElementAt(0);
                            int weaponAmmo = 999;
                            if (!String.IsNullOrEmpty(weaponAmmoTxt.Text))
                                weaponAmmo = Convert.ToInt32(weaponAmmoTxt.Text);

                            qscData = QHuman.AddWeapon(weaponModel, weaponAmmo, true);
                            if (!String.IsNullOrEmpty(qscData))
                                QUtils.SaveFile(qscData);
                        }
                        catch (Exception ex) { }
                    }

                    if (markWeaponsCb.Checked) { markWeaponsCb.Checked = false; weaponMarkedIds.Clear(); }
                    if (!String.IsNullOrEmpty(qscData))
                        compileStatus = QCompiler.Compile(qscData, QUtils.gamePath, false, allWeaponsCb.Checked);

                    weaponName = (allWeaponsCb.Checked) ? "ALL WEAPONS" : weaponName;
                    if (compileStatus) weaponStatus = "Weapon " + weaponName + " added successfully";
                }
                if (compileStatus)
                {
                    QInternals.StatusMessageShow(weaponStatus);
                    SetStatusText(weaponStatus);
                }
                else
                {
                    weaponStatus = "Weapon " + weaponName + " adding error";
                    SetStatusText(weaponStatus);
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
                QUtils.ShowError("Error adding Weapon '" + weaponName + "' to level");
            }
        }

        private void addBuildingBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var buildingName = QUtils.buildingList[buildingSelectDD.SelectedIndex].Keys.ElementAt(0);
                var buildingModel = QUtils.buildingList[buildingSelectDD.SelectedIndex].Values.ElementAt(0);
                var buildingPos = QUtils.GetViewPortPos(); //QHuman.GetPositionInMeter();

                if (liveEditorCb.Checked)
                {
                    QInternals.MEF_ModelRestore();
                    SetStatusText("Buildiing " + buildingName + " restored successfully");
                    return;
                }

#if !DEV_MODE
                if (!editorModeCb.Checked)
                {
                    var result = QUtils.ShowEditModeDialog();
                    if (result) editorModeCb.Checked = true;
                    else return;
                }
#endif




                bool hasOrientation = String.IsNullOrEmpty(alphaTxt.Text) && String.IsNullOrEmpty(betaTxt.Text) && String.IsNullOrEmpty(gammaTxt.Text);
                if (buildingPos.x != 0.0f || buildingPos.y != 0.0f)
                {
                    AddRigidObject(buildingModel, false, buildingPos, !hasOrientation);
                    if (compileStatus) SetStatusText("Buildiing " + buildingName + " added successfully");
                    string buildingInfoMsg = buildingName + " Model: " + buildingModel + " Added.";
                    QInternals.StatusMessageShow(buildingInfoMsg);
                }
                else
                {
                    SetStatusText("Error: Buildiing positions are invalid.");
                }

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index was out of range"))
                {
                    QUtils.ShowError("No Building or Object have been selected to perfom this operation");
                }
                else
                    QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void removeBuildingBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var buildingModel = QUtils.buildingList[buildingSelectDD.SelectedIndex].Values.ElementAt(0);
                var buildingName = QUtils.buildingList[buildingSelectDD.SelectedIndex].Keys.ElementAt(0);

                if (String.IsNullOrEmpty(buildingModel)) return;

                if (liveEditorCb.Checked)
                {
                    QInternals.MEF_ModelRemove(buildingModel);
                    SetStatusText("Buildiing " + buildingName + " removed successfully");
                }
                else
                {
                    var qscData = QUtils.LoadFile();
                    qscData = QObjects.RemoveObject(qscData, buildingModel, true, false);
                    compileStatus = QCompiler.CompileEx(qscData);
                    if (compileStatus)
                        SetStatusText("Buildiing " + buildingName + " removed successfully");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index was out of range"))
                {
                    QUtils.ShowError("No Building or Object have been selected to perfom this operation");
                }
                else
                    QUtils.ShowError(ex.Message ?? ex.StackTrace);
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }


        private void addObjectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var objectRigidModel = QUtils.objectRigidList[objectSelectDD.SelectedIndex].Values.ElementAt(0);
                var objectRigidName = QUtils.objectRigidList[objectSelectDD.SelectedIndex].Keys.ElementAt(0);
                var objectPos = QUtils.GetViewPortPos();//QHuman.GetPositionInMeter();
                bool hasOrientation = String.IsNullOrEmpty(alphaTxt.Text) && String.IsNullOrEmpty(betaTxt.Text) && String.IsNullOrEmpty(gammaTxt.Text);

                if (liveEditorCb.Checked)
                {
                    QInternals.MEF_ModelRestore();
                    SetStatusText("Object " + objectRigidName + " restored successfully");
                    return;
                }

#if !DEV_MODE
                if (!editorModeCb.Checked)
                {
                    var result = QUtils.ShowEditModeDialog();
                    if (result) editorModeCb.Checked = true;
                    else return;
                }
#endif


                if (objectPos.x != 0.0f || objectPos.y != 0.0f)
                {
                    AddRigidObject(objectRigidModel, true, objectPos, !hasOrientation);
                    if (compileStatus) SetStatusText("Object " + objectRigidName + " added successfully");
                }
                else
                {
                    SetStatusText("Error: Object position is invalid.");
                }


            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index was out of range"))
                    QUtils.ShowError("No Building or Object have been selected to perfom this operation.");
                else
                    QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void removeObjectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var objectRigidModel = QUtils.objectRigidList[objectSelectDD.SelectedIndex].Values.ElementAt(0);
                var objectRigidName = QUtils.objectRigidList[objectSelectDD.SelectedIndex].Keys.ElementAt(0);

                if (String.IsNullOrEmpty(objectRigidModel)) return;

                if (liveEditorCb.Checked)
                {
                    QInternals.MEF_ModelRemove(objectRigidModel);
                    SetStatusText("Object " + objectRigidName + " removed successfully");
                }
                else
                {
                    var qscData = QUtils.LoadFile();
                    qscData = QObjects.RemoveObject(qscData, objectRigidModel, true, false);
                    compileStatus = QCompiler.CompileEx(qscData);
                    if (compileStatus)
                        SetStatusText("Object " + objectRigidName + " removed successfully");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index was out of range"))
                {
                    QUtils.ShowError("No Building or Object have been selected to perfom this operation");
                }
                else
                    QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void minimizeBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void resetLevelBtn_Click(object sender, EventArgs e)
        {
            QUtils.ResetCurrentLevel(true);
        }

        private void resetAllLevelsBtn_Click(object sender, EventArgs e)
        {
            for (int level = 1; level <= GAME_MAX_LEVEL; ++level)
            {
                inputQvmPath = QUtils.cfgQvmPath + level + "\\" + QUtils.objectsQvm;
                QUtils.RestoreLevel(level);
            }
            QMemory.RestartLevel(true);
        }

        private void xPosLbl_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(xPosLbl.Text))
            {
                Clipboard.SetText(xPosLbl.Text);
                SetStatusText("X-Position Copied successfully");
            }
        }

        private void yPosLbl_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(yPosLbl.Text))
            {
                Clipboard.SetText(yPosLbl.Text);
                SetStatusText("Y-Position Copied successfully");
            }
        }

        private void zPosLbl_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(zPosLbl.Text))
            {
                Clipboard.SetText(zPosLbl.Text);
                SetStatusText("Z-Position Copied successfully");
            }
        }


        private void posOffCb_CheckedChanged(object sender, EventArgs e)
        {
            if (posOffCb.Checked) posMeterCb.Checked = posCurrentCb.Checked = false;
        }

        private void posMeterCb_CheckedChanged(object sender, EventArgs e)
        {
            if (posMeterCb.Checked) posOffCb.Checked = posCurrentCb.Checked = false;
        }

        private void posCurrentCb_CheckedChanged(object sender, EventArgs e)
        {
            if (posCurrentCb.Checked) posOffCb.Checked = posMeterCb.Checked = false;
        }

        private void RefreshGame(bool findGame, bool attachInternals)
        {
            try
            {
                if (findGame)
                {
                    QUtils.gameFound = QMemory.FindGame();
                    if (!QUtils.gameFound)
                    {
                        QMemory.StartGame();
                        SetStatusText("Game not found! starting new game");
                        QUtils.Sleep(8);

                        QUtils.gGameLevel = gameLevel = QMemory.GetRunningLevel();
                        levelStartTxt.Text = gameLevel.ToString();
                        InitEditorPaths(QUtils.gGameLevel);
                        QUtils.gameFound = true;
                    }
                }

                SetStatusText("Game found success");
                QUtils.gGameLevel = QUtils.gameFound ? QMemory.GetRunningLevel() : Convert.ToInt32(levelStartTxt.Value);
                LoadLevelDetails(gameLevel);
                RefreshUIComponents(gameLevel);
                CleanUpAiFiles();
                InitEditorPaths(gameLevel);

                if (autoResetCb.Checked)
                {
                    var dlgResult = QUtils.ShowDialog("Auto-Reset option is selected and will reset your level\nDo you want to continue?");
                    if (dlgResult == DialogResult.Yes)
                    {
                        QUtils.ResetScriptFile(gameLevel);
                        QUtils.RestoreLevel(gameLevel);
                    }

                }

                // Attach internals if selected.
                if (attachInternals)
                {
                    QUtils.attachStatus = QUtils.CheckInternalsAttached();
                    if (!QUtils.attachStatus) QUtils.AttachInternals();
                }
                QUtils.graphAreas.Clear();

            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void refreshGame_Click(object sender, EventArgs e)
        {
            RefreshGame(true, true);
        }

        private void updateObjPosition_Click(object sender, EventArgs e)
        {
            try
            {
                var buildingModel = QUtils.buildingList[buildingPosDD.SelectedIndex].Values.ElementAt(0);
                var buildingName = QUtils.buildingList[buildingPosDD.SelectedIndex].Keys.ElementAt(0);

                var objectRigidModel = QUtils.objectRigidList[objectPosDD.SelectedIndex].Values.ElementAt(0);
                var objectRigidName = QUtils.objectRigidList[objectPosDD.SelectedIndex].Keys.ElementAt(0);

                string model = isBuildingDD ? buildingModel : objectRigidModel;

                double xpos, ypos, zpos;
                string qscData = null;

                if (posCurrentCb.Checked)
                {
                    var meterPos = QUtils.GetViewPortPos(); QHuman.GetPositionInMeter();

                    xPosObjTxt.Text = meterPos.x.ToString();
                    yPosObjTxt.Text = meterPos.y.ToString();
                    zPosObjTxt.Text = meterPos.z.ToString();
                }

                xpos = Convert.ToDouble(xPosObjTxt.Text);
                ypos = Convert.ToDouble(yPosObjTxt.Text);
                zpos = Convert.ToDouble(zPosObjTxt.Text);

                var objectPos = new Real64(xpos, ypos, zpos);
                if (posOffCb.Checked)
                    qscData = QObjects.UpdatePositionOffset(model, ref objectPos);
                else if (posMeterCb.Checked)
                    qscData = QObjects.UpdatePositionMeter(model, ref objectPos);
                else if (posCurrentCb.Checked)
                    qscData = QObjects.UpdatePositionMeter(model, ref objectPos);

                if (!String.IsNullOrEmpty(qscData))
                    compileStatus = QCompiler.Compile(qscData, QUtils.gamePath, false, true, true);

                if (compileStatus)
                {
                    string statusText = (isBuildingDD ? (buildingName) : (objectRigidName)) + " updated successfully";
                    SetStatusText(statusText);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index was out of range"))
                    QUtils.ShowError("No building or object have been selected to perfom this operation");
                else if (ex.Message.Contains("not in a correct format"))
                    QUtils.ShowError("Position parameters are empty or invalid");
                else
                    QUtils.ShowError(ex.Message ?? ex.StackTrace);
            }
        }

        private void updateObjOrientation_Click(object sender, EventArgs e)
        {
            try
            {
                var buildingModel = QUtils.buildingList[buildingPosDD.SelectedIndex].Values.ElementAt(0);
                var buildingName = QUtils.buildingList[buildingPosDD.SelectedIndex].Keys.ElementAt(0);

                var objectRigidModel = QUtils.objectRigidList[objectPosDD.SelectedIndex].Values.ElementAt(0);
                var objectRigidName = QUtils.objectRigidList[objectPosDD.SelectedIndex].Keys.ElementAt(0);

                string model = isBuildingDD ? buildingModel : objectRigidModel;

                float alpha, beta, gamma;
                alpha = float.Parse(alphaTxt.Text);
                beta = float.Parse(betaTxt.Text);
                gamma = float.Parse(gammaTxt.Text);
                string qscData = null;

                var orientation = new Real32(alpha, beta, gamma);
                qscData = QObjects.UpdateOrientation(model, ref orientation);

                if (!String.IsNullOrEmpty(qscData))
                    compileStatus = QCompiler.Compile(qscData, QUtils.gamePath, false, true, true);

                if (compileStatus)
                {
                    string statusText = (isBuildingDD ? (buildingName) : (objectRigidName)) + " orientation updated successfully";
                    SetStatusText(statusText);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index was out of range"))
                    QUtils.ShowError("No Building or Object have been selected to perfom this operation");
                else if (ex.Message.Contains("not in a correct format"))
                    QUtils.ShowError("Position parameters are empty or invalid");
                else
                    QUtils.ShowError(ex.Message ?? ex.StackTrace);
            }
        }

        private void humanPosOffCb_CheckedChanged(object sender, EventArgs e)
        {
            if (humanPosOffCb.Checked) humanPosMeterCb.Checked = resetPosCb.Checked = false;
        }

        private void humanPosMeterCb_CheckedChanged(object sender, EventArgs e)
        {
            if (humanPosMeterCb.Checked) humanPosOffCb.Checked = resetPosCb.Checked = false;
        }

        private void updateHumaPosition_Click(object sender, EventArgs e)
        {
            try
            {
                string qscData = null;

                var humanPos = new Real64();
                if (resetPosCb.Checked)
                {
                    humanPos = QHuman.GetHumanTaskList(true).qtask.position;
                    xPosTxt_H.Text = humanPos.x.ToString();
                    yPosTxt_H.Text = humanPos.y.ToString();
                    zPosTxt_H.Text = humanPos.z.ToString();
                }
                else
                {
                    Double xpos = Convert.ToDouble(xPosTxt_H.Text);
                    Double ypos = Convert.ToDouble(yPosTxt_H.Text);
                    Double zpos = Convert.ToDouble(zPosTxt_H.Text);
                    humanPos = new Real64(xpos, ypos, zpos);
                }

                if (humanPosOffCb.Checked) qscData = QHuman.UpdatePositionOffset(humanPos);

                else if (humanPosMeterCb.Checked || resetPosCb.Checked) qscData = QHuman.UpdatePositionInMeter(humanPos);

                if (!string.IsNullOrEmpty(qscData)) compileStatus = QCompiler.Compile(qscData, QUtils.gamePath, false, true, false);

                if (compileStatus) SetStatusText("Human position updated successfully");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not in a correct format"))
                    QUtils.ShowError("Position parameters are empty or invalid");
                else
                    QUtils.ShowError(ex.Message ?? ex.StackTrace);
            }
        }

        private void weaponSelectDD_SelectedValueChanged(object sender, EventArgs e)
        {
            int weaponIdx = weaponDD.SelectedIndex;
            PopulateWeaponDD(weaponDD.SelectedIndex, weaponImgBox);

            if (markWeaponsCb.Checked)//Mark Weapon if selected.
            {
                weaponMarkedIds.Add(weaponIdx);
                string weaponName = QUtils.weaponList[weaponIdx].Keys.ElementAt(0);
                SetStatusText("Weapon '" + weaponName + "' Marked");
                int weaponAmmo = Convert.ToInt32(weaponAmmoTxt.Value);

                //Add Weapon to Group list.
                var weaponGroup = new WeaponGroup();
                weaponGroup.Weapon = weaponName;
                weaponGroup.Ammo = weaponAmmo;
                weaponsGroupList.Add(weaponGroup);
            }
        }

        private void PopulateWeaponDD(int index, PictureBox imgBox)
        {
            string weaponModel = null, weaponName = null, imgUrl = null, imgPath = null;
            try
            {
                weaponName = QUtils.weaponList[index].Keys.ElementAt(0);
                //Weapon image paths.
                //imgUrl = baseImgUrl + weaponsImgUrl[index];
                imgPath = weaponName + QUtils.FileExtensions.Png;
                var imgTmpPath = QUtils.cachePathAppImages + "\\" + imgPath;

                //Load image from Cache.
                if (File.Exists(imgTmpPath))
                {
                    using (var bmpTemp = new Bitmap(imgTmpPath))
                    {
                        imgBox.Image = new Bitmap(bmpTemp);
                    }
                }

                //Load image from Web.
                else
                {
                    QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Downloading resource please wait...");
                    imgUrl = "/" + QServer.resourceDir + "/" + weaponName + QUtils.FileExtensions.Jpg;
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Downloading resource URL: '" + imgUrl + "'");
                    QServer.Download(imgUrl, imgPath, imgTmpPath);
                    imgBox.Refresh();
                    QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Downloading resource done");
                }
            }
            catch (Exception ex)
            {
                imgBox.Image = null;
            }
        }

        private void removeWeaponBtn_Click(object sender, EventArgs e)
        {
            string qscData = null, weaponName = null;
            try
            {
                if (allWeaponsCb.Checked)
                {
                    qscData = QHuman.RemoveWeapons();
                }
                else
                {
                    var weaponModel = QUtils.weaponId + QUtils.weaponList[weaponDD.SelectedIndex].Keys.ElementAt(0);
                    weaponName = weaponList[weaponDD.SelectedIndex].Keys.ElementAt(0);
                    qscData = QHuman.RemoveWeapon(weaponModel, true);
                }

                if (!String.IsNullOrEmpty(qscData))
                    compileStatus = QCompiler.Compile(qscData, QUtils.gamePath, false, true, true);
                if (compileStatus) SetStatusText("Weapon " + weaponName + " removed successfully");
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void exportObjectsBtn_Click(object sender, EventArgs e)
        {
            var qtaskList = QTask.GetQTaskList(false, true);

            switch (exportObjectsDD.SelectedIndex)
            {
                case 0:
                    QUtils.ExportCSV(QUtils.objects + QUtils.FileExtensions.Csv, qtaskList);
                    break;
                case 1:
                    QUtils.ExportXML(QUtils.objects + QUtils.FileExtensions.Xml);
                    break;
                case 2:
                    QUtils.ExportJson(QUtils.objects + QUtils.FileExtensions.Json);
                    break;
            }
            SetStatusText("Data exported success");
        }

        private void objectSelectDD_Click(object sender, EventArgs e)
        {
            isObjectDD = true;
            isBuildingDD = false;
        }

        private void clearCacheBtn_Click(object sender, EventArgs e)
        {
            string cacheDlg = "Clearing cache will delete all resources/data application uses.\nDo you want to continue?";
            var dlgResult = QUtils.ShowDialog(cacheDlg, "WARNING");

            if (dlgResult == DialogResult.Yes)
            {
                if (Directory.Exists(QUtils.cachePath))
                {
                    QUtils.DirectoryDelete(QUtils.cachePath);
                    SetStatusText("Application cache cleared.");
                    CreateCacheDir();//Create empty cache directory after.
                }
            }
        }

        private void appLogsCb_CheckedChanged(object sender, EventArgs e)
        {
            appLogsCb.Checked = !appLogsCb.Checked;

            if (appLogsCb.Checked)
            {
                QUtils.appLogs = true;
                QUtils.EnableLogs();
                SetStatusText("Application Logs enabled");
            }
            else
            {
                QUtils.appLogs = false;
                QUtils.DisableLogs();
                SetStatusText("Application Logs disabled");
            }
        }

        private void autoResetCb_CheckedChanged(object sender, EventArgs e)
        {
            autoResetCb.Checked = !autoResetCb.Checked;
            if (autoResetCb.Checked)
            {
                QUtils.gameReset = true;
                SetStatusText("Auto reset level enabled");
            }
            else
            {
                QUtils.gameReset = false;
                SetStatusText("Auto reset level disabled");
            }
        }

        private void aboutBtn_Click(object sender, EventArgs e)
        {
            QUtils.ShowInfo(QUtils.aboutStr, "ABOUT");
            string readmeCmd = (QUtils.nppInstalled ? "notepad++  -nosession -notabbar -alwaysOnTop -multiInst -lhaskell \"" : "notepad \"") + QUtils.editorCurrPath + "\\" + QUtils.editorReadme + QUtils.FileExtensions.Text + "\"";
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, readmeCmd);
            QUtils.ShellExec(readmeCmd);
        }

        private void objectIDTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var modelId = modelIDTxt.Text;
                var modelName = QObjects.FindModelName(modelId);
                if (!String.IsNullOrEmpty(modelName)) modelNameOutLbl.Text = modelName;
            }
        }

        private void tabContainer_Selected(object sender, TabControlEventArgs e)
        {
            //Level Editor
            if (e.TabPage.Name == "levelEditor")
            {
                try
                {
                    //Nothing to update at the moment.
                }
                catch (Exception) { }
            }

            //Object Editor
            else if (e.TabPage.Name == "objectEditor")
            {
                try
                {
                    SetStatusText(e.TabPage.Name.ToLower() + " supports live editor features.");
                    qtaskList = QTask.GetQTaskList(false, false, true);

                    buildingsCount = qtaskList.Count(b => b.name.Contains("Building"));
                    rigidObjCount = qtaskList.Count(o => o.name.Contains("EditRigidObj"));

                    objectsRemoveTxt.Maximum = objectsResetTxt.Maximum = Convert.ToInt32(rigidObjCount);
                    buildingsRemoveTxt.Maximum = buildingsResetTxt.Maximum = Convert.ToInt32(buildingsCount);
                }
                catch (Exception ex) { QUtils.LogException(e.TabPage.Name.ToUpper(), ex); }
            }

            //A.I Editor.
            else if (e.TabPage.Name == "aiEditor")
            {
                try
                {
                    UpdateUIComponent(aiGraphIdDD, QUtils.aiGraphIdStr);
                    if (aiTypeDD.Items.Count > 0 && aiModelSelectDD.Items.Count > 0 && aiWeaponDD.Items.Count > 0)
                        aiTypeDD.SelectedIndex = aiModelSelectDD.SelectedIndex = aiWeaponDD.SelectedIndex = 0;
                }
                catch (Exception ex) { QUtils.LogException(e.TabPage.Name.ToUpper(), ex); }
            }

            //Weapon Editor.
            else if (e.TabPage.Name == "weaponEditor")
            {
                try
                {
                    if (weaponDD.Items.Count > 0)
                        weaponDD.SelectedIndex = 0;
                    SetStatusText(e.TabPage.Name.ToLower() + " supports live editor features.");
                }
                catch (Exception ex) { QUtils.LogException(e.TabPage.Name.ToUpper(), ex); }
            }


            //Graph Editor.
            else if (e.TabPage.Name == "graphEditor")
            {
                try
                {
                    if (!gameFound)
                    {
                        //Init AI Graph list.
                        int level = Convert.ToInt32(levelStartTxt.Text);
                        bool initialInit = true;
                        QGraphs.GraphLevelInit(level, showAllGraphsCb.Checked, initialInit, ref aiGraphIdDD);
                    }


                    UpdateUIComponent(graphIdDD, QUtils.aiGraphIdStr);
                    UpdateUIComponent(nodeIdDD, QUtils.aiGraphNodeIdStr);
                    if (graphIdDD.Items.Count > 0 && nodeIdDD.Items.Count > 0)
                        graphIdDD.SelectedIndex = nodeIdDD.SelectedIndex = 0;
                }
                catch (Exception ex) { QUtils.LogException(e.TabPage.Name.ToUpper(), ex); }
            }

            //Position Editor
            else if (e.TabPage.Name == "positionEditor")
            {
                try
                {
                    UpdateUIComponent(buildingPosDD, QUtils.buildingListStr);
                    UpdateUIComponent(objectPosDD, QUtils.objectRigidListStr);
                }
                catch (Exception ex) { QUtils.LogException(e.TabPage.Name.ToUpper(), ex); }
            }

            //Human Editor
            else if (e.TabPage.Name == "humanEditor")
            {
                //movementSpeedTxt.Maximum  = forwardJumpTxt.Maximum = upwardJumpTxt.Maximum = inAirSpeedTxt.Maximum = Convert.ToDecimal(float.MaxValue);
            }


            //MissionEditor Editor
            else if (e.TabPage.Name == "missionEditor")
            {
                //if (editorOnline) UpdateUIComponent(missionsOnlineDD, QUtils.missionNameListStr);
            }

            //Misc Editor
            else if (e.TabPage.Name == "miscEditor")
            {

            }


        }

        private void removeObjsBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var itemsCount = Convert.ToInt32(objectsRemoveTxt.Text);
                int itemCount = 0;
                if (liveEditorCb.Checked)
                {
                    foreach (var objectRigid in QUtils.objectRigidList)
                    {
                        string modelName = objectRigid.Keys.ElementAt(0);
                        string modelId = objectRigid.Values.ElementAt(0);
                        if (itemCount >= itemsCount) break;

                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Object model " + modelName + "[" + modelId + "] removed via Live Editor");
                        QInternals.MEF_ModelRemove(modelId);
                        SetStatusText("Object " + modelName + " removed successfully");
                        QUtils.Sleep(1f);
                        itemCount++;
                    }
                }
                else
                {
                    var qscData = QUtils.LoadFile();
                    qscData = QObjects.RemoveAllObjects(qscData, false, true, itemsCount, true);
                    compileStatus = QCompiler.CompileEx(qscData);
                    if (compileStatus)
                        SetStatusText(itemsCount + " Objects removed successfully");
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void removeBuildingsBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var itemsCount = Convert.ToInt32(buildingsRemoveTxt.Text);
                int itemCount = 0;
                if (liveEditorCb.Checked)
                {
                    foreach (var building in QUtils.buildingList)
                    {
                        string modelName = building.Keys.ElementAt(0);
                        string modelId = building.Values.ElementAt(0);
                        if (modelId == "472_01_1") continue;

                        if (itemCount >= itemsCount) break;

                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Building model " + modelName + "[" + modelId + "] removed via Live Editor");
                        QInternals.MEF_ModelRemove(modelId);
                        SetStatusText("Buildiing " + modelName + " removed successfully");
                        QUtils.Sleep(1f);
                        itemCount++;
                    }
                }

                else
                {
                    var qscData = QUtils.LoadFile();

                    qscData = QObjects.RemoveAllObjects(qscData, true, false, itemsCount, true);
                    compileStatus = QCompiler.CompileEx(qscData);
                    if (compileStatus)
                        SetStatusText(itemsCount + " Buildings removed successfully");
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void buildingSelectDD_Click(object sender, EventArgs e)
        {
            isBuildingDD = true;
            isObjectDD = false;
        }

        private void restartLevelBtn_Click(object sender, EventArgs e)
        {
            QInternals.RestartLevel();

            //Genrate random scriptId according to Level A.I.
            GenerateAIScriptId();
        }

        //Testing phase - remove in final build
        private void compileBtn_Click(object sender, EventArgs e)
        {
            string qscData = QUtils.LoadFile();
            QCompiler.CompileEx(qscData);

            if (compileStatus) SetStatusText("Compile success");
        }


        private void resetBuildingsBtn_Click(object sender, EventArgs e)
        {
            if (liveEditorCb.Checked)
            {
                QInternals.MEF_ModelRestore();
                return;
            }

            SetStatusText("Resetting please wait...");
            qtaskList = QTask.GetQTaskList(false, false, true);
            var qTaskListCount = qtaskList.Count();
            var itemsCount = String.IsNullOrEmpty(buildingsResetTxt.Text) ? 0 : Convert.ToInt32(buildingsResetTxt.Text);
            int qTaskCount = 0;

            if (itemsCount > qTaskListCount)
            {
                QUtils.ShowError("Items count must be less than max items.");
                return;
            }

            for (int i = 0; i < qTaskListCount; i++)
            {
                var qtask = qtaskList[i];
                if (qtask.name == "\"Building\"")
                {
                    if (qTaskCount == itemsCount)
                        break;

                    var buildingModel = qtask.model;
                    AddRigidObject(buildingModel, false, qtask.position, true, -1, qtask.note, true, qtask.orientation.alpha, qtask.orientation.beta, qtask.orientation.gamma);
                    qTaskCount++;
                }
            }

            if (compileStatus)
                SetStatusText(itemsCount + " Buildings reset success");
        }

        private void cutsceneRemoveBtn_Click(object sender, EventArgs e)
        {
            var qscData = QMisc.RemoveCutscene();
            if (!String.IsNullOrEmpty(qscData)) compileStatus = QCompiler.CompileEx(qscData);

            if (compileStatus) SetStatusText("Cutscene removed success.");
        }

        private void objectPosDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            isObjectDD = true;
            isBuildingDD = false;
        }

        private void buildingPosDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            isBuildingDD = true;
            isObjectDD = false;
        }

        private void updateHumanSpeedBtn_Click(object sender, EventArgs e)
        {
            try
            {
                QUtils.movSpeed = Convert.ToDouble(movementSpeedTxt.Text);
                QUtils.forwardSpeed = Convert.ToDouble(forwardJumpTxt.Text);
                QUtils.upwardSpeed = Convert.ToDouble(upwardJumpTxt.Text);
                QUtils.inAirSpeed = Convert.ToDouble(inAirSpeedTxt.Text);
                QHuman.UpdateHumanPlayerSpeed(QUtils.movSpeed, QUtils.forwardSpeed, QUtils.upwardSpeed, QUtils.inAirSpeed);
                QInternals.StatusMessageShow("Humanplayer speed updated.");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not in a correct format"))
                    QUtils.ShowError("Human speed parameters are empty or invalid");
                else
                    QUtils.ShowError(ex.Message ?? ex.StackTrace);
            }
        }

        private void updateHumanHealthBtn_Click(object sender, EventArgs e)
        {
            try
            {
                QUtils.healthScale = Convert.ToDouble(damageScaleTxt.Text);
                QUtils.healthScaleFence = Convert.ToDouble(damageScaleFenceTxt.Text);
                QUtils.healthScaleFall = Convert.ToInt32(damageScaleFallTxt.Text);
                QHuman.UpdateHumanPlayerHealth(QUtils.healthScale, QUtils.healthScaleFence, healthScaleFall);
                QInternals.StatusMessageShow("Humanplayer health updated.");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not in a correct format"))
                    QUtils.ShowLogError(MethodBase.GetCurrentMethod().Name, "Human health parameters are empty or invalid");
                else
                    QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }

        }

        private void updateHumanPeekBtn_Click(object sender, EventArgs e)
        {
            try
            {
                QUtils.peekLRLen = Convert.ToDouble(peekLRTxt.Text);
                QUtils.peekCrouchLen = Convert.ToDouble(peekCrouchTxt.Text);
                QUtils.peekTime = Convert.ToDouble(peekTimeTxt.Text);
                QHuman.UpdateHumanPlayerPeek(QUtils.peekLRLen, QUtils.peekCrouchLen, QUtils.peekTime);
                QInternals.StatusMessageShow("Humanplayer peek updated.");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not in a correct format"))
                    QUtils.ShowError("Human Peek parameters are empty or invalid");
                else
                    QUtils.ShowError(ex.Message ?? ex.StackTrace);
            }
        }

        private void resetHumanBtn_Click(object sender, EventArgs e)
        {
            string humanFileName = QUtils.cfgHumanplayerPathQvm + @"\humanplayer.qvm";
            var outputHumanPlayerPath = QUtils.gameAbsPath + "\\humanplayer\\";
            var moveCmd = "copy " + humanFileName + " " + outputHumanPlayerPath + " /y";
            QUtils.ShellExec(moveCmd, true);

            //Reset params in UI.
            movementSpeedTxt.Text = "1.75";
            forwardJumpTxt.Text = "17.5";
            upwardJumpTxt.Text = "27";
            inAirSpeedTxt.Text = "0.5";
            peekLRTxt.Text = peekCrouchTxt.Text = "0.8500000238418579";
            peekTimeTxt.Text = " 0.25";
            damageScaleTxt.Text = "3.0";
            damageScaleFenceTxt.Text = "0.5";
            damageScaleFallTxt.Value = 0;
            QMemory.UpdateHumanHealth(HEALTH_ACTION.RESTORE);
            QInternals.HumanplayerLoad();
            QMemory.SetStatusMsgText("Human parameters reset success");
        }

        private void readHumanBtn_Click(object sender, EventArgs e)
        {
            var humanPlayerFile = QUtils.cfgHumanplayerPathQsc + @"\humanplayer" + QUtils.FileExtensions.Qsc;
            string humanFileName = "humanplayer.qsc";
            string humanPlayerData = QUtils.LoadFile(humanPlayerFile);

            var outputHumanPlayerPath = QUtils.gameAbsPath + "\\humanplayer\\";

            QUtils.SaveFile(humanFileName, humanPlayerData);
            bool status = QCompiler.Compile(humanFileName, outputHumanPlayerPath, 0x0);
            QUtils.FileIODelete(humanFileName);

            if (status)
            {
                Thread.Sleep(1000);
                QInternals.HumanplayerLoad();
                QMemory.SetStatusMsgText("Human parameters set success");
            }
        }

        private void resetObjectsBtn_Click(object sender, EventArgs e)
        {
            if (liveEditorCb.Checked)
            {
                QInternals.MEF_ModelRestore();
                return;
            }

            SetStatusText("Resetting please wait...");
            qtaskList = QTask.GetQTaskList(false, false, true);
            var qTaskListCount = qtaskList.Count();
            var itemsCount = String.IsNullOrEmpty(objectsResetTxt.Text) ? 0 : Convert.ToInt32(objectsResetTxt.Text);
            int qTaskCount = 0;

            if (itemsCount > qTaskListCount)
            {
                QUtils.ShowError("Items count must be less than max items.");
                return;
            }

            for (int i = 0; i < qTaskListCount; i++)
            {
                var qtask = qtaskList[i];
                if (qtask.name == "\"EditRigidObj\"")
                {
                    if (qTaskCount == itemsCount)
                        break;

                    var rigidModel = qtask.model;
                    AddRigidObject(rigidModel, true, qtask.position, true, -1, qtask.note, true, qtask.orientation.alpha, qtask.orientation.beta, qtask.orientation.gamma);
                    qTaskCount++;
                }
            }

            if (compileStatus)
                SetStatusText(itemsCount + " Objects reset success");
        }

        private void startFullScreenGameBtn_Click(object sender, EventArgs e)
        {
            try
            {
                int level = Convert.ToInt32(levelStartTxt.Text);
                gameLevel = Convert.ToInt32(levelStartTxt.Text.ToString());

                RefreshUIComponents(gameLevel);
                InitEditorPaths(gameLevel);
                QUtils.graphAreas.Clear();
                CleanUpAiFiles();
                StartGameLevel(level, false);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void aiModelSelectDD_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                var aiModelName = QAI.GetAiModelNamesList(gameLevel)[aiModelSelectDD.SelectedIndex];
                var aiModelId = QAI.GetAiModelId4Name(aiModelName);

                var imgPath = aiModelName + QUtils.FileExtensions.Png;
                var imgTmpPath = QUtils.cachePathAppImages + "\\" + imgPath;
                var aiModelQualifyName = aiModelName.Contains("_") ? aiModelName.Substring(0, aiModelName.IndexOf("_")) : aiModelName;
                aiModelNameLbl.Text = aiModelQualifyName;

                //Load image from Cache.
                if (File.Exists(imgTmpPath))
                {
                    using (var bmpTemp = new Bitmap(imgTmpPath))
                    {
                        aiImgBox.Image = new Bitmap(bmpTemp);
                    }
                    SetStatusText("Setting resource image success");
                }

                //Load image from Web.
                else
                {
                    QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Downloading resource please wait...");
                    //A.I image paths.
                    var imgUrl = "/" + QServer.resourceDir + "/" + aiModelName + QUtils.FileExtensions.Jpg;
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Downloading image resource: URL : " + imgUrl);
                    QServer.Download(imgUrl, imgPath, imgTmpPath);
                    aiImgBox.Refresh();
                    QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Downloading resource done");
                }
            }
            catch (Exception ex)
            {
                aiImgBox.Image = null;
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void weaponAiDD_SelectedValueChanged(object sender, EventArgs e)
        {
            PopulateWeaponDD(aiWeaponDD.SelectedIndex, weaponAIImgBox);
        }

        private void aiTypeDD_SelectedValueChanged(object sender, EventArgs e)
        {
        }

        private void guardGeneratorCb_CheckedChanged(object sender, EventArgs e)
        {
            maxSpawnsTxt.Enabled = guardGeneratorCb.Checked;
        }

        private void aiGraphIdDD_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                aiGraphId = Int32.Parse(aiGraphIdDD.SelectedItem.ToString());
                string graphArea = QGraphs.GetGraphArea(aiGraphId);
                graphAreaAiLbl.Text = graphArea;
            }

            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void addAiBtn_Click(object sender, EventArgs e)
        {
            SetStatusText("Adding new A.I to level please wait...");
            addAiSoldierWorker.RunWorkerAsync();
        }

        private void customAiCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                string nppCmd = (QUtils.nppInstalled) ? "notepad++ -titleAdd=\"A.I Custom Files\" -nosession -notabbar -alwaysOnTop -multiInst -lcpp " : "notepad ";
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, nppCmd);
                SetStatusText("Add your custom scripts/path for A.I");
                QUtils.ShowInfo("Add your custom scripts for your A.I\n'XXXX' or 'YYYY' are Masking IDs dont replace them.");
                QUtils.ShellExec(nppCmd + QUtils.customScriptPathQEd);
                QUtils.ShowInfo("Add your custom path for your A.I\n'XXXX' or 'YYYY' are Masking IDs dont replace them.");
                QUtils.ShellExec(nppCmd + QUtils.customPatrolPathQEd);
                QUtils.customAiSelected = true;
                maxSpawnsTxt.Enabled = true;
            }
            else
            {
                maxSpawnsTxt.Enabled = false;
                QUtils.customAiSelected = false;
            }
        }

        private void editorOnlineCb_CheckedChanged(object sender, EventArgs e)
        {
            if ((((CheckBox)sender).Checked))
            {
                if (QUtils.IsNetworkAvailable())
                {
                    editorOnline = true;
                    editorOnlineCb.Text = "Online";
                    editorOnlineCb.ForeColor = Green;
                    downloadMissionBtn.Enabled = uploadMissionBtn.Enabled = missionsOnlineDD.Enabled = missionRefreshBtn.Enabled = editorUpdaterBtn.Enabled = updateCheckerAutomaticOption.Enabled = updateIntervalTxt.Enabled = true;
                    InitMissionsOnline(true);
                    SetStatusText("Editor online mode enabled...");
                }
                else
                {
                    editorOnline = false;
                    editorOnlineCb.Text = "Offline";
                    editorOnlineCb.ForeColor = Red;
                    (((CheckBox)sender).Checked) = false;
                    downloadMissionBtn.Enabled = uploadMissionBtn.Enabled = missionsOnlineDD.Enabled = missionRefreshBtn.Enabled = editorUpdaterBtn.Enabled = updateCheckerAutomaticOption.Enabled = updateCheckerAutomaticOption.Checked = updateIntervalTxt.Enabled = false;
                    SetStatusText("Please check your internet connection.");
                }
            }
            else
            {
                editorOnline = false;
                editorOnlineCb.Text = "Offline";
                editorOnlineCb.ForeColor = Red;
                downloadMissionBtn.Enabled = uploadMissionBtn.Enabled = missionsOnlineDD.Enabled = missionRefreshBtn.Enabled = editorUpdaterBtn.Enabled = updateCheckerAutomaticOption.Enabled = updateCheckerAutomaticOption.Checked = updateIntervalTxt.Enabled = false;
                SetStatusText("Editor offline mode enabled...");
            }
        }

        private void removeModelBtn_Click(object sender, EventArgs e)
        {
            try
            {
#if DEV_MODE
                if (!editorModeCb.Checked && !liveEditorCb.Checked)
                {
                    var result = QUtils.ShowEditModeDialog();
                    if (result) editorModeCb.Checked = true;
                    else return;
                }
#endif

                if (liveEditorCb.Checked)
                {
                    var modelId = modelIDTxt.Text;
                    QInternals.MEF_ModelRemove(modelId);
                    var modelName = QObjects.FindModelName(modelId);
                    if (!String.IsNullOrEmpty(modelName)) modelNameOutLbl.Text = modelName;
                    SetStatusText("Object " + modelName + " removed successfully");
                }
                else
                {
                    var modelRegex = @"\d{3}_\d{2}_\d{1}";
                    var valueRegex = Regex.Match(modelIDTxt.Text, modelRegex).Value;
                    if (String.IsNullOrEmpty(valueRegex))
                        QUtils.ShowError("Input Object Id is in wrong format. Check Help for proper format");
                    else
                    {
                        var modelId = modelIDTxt.Text;
                        var qscData = QUtils.LoadFile();
                        qscData = QObjects.RemoveObject(qscData, modelId, true, true);

                        if (!String.IsNullOrEmpty(qscData))
                        {
                            compileStatus = QCompiler.CompileEx(qscData);

                            var modelName = QObjects.FindModelName(modelId);
                            if (!String.IsNullOrEmpty(modelName)) modelNameOutLbl.Text = modelName;

                            if (compileStatus)
                                SetStatusText("Object " + modelName + " removed successfully");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void restoreModelBtn_Click(object sender, EventArgs e)
        {
            if (!editorModeCb.Checked && !liveEditorCb.Checked)
            {
                var result = QUtils.ShowEditModeDialog();
                if (result) editorModeCb.Checked = true;
                else return;
            }

            if (liveEditorCb.Checked)
            {
                var modelId = modelIDTxt.Text;
                QInternals.MEF_ModelRestore();
                var modelName = QObjects.FindModelName(modelId);
                if (!String.IsNullOrEmpty(modelName)) modelNameOutLbl.Text = modelName;
                SetStatusText("Object " + modelName + " restored successfully");
            }
        }

        private void StartGameLevelNow(int gameLevel)
        {
            RefreshUIComponents(gameLevel);
            InitEditorPaths(gameLevel);
            QUtils.graphAreas.Clear();
            CleanUpAiFiles();

            if (liveEditorCb.Checked)
            {
                QUtils.gameFound = QMemory.FindGame();
                if (QUtils.gameFound)
                    QInternals.StartLevel(gameLevel.ToString());
                else
                {
                    SetStatusText("Live Editor - Error game not running.");
                    liveEditorCb.Checked = false;
                    StartGameLevel(gameLevel, true);

                }
            }
            else
                StartGameLevel(gameLevel, true);
        }


        private void startGameBtn_Click(object sender, EventArgs e)
        {
            try
            {
                gameLevel = Convert.ToInt32(levelStartTxt.Text.ToString());
                StartGameLevelNow(gameLevel);
                refreshGame_Click(sender, e);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void disableWarningsCb_CheckedChanged(object sender, EventArgs e)
        {
            disableWarningsCb.Checked = !disableWarningsCb.Checked;
            if (disableWarningsCb.Checked)
            {
                QMemory.DisableGameWarnings();
            }
            QUtils.gameDisableWarns = disableWarningsCb.Checked;
        }

        private void addNodesBtn_Click(object sender, EventArgs e)
        {
            if (nodesObjectsCb.Checked || nodesHilightCb.Checked || nodesInfoCb.Checked)
            {
                var graphIds = new List<int>();

                if (graphsMarkCb.Checked) graphIds = graphdIdsMarked;
                else if (graphsAllCb.Checked) graphIds = aiGraphIdStr;
                else graphIds.Add(Convert.ToInt32(graphIdDD.SelectedValue));
                var graphIdsVal = string.Join(",", graphIds.Select(m => m.ToString()).ToArray());

                var graphMode = "";
                if (nodesObjectsCb.Checked) graphMode = "Nodes-Object";
                else if (nodesInfoCb.Checked) graphMode = "Nodes-Information";
                else graphMode = "Nodes-Hilight Computer Map.";

                string dlgMsg = "Do you want to Add selected Graph Nodes to current level?\n" +
                     "Graph Id's: '" + graphIdsVal + "'\n" +
                     "Graph Mode: " + graphMode;

                var dlgResult = QUtils.ShowDialog(dlgMsg);
                if (dlgResult == DialogResult.Yes)
                {
                    SetStatusText("Adding Nodes Visualisation....Please Wait");
                    graphNodesAddWorker.RunWorkerAsync();
                }

            }
            else if (!nodesObjectsCb.Checked || !nodesHilightCb.Checked || nodesInfoCb.Checked)
            {
                ShowError("Adding Nodes visual needs type to be selected first.");
            }
        }

        private void graphIdDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                QUtils.gGameLevel = QUtils.gameFound ? QMemory.GetRunningLevel() : Convert.ToInt32(levelStartTxt.Value);
                graphIdDD.SelectedIndex = (graphIdDD.SelectedIndex == -1) ? 0 : graphIdDD.SelectedIndex;
                aiGraphId = QUtils.aiGraphIdStr[graphIdDD.SelectedIndex];
				QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "GraphId : " + aiGraphId + ",Level: " + QUtils.gGameLevel);

                SetStatusText("Updating GraphNodes data please wait....");

				int level = QUtils.gGameLevel;

				if (level <= 0 || level > QUtils.GAME_MAX_LEVEL)
				    level = QMemory.GetRunningLevel();

				QUtils.AddLog(MethodBase.GetCurrentMethod().Name," Previous level: " + QUtils.gGameLevel + ", Current Level: " + level);

                // Extract Nodes Data from Graph file.
                if (QUtils.gameFound)
                {
                    graphPos = QGraphs.GetGraphPosition(aiGraphId, QUtils.gGameLevel);
                    aiGraphNodeIdStr = QGraphs.GetNodesForGraph(aiGraphId, level);
                    graphTotalNodes = aiGraphNodeIdStr.Count;
                    graphTotalNodesTxt.Text = graphTotalNodes.ToString();
                    SetStatusText("Updating GraphNodes data done....");

                    //Update Graph Nodes.
                    UpdateUIComponent(nodeIdDD, QUtils.aiGraphNodeIdStr);
                }

                // Extract Nodes Data from QScript file.
                else
                {
                    int totalNodes = QGraphs.ExtractNodesData4mQScript(aiGraphId);
                    graphTotalNodesTxt.Text = totalNodes.ToString();
                }

                string graphArea = QGraphs.GetGraphArea(aiGraphId);
                graphAreaLbl.Text = graphArea;


                //Add all marked GraphIds.
                if (graphsMarkCb.Checked)
                {
                    QUtils.graphdIdsMarked.Add(aiGraphId);
                    SetStatusText("Graph#" + aiGraphId + " Marked...");
                }

                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "GraphId : " + aiGraphId + ",GraphArea: '" + graphArea + "',TotalNodes: " + graphTotalNodes);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void viewPortEnableCb_CheckedChanged(object sender, EventArgs e)
        {
            unsafe
            {
                IntPtr viewPortAddr = (IntPtr)0x00497E94;

                if (viewPortCameraEnableCb.Checked)
                {
                    GT.GT_WriteNOP(viewPortAddr, 2);
                    QInternals.HumanInputDisable();
                    viewPortCameraEnableCb.Text = "ViewPort - Enabled";
                }
                else
                {
                    GT.GT_WriteMemory(viewPortAddr, "2bytes", "42483");
                    QInternals.HumanInputEnable();
                    viewPortCameraEnableCb.Text = "ViewPort - Disabled";
                }

            }
        }

        private void addLevelFlowBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (QUtils.anyaTeamTaskId == -1)
                {
                    QUtils.ShowError("You need to add objects (A.I/Buildings) to your custom level first");
                }
                else
                {
                    if (String.IsNullOrEmpty(QUtils.levelFlowData)) throw new Exception("Mission levelflow data is empty. Couldn't add custom level");

                    if (String.IsNullOrEmpty(missionPlayTimeTxt.Text)) throw new ArgumentNullException("Play time must be entered in correct format(Seconds).");

                    float maxPlayTime = float.Parse(missionPlayTimeTxt.Text);
                    bool enableTimer = missionLevelFlowTimerCb.Checked;

                    //Add Complete and Failed mission tasks.
                    string completeMissionTask = "StatusMessage_" + QUtils.ekkTeamTaskId + ".isSendt";
                    string failedMissionTask = "StatusMessage_" + QUtils.anyaTeamTaskId + ".isSendt";

                    var qscData = QUtils.LoadFile();
                    var levelFlowTask = QMission.AddLevelFlow(completeMissionTask, failedMissionTask, maxPlayTime, enableTimer);
                    qscData = qscData.Replace(QUtils.levelFlowData, levelFlowTask);
                    if (!String.IsNullOrEmpty(qscData))
                        compileStatus = QCompiler.CompileEx(qscData);
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void installMissionBtn_Click(object sender, EventArgs e)
        {
            var missionName = missionNameTxt.Text;
            try
            {
                OpenFileDialog fileDlg = new OpenFileDialog();
                fileDlg.Filter = "IGI 1 Mission files (*.igimsf)|*.igimsf|All files (*.*)|*.*";
                fileDlg.RestoreDirectory = true;
                fileDlg.Title = "Select IGI Mission file";
                fileDlg.DefaultExt = ".igimsf";
                fileDlg.Multiselect = false;
                fileDlg.CheckFileExists = fileDlg.RestoreDirectory = fileDlg.AddExtension = true;
                string missionNamePath = null;

                DialogResult dlgResult = fileDlg.ShowDialog();
                if (dlgResult == DialogResult.Yes)
                {
                    missionNamePath = fileDlg.FileName;
                    missionName = Path.GetFileName(missionNamePath);

                    if (String.IsNullOrEmpty(missionName)) throw new Exception("Mission name cannot be empty");

                    //string missionNameExt = missionName + QUtils.FileExtensions.Mission;
                    var mFilesList = new List<string>() { QUtils.missionLevelFile, QUtils.missionDescFile, QUtils.objectsQsc };

                    if (File.Exists(QUtils.qMissionsPath + @"\" + missionName))
                    {
                        QUtils.ShowError("Mission '" + missionName + "' already exist in your missions directory");
                        return;
                    }
                    bool isValidMission = QZip.FilesExist(missionNamePath, mFilesList);
                    isValidMission = QZip.FileExist(missionNamePath, null, QUtils.FileExtensions.Qsc);

                    if (!isValidMission) throw new Exception("Mission '" + missionName + "' is invalid mission file and cannot be installed");

                    QUtils.FileIOMove(missionNamePath, QUtils.qMissionsPath + @"\" + missionName);
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Mission '" + missionName + "' was installed.");
                    SetStatusText("Mission '" + missionName + "' was installed.");
                }
            }
            catch (NullReferenceException ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void removeMissionBtn_Click(object sender, EventArgs e)
        {
            if (QUtils.aiScriptFiles.Count == 0)
            {
                QUtils.ShowError("No Mission was installed in your Missions directory.");
                return;
            }

            QUtils.CleanUpAiFiles();
            QUtils.RestoreLevel(gameLevel);
            QUtils.ResetScriptFile(gameLevel);
            QMemory.RestartLevel();
            missionNameTxt.Text = missionDescTxt.Text = String.Empty;
            SetStatusText("Mission removed successfully.");
            QUtils.aiScriptFiles.Clear();
        }

        private void saveMissionBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var missionName = missionNameTxt.Text;
                var missionDesc = missionDescTxt.Text;

                if (String.IsNullOrEmpty(missionName)) throw new Exception("Mission name cannot be empty");

                QMission.ChangeMissionDetails(QUtils.gGameLevel, missionName, missionDesc);
                var missionNameExt = missionName + QUtils.FileExtensions.Mission;

                //Create mission folder.
                Directory.CreateDirectory(missionName);
                Directory.CreateDirectory(missionName + @"\ai");

                QUtils.SaveFile(QUtils.missionDescFile, missionDesc);

                //Save A.I Files and Objects to mission.
                var outputAiPath = QUtils.cfgGamePath + QUtils.gGameLevel + "\\ai\\";
                QUtils.SaveFile(QUtils.missionLevelFile, gameLevel.ToString());

                foreach (var scriptFile in QUtils.aiScriptFiles)
                {
                    var missionAiPath = missionName + @"\ai\" + scriptFile;
                    if (!File.Exists(missionAiPath))
                        QUtils.FileCopy(outputAiPath + scriptFile, missionAiPath);
                }
                //QCryptor.Encrypt(QUtils.objectsQsc);

                QUtils.FileCopy(QUtils.objectsQsc, missionName + @"\" + QUtils.objectsQsc);
                QUtils.FileIOMove(QUtils.missionDescFile, missionName + @"\" + QUtils.missionDescFile);
                QUtils.FileIOMove(QUtils.missionLevelFile, missionName + @"\" + QUtils.missionLevelFile);

                QZip.Create(missionName, QUtils.FileExtensions.Mission, true);
                QUtils.FileIOMove(missionNameExt, QUtils.qMissionsPath + @"\" + missionNameExt);
                QUtils.ShowInfo("Mission '" + missionName + "' was saved successfully");
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void loadMissionBtn_Click(object sender, EventArgs e)
        {
            try
            {
                missionNameTxt.Text = missionDescTxt.Text = String.Empty;
                string missionName = null;

                fopenIO = QUtils.ShowOpenFileDlg("Select Mission file", ".igimsf", "Mission files (*.igimsf)|*.igimsf|All files (*.*)|*.*", true, QUtils.qMissionsPath);

                missionName = Path.GetFileNameWithoutExtension(fopenIO.FileName);
                if (String.IsNullOrEmpty(missionName)) throw new Exception("Mission name cannot be empty");

                QUtils.CleanUpAiFiles();//Cleanup previous mission files.

                //Append level to mission name.
                var missionPath = QUtils.qMissionsPath + Path.DirectorySeparatorChar + missionName;

                var missionDescFile = missionPath + @"\" + QUtils.missionDescFile;
                var missionPathExt = missionPath + QUtils.FileExtensions.Mission;

                //if (!File.Exists(missionPathExt)) throw new FileNotFoundException("Mission '" + missionName + "' does not exist");
                bool missionAiExist = false;
                //Extract the mission directory.
                QZip.Extract(missionPathExt, QUtils.qMissionsPath);
                QUtils.Sleep(1);

                if (Directory.Exists(missionPath + @"\ai"))
                {
                    if (QUtils.IsDirectoryEmpty(missionPath + @"\ai"))
                    {
                        QUtils.ShowWarning("Mission directory doesn't include A.I Files.");
                        missionAiExist = false;
                    }
                    else missionAiExist = true;
                }

                var missionLevelFile = missionPath + @"\" + QUtils.missionLevelFile;
                int missionLevel = Convert.ToInt32(QUtils.LoadFile(missionLevelFile));
                bool levelSwitched = false;
                gameLevel = QUtils.gGameLevel = QMemory.GetRunningLevel();
                levelStartTxt.Text = gameLevel.ToString();

                QMemory.DisableGameWarnings();
                if (missionLevel != gameLevel)
                {
                    var dlgResult = QUtils.ShowDialog("Mission level error do you want to switch level to '" + missionLevel + "' to continue ?", "Error");
                    if (dlgResult == DialogResult.Yes)
                    {
                        QUtils.ResetScriptFile(missionLevel);
                        QUtils.RestoreLevel(missionLevel);
                        levelStartTxt.Text = missionLevel.ToString();
                        gameLevel = QUtils.gGameLevel = missionLevel;
                        levelSwitched = true;
                    }
                    else
                        levelSwitched = false;
                }

                var missionOutAiPath = QUtils.cfgGamePath + QUtils.gGameLevel + "\\ai\\";

                var missionDesc = QUtils.LoadFile(missionDescFile);
                QMission.ChangeMissionDetails(missionLevel, missionName, missionDesc);

                QUtils.FileCopy(missionPath + @"\" + QUtils.objectsQsc, QUtils.objectsQsc);
                var qData = QUtils.LoadFile();
                QUtils.SaveFile(QUtils.objectsQsc, qData);

                //Copy all A.I Files.
                var missionAiPath = missionPath + @"\ai";
                if (missionAiExist)
                {
                    foreach (var outputPath in Directory.GetFiles(missionAiPath, "*.qvm*", SearchOption.AllDirectories))
                    {
                        string outputAiPath = outputPath.Replace(missionAiPath, missionOutAiPath);
                        QUtils.FileCopy(outputPath, outputAiPath, true);
                        string aiFileName = outputPath.Substring(outputPath.LastIndexOf("\\") + 1);
                    }
                }

                var qscData = QUtils.LoadFile();
                qscData += QUtils.AddStatusMsg(-1, "Mission : " + missionName, "!HumanPlayer_0.isDead", ';');
                qscData += QUtils.AddStatusMsg(-1, "Description : " + missionDesc, "!HumanPlayer_0.isDead", ';');

                if (levelSwitched)
                {
                    startGameBtn_Click(sender, e);
                }

                if (!String.IsNullOrEmpty(qscData)) compileStatus = QCompiler.CompileEx(qscData);

                var aiFiles = Directory.GetFiles(missionPath, "*.qvm", SearchOption.AllDirectories);
                var aiFilesCount = aiFiles.Length;
                var missionsAiList = aiFiles.ToList();

                //Update AI Scirpts and Index.
                QUtils.aiScriptFiles.Clear();
                string aiScriptStr = null;
                foreach (var aiScript in missionsAiList)
                {
                    aiScriptStr = aiScript.Substring(aiScript.LastIndexOf(@"\") + 1);
                    QUtils.aiScriptFiles.Add(aiScriptStr);//Update script file from Mission A.I.
                }

                QUtils.aiScriptId += aiFilesCount * 2;//Update A.I Script Index.
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Ai Script Id : " + aiScriptId);

                QUtils.DirectoryDelete(missionPath);
                missionNameTxt.Text = missionName;
                missionDescTxt.Text = missionDesc;
                SetStatusText("Mission '" + missionName + "' was loaded.");

            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void removeAiBtn_Click(object sender, EventArgs e)
        {
            var aiModelName = QAI.GetAiModelNamesList(gameLevel)[aiModelSelectDD.SelectedIndex];
            var aiModelId = QAI.GetAiModelId4Name(aiModelName);
            if (removeAllAiCb.Checked) aiModelName = "all_a.i_soldiers";
            var aiModelQualifyName = aiModelName.Contains("_") ? aiModelName.Substring(0, aiModelName.IndexOf("_")) : aiModelName;

            var dlgResult = QUtils.ShowDialog("Do you want to remove '" + aiModelQualifyName + "' from level ?");
            if (dlgResult == DialogResult.No) return;

            string qscData = QUtils.LoadFile();
            if (removeAllAiCb.Checked)
            {
                var aiList = QAI.GetAiModelIds(gameLevel);
                foreach (var modelIds in aiList)
                    qscData = QAI.RemoveHumanSoldier(qscData, modelIds);
            }
            else
                qscData = QAI.RemoveHumanSoldier(qscData, aiModelId);

            if (!String.IsNullOrEmpty(qscData))
                compileStatus = QCompiler.CompileEx(qscData);

            if (compileStatus) SetStatusText("AI " + aiModelQualifyName + " removed successfully");

            //if (QUtils.aiScriptFiles.Count == 0)
            //{
            //    QUtils.ShowError("No A.I Soldiers were added yet.");
            //    return;
            //}

            //QUtils.CleanUpAiFiles();
            //QUtils.RestoreLevel(gameLevel);
            //QUtils.ResetScriptFile(gameLevel);
            //QMemory.RestartLevel(false);
            //SetStatusText("All A.I soldiers were removed successfully.");
        }

        private void teleportToGraphBtn_Click(object sender, EventArgs e)
        {
            if (!viewPortCameraEnableCb.Checked) { QUtils.ShowWarning("Camera ViewPort was not enabled yet."); viewPortCameraEnableCb.Checked = true; }
            //Graph Teleport section - MANUAL.
            if (manualTeleportGraphCb.Checked) QUtils.UpdateViewPort(graphPos);

            //Graph Teleport section - AUTO.
            else if (autoTeleportGraphCb.Checked)
            {
                if (!graphTraverseWorker.IsBusy)
                {
                    SetStatusText("Graph traversing started...");
                    QInternals.HumanInputDisable();
                    graphTraverseWorker.RunWorkerAsync();
                }
            }
        }

        private void teleportToNodeBtn_Click(object sender, EventArgs e)
        {
            if (!viewPortCameraEnableCb.Checked) { QUtils.ShowWarning("Camera ViewPort was not enabled yet."); viewPortCameraEnableCb.Checked = true; }

            //Graph Teleport section - MANUAL.
            if (manualTeleportNodeCb.Checked) QUtils.UpdateViewPort(nodeRealPos);

            //Graph Teleport section - AUTO.
            else if (autoTeleportNodeCb.Checked)
            {
                if (!nodesTraverseWorker.IsBusy)
                {
                    SetStatusText("Node traversing started...");
                    QInternals.HumanInputDisable();
                    nodesTraverseWorker.RunWorkerAsync();
                }
            }
        }

        private void objectSelectDD_SelectedIndexChanged(object sender, EventArgs e)
        {
#if! DEV_MODE
            try
            {
                var objectRigidModel = QUtils.objectRigidList[objectSelectDD.SelectedIndex].Values.ElementAt(0);
                PopulateImageBox(objectRigidModel, objectImgBox);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
#endif
        }

        private void buildingSelectDD_SelectedIndexChanged(object sender, EventArgs e)
        {
#if !DEV_MODE
            try
            {
                var buildingModel = QUtils.buildingList[buildingSelectDD.SelectedIndex].Values.ElementAt(0);
                PopulateImageBox(buildingModel, objectImgBox);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
#endif
        }

        private void aiModelSelectDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            aiModelSelectDD.SelectedIndex = aiModelSelectDD.SelectedIndex == -1 ? 0 : aiModelSelectDD.SelectedIndex;
            var aiModelName = QAI.GetAiModelNamesList(gameLevel)[aiModelSelectDD.SelectedIndex];
            var aiModelId = QAI.GetAiModelId4Name(aiModelName);
            PopulateImageBox(aiModelId, aiImgBox);
        }

        private void manualTeleportGraphCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                teleportToGraphBtn.Text = "Teleport To Graph";
                stopTraversingNodesBtn.Enabled = false;
                autoTeleportGraphCb.Checked = false;
            }
        }

        private void autoTeleportGraphCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                stopTraversingNodesBtn.Enabled = true;
                teleportToGraphBtn.Text = "Traverse Graphs";
                manualTeleportGraphCb.Checked = false;
            }
        }

        private void manualTeleportNodeCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                teleportToNodeBtn.Text = "Teleport To Node";
                stopTraversingNodesBtn.Enabled = false;
                autoTeleportNodeCb.Checked = false;
            }
        }

        private void missionRefreshBtn_Click(object sender, EventArgs e)
        {
            try
            {
                InitMissionsOnline(true, false);
                UpdateUIComponent(missionsOnlineDD, QUtils.missionNameListStr);
            }
            catch (Exception ex) { }
        }

        private void editorModeCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                QInternals.HumanFreeCam();
                ((CheckBox)sender).Text = "Edit Mode";
                ((CheckBox)sender).ForeColor = SpringGreen;
                QInternals.StatusMessageShow("Editor mode enabled. use Arrows keys to move ALT/SPACE change height");
            }
            else
            {
                GT.GT_SendKeyStroke("HOME");
                QUtils.Sleep(0.5f);
                ((CheckBox)sender).Text = "Play Mode";
                ((CheckBox)sender).ForeColor = Tomato;
                QInternals.StatusMessageShow("Play mode enabled - Play level.");
            }
            SetStatusText(((CheckBox)sender).Text + " enabled");
        }

        private void aiIdleCb_CheckedChanged(object sender, EventArgs e)
        {
            aiIdleCb.Checked = !aiIdleCb.Checked;
            if (aiIdleCb.Checked)
            {
                bool status = QUtils.SetAIEventIdle(true);
                SetStatusText("A.I Idle Mode Enabled");
            }
            else
            {
                QUtils.SetAIEventIdle(false);
                SetStatusText("A.I Idle Mode disabled");
            }
            QUtils.gameAiIdleMode = ((CheckBox)sender).Checked;
        }

        private void updateTeamIdBtn_Click(object sender, EventArgs e)
        {
            int teamId = 0;
            if (!String.IsNullOrEmpty(teamIdTxt.Text))
            {
                teamId = Convert.ToInt32(teamIdTxt.Text);
                if (teamId != 0)
                {
                    var qscData = QHuman.UpdateTeamId(teamId);

                    if (!String.IsNullOrEmpty(qscData)) compileStatus = QCompiler.Compile(qscData, QUtils.gamePath, false, true, false);
                    if (compileStatus) SetStatusText("Human team updated successfully");
                }
            }
            if (!String.IsNullOrEmpty(humanViewCamTxt.Text)) QInternals.HumanCameraView(humanViewCamTxt.Text);
            QInternals.StatusMessageShow("Human camera updated.");
        }

        private void setFramesBtn_Click(object sender, EventArgs e)
        {
            string frames = framesTxt.Text;
            QInternals.FramesSet(frames);
            SetStatusText("Frames has been set to '" + frames + "'");
        }

        private void quitLevelBtn_Click(object sender, EventArgs e)
        {
            QInternals.QuitLevel();
        }

        private void debugModeCb_CheckedChanged(object sender, EventArgs e)
        {
            debugModeCb.Checked = !debugModeCb.Checked;
            QInternals.DebugMode();
            QUtils.gameDebugMode = debugModeCb.Checked;
        }

        private void enableMusicCb_CheckedChanged(object sender, EventArgs e)
        {
            enableMusicCb.Checked = !enableMusicCb.Checked;
            string statusMusic;
            if (enableMusicCb.Checked)
            {
                QInternals.MusicEnable();
                statusMusic = "Music Enabled";
                enableMusicCb.ForeColor = Green;
            }
            else
            {
                QInternals.MusicDisable();
                statusMusic = "Music Disabled";
                enableMusicCb.ForeColor = Tomato;
            }
            SetStatusText(statusMusic + " successfully.");
            QUtils.gameMusicEnabled = enableMusicCb.Checked;
        }

        private void gfxResetBtn_Click(object sender, EventArgs e)
        {
            QInternals.GraphicsReset();
            SetStatusText("Graphics settings reset");
        }

        private void resetPosCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) humanPosOffCb.Checked = humanPosMeterCb.Checked = false;
        }

        private void aiTypeDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            var aiModelName = QAI.GetAiModelNamesList(gameLevel)[aiModelSelectDD.SelectedIndex];
            var aiTypeName = ((ComboBox)sender).SelectedItem.ToString();

            //HumanSoldierFemale exceptions.
            if (aiModelName == "ANYA" && (aiTypeName != "ANYA" && aiTypeName != "EKK"))
            {
                QUtils.ShowWarning("HumanSoldier-Female 'ANYA' Type should also be of female soldier.", "A.I WARNING");
                addAiBtn.Enabled = false;
            }
            else if (aiModelName == "EKK" && (aiTypeName != "ANYA" && aiTypeName != "EKK"))
            {
                QUtils.ShowWarning("HumanSoldier-Female 'EKK' Type should also be of female soldier.", "A.I WARNING");
                addAiBtn.Enabled = false;
            }
            else
                addAiBtn.Enabled = true;
        }

        private void nodeIdMetreCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) nodeIdOffsetCb.Checked = false;
            else nodeXLbl.Text = nodeYLbl.Text = nodeZLbl.Text = String.Empty;
        }

        private void nodeIdOffsetCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) nodeIdMetreCb.Checked = false;
            else nodeXLbl.Text = nodeYLbl.Text = nodeZLbl.Text = String.Empty;
        }

        private void modWeaponBtn_Click(object sender, EventArgs e)
        {
            var weaponsCfgPathFile = QUtils.weaponsGamePath + @"\" + QUtils.weaponConfigQVM;
            QUtils.FileCopy(QUtils.weaponsModQvmPath, weaponsCfgPathFile);

            QInternals.WeaponConfigRead();
            string weaponMsg = "Weapons Modded loaded successfully.";
            SetStatusText(weaponMsg);
            QInternals.StatusMessageShow(weaponMsg);
        }

        private void resetModWeaponBtn_Click(object sender, EventArgs e)
        {
            var weaponsCfgPathFile = QUtils.weaponsGamePath + @"\" + QUtils.weaponConfigQVM;
            QUtils.FileCopy(QUtils.weaponsOrgCfgPath, weaponsCfgPathFile);
            QInternals.WeaponConfigRead();

            string weaponMsg = "Weapons reset success.";
            SetStatusText(weaponMsg);
            QInternals.StatusMessageShow(weaponMsg);
        }

        private void framesTxt_TextChanged(object sender, EventArgs e)
        {
            HandleInvalidTextFmt(sender, 30, MAX_FPS, MethodBase.GetCurrentMethod().Name);
        }

        private void objectIDTxt_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var modelId = ((TextBox)sender).Text;
                var modelName = QObjects.FindModelName(modelId, false);
                if (!String.IsNullOrEmpty(modelName)) modelNameOutLbl.Text = modelName;
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void modelNameTxt_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var modelName = ((TextBox)sender).Text;
                var modelId = QObjects.FindModelId(modelName, false);
                if (!String.IsNullOrEmpty(modelName)) modelIdOutLbl.Text = modelId;
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void stopTraversingNodesBtn_Click(object sender, EventArgs e)
        {
            if (graphTraverseWorker.IsBusy)
            {
                autoTeleportGraphCb.Checked = viewPortCameraEnableCb.Checked = false;
                graphTraverseWorker.CancelAsync();
                QInternals.HumanInputEnable();
                SetStatusText("Graphs traversing canceled.");
            }

            if (nodesTraverseWorker.IsBusy)
            {
                autoTeleportNodeCb.Checked = viewPortCameraEnableCb.Checked = false;
                nodesTraverseWorker.CancelAsync();
                QInternals.HumanInputEnable();
                SetStatusText("Nodes traversing canceled.");
            }
        }

        private void humanViewCamTxt_TextChanged(object sender, EventArgs e)
        {
            var viewCam = ((TextBox)sender).Text;
            int cam = Convert.ToInt32(viewCam);
            //Check for Max FPS
            if (cam < 0 || cam > MAX_HUMAN_CAM)
            {
                cam = 1;
                ((TextBox)sender).Text = cam.ToString();
            }
        }

        private void nodesObjectsCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) nodesHilightCb.Checked = nodesInfoCb.Checked = false;
        }

        private void nodesHilightCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) nodesObjectsCb.Checked = nodesInfoCb.Checked = false;
        }

        private void configLoadBtn_Click(object sender, EventArgs e)
        {
            QInternals.GameConfigRead();
            string status = "Game config load success";
            SetStatusText(status);
            QInternals.StatusMessageShow(status);
        }

        private void configSaveBtn_Click(object sender, EventArgs e)
        {
            QInternals.GameConfigWrite();
            string status = "Game config save success";
            SetStatusText(status);
            QInternals.StatusMessageShow(status);
        }

        private void assembleBtn_Click(object sender, EventArgs e)
        {
            string qscData = QUtils.LoadFile();
            QInternals.ScriptParser(qscData);
            SetStatusText("Parsing of script file done.");
        }

        private void addLinksBtn_Click(object sender, EventArgs e)
        {
            if (nodesObjectsCb.Checked || nodesHilightCb.Checked || nodesInfoCb.Checked)
            {
                var graphIds = new List<int>();

                if (graphsMarkCb.Checked) graphIds = graphdIdsMarked;
                else if (graphsAllCb.Checked) graphIds = aiGraphIdStr;
                else graphIds.Add(Convert.ToInt32(graphIdDD.SelectedValue));
                var graphIdsVal = string.Join(",", graphIds.Select(m => m.ToString()).ToArray());

                var graphMode = "";
                if (nodesObjectsCb.Checked) graphMode = "Nodes-Object";
                else if (nodesInfoCb.Checked) graphMode = "Nodes-Information";
                else graphMode = "Nodes-Hilight Computer Map.";

                string dlgMsg = "Do you want to Add selected Graph Links to current level?\n" +
                     "Graph Id's: '" + graphIdsVal + "'\n" +
                     "Graph Mode: " + graphMode;

                var dlgResult = QUtils.ShowDialog(dlgMsg);
                if (dlgResult == DialogResult.Yes)
                {
                    SetStatusText("Adding Nodes Link Visualisation....Please Wait");
                    graphLinksAddWorker.RunWorkerAsync();
                }
            }

            else if (!nodesObjectsCb.Checked || !nodesHilightCb.Checked || nodesInfoCb.Checked)
            {
                ShowError("Adding Links visual needs type to be selected first.");
            }

        }

        private void removeLinksBtn_Click(object sender, EventArgs e)
        {
            string taskNote = "G#" + aiGraphId + "L#" + nodeId;
            bool status = QGraphs.RemoveNodeLinks(aiGraphId, nodeId, taskNote);
            if (status) SetStatusText("Removed Graph#" + aiGraphId + " Links success");
            else SetStatusText("Removed Graph#" + aiGraphId + " Links error");
        }

        private void removeNodesBtn_Click(object sender, EventArgs e)
        {
            string taskNote = "G#" + aiGraphId + "N#" + nodeId;
            bool status = QGraphs.RemoveNodeLinks(aiGraphId, nodeId, taskNote);
            if (status) SetStatusText("Removed Graph#" + aiGraphId + " nodes success");
            else SetStatusText("Removed Graph#" + aiGraphId + " nodes error");
        }

        private void gameItemsLbl_Click(object sender, EventArgs e)
        {
            gameItemsLbl.Text = "Game Items: " + QUtils.GameitemsCount();
        }

        private void nodesAllCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) graphsMarkCb.Checked = false;
        }

        private void refreshNodesBtn_Click(object sender, EventArgs e)
        {
            removeNodesBtn_Click(sender, e);
            QUtils.Sleep(2.5f);
            addNodesBtn_Click(sender, e);
        }

        private void refreshLinksBtn_Click(object sender, EventArgs e)
        {
            removeLinksBtn_Click(sender, e);
            QUtils.Sleep(2.5f);
            addLinksBtn_Click(sender, e);
        }

        private void aiGraphIdDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                int graphAiId = -1;
                if (aiGraphIdDD.SelectedIndex == -1) aiGraphIdDD.SelectedIndex = 0;

                if (aiGraphIdDD.Items.Count > 0 && aiGraphIdDD.SelectedIndex >= 0 && aiGraphIdDD.SelectedValue != null)
                    graphAiId = (int)aiGraphIdDD.SelectedValue;

                string graphArea = QGraphs.GetGraphArea(graphAiId);
                graphAreaAiLbl.Text = graphArea;
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "GraphId : " + aiGraphId + " GraphArea: " + graphArea);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void showAllGraphsCb_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateUIComponent(graphIdDD, QUtils.aiGraphIdStr);
                UpdateUIComponent(nodeIdDD, QUtils.aiGraphNodeIdStr);
                RefreshUIComponents(gameLevel, false, true, false);
                if (graphIdDD.Items.Count > 0 && nodeIdDD.Items.Count > 0)
                    graphIdDD.SelectedIndex = nodeIdDD.SelectedIndex = 0;
            }
            catch (Exception ex) { QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        private void updateCheckerCb_CheckedChanged(object sender, EventArgs e)
        {
            if (updateCheckerCb.Checked)
            {
                var dlgResult = QUtils.ShowDialog("Do you want to check for new updates in every " + QUtils.updateTimeInterval + " minutes?");
                if (dlgResult == DialogResult.Yes)
                {
                    updateCheckerTimer.Interval = QUtils.updateTimeInterval * 60000;
                    updateCheckerTimer.Start();
                    SetStatusText("Editor will check for update every " + QUtils.updateTimeInterval + " minutes");
                    QUtils.editorUpdateCheck = true;
                }
                else updateCheckerCb.Checked = false;
            }
            else
            {
                updateCheckerTimer.Stop();
                SetStatusText("Editor auto update check cancelled");
                QUtils.editorUpdateCheck = false;
            }
        }

        private void editorUpdaterBtn_Click(object sender, EventArgs e)
        {
            downloadUpdaterWorker.RunWorkerAsync();
        }

        internal void RichViewerUpdateFormat()
        {
            if (devFileNameTxt.Text == QUtils.editorChangeLogs + QUtils.FileExtensions.Text)
            {
                var keywords = new List<string>() { "CHANGELOGS", "BETA", "version", "Updated", "Added", "Removed", "fixed", "Offline mode" };
                var colors = new List<Color>() { Red, Violet, Cyan, Green, Green, Red, DarkSeaGreen, Gray };
                var fontStyles = new List<FontStyle>() { Underline, Underline, Bold, Bold, Bold, Bold, Bold, Bold };

                RichViewerFormatter(devViewerTxt, keywords, colors, "Consolas", 12, fontStyles);
            }

            if (devFileNameTxt.Text == editorAppName + QUtils.FileExtensions.Ini)
            {
                var keywords = new List<string>() { "[PATH]", "[EDITOR]", "game_path", "game_reset", "app_logs", "app_online", "update_check", "update_interval", "true", "false" };
                var colors = new List<Color>() { Red, Red, Green, Cyan, Cyan, Cyan, Cyan, Cyan, DarkGreen, Olive };
                var fontStyles = new List<FontStyle>() { Underline, Underline, Bold, Bold, Bold, Bold, Bold, Bold, Italic, Italic };

                RichViewerFormatter(devViewerTxt, keywords, colors, "Consolas", 10, fontStyles);
            }

            if (aiFileNameTxt.Text.Contains(QUtils.FileExtensions.Json))
            {
                var keywords = new List<string>() { "aiCount", "aiType", "model", "graphId", "weapon", "friendly", "guardGenerator", "maxSpawns", "teamId", "invincible", "advanceView", "true", "false" };
                var colors = new List<Color>() { Red, Red, Green, Cyan, Cyan, Cyan, Cyan, Cyan, Cyan, Cyan, Cyan, DarkGreen, Olive };
                var fontStyles = new List<FontStyle>() { Underline, Underline, Bold, Bold, Bold, Bold, Bold, Bold, Bold, Bold, Bold, Italic, Italic };

                RichViewerFormatter(aiJsonEditorTxt, keywords, colors, "Consolas", 12, fontStyles);
            }

        }

        public void RichViewerFormatter(RichTextBox richBox, List<string> keywords, List<Color> colors, string fontName, int fontSize, List<FontStyle> fontStyles)
        {
            for (int index = 0; index < keywords.Count; index++)
            {
                RichViewerFormat(richBox, keywords[index], colors[index], fontName, fontSize, fontStyles[index]);
            }
        }

        private void RichViewerFormat(RichTextBox richBox, string phrase, Color color, string fontName, int fontSize, FontStyle fontStyles)
        {
            int pos = richBox.SelectionStart;
            string text = richBox.Text;
            int startIndex = 0;
            do
            {
                int index = text.IndexOf(phrase, startIndex, StringComparison.CurrentCultureIgnoreCase);
                if (index < 0) break;
                richBox.SelectionStart = index;
                richBox.SelectionLength = phrase.Length;
                richBox.SelectionColor = color;
                richBox.SelectionFont = new Font(fontName, fontSize, fontStyles);
                startIndex = index + 1;
            } while (true);

            richBox.SelectionStart = pos;
            richBox.SelectionLength = 0;
        }

        private void loadDevFileBtn_Click(object sender, EventArgs e)
        {
            try
            {
                //Reset before Load File.
                devFileNameTxt.Text = String.Empty;
                devViewerTxt.Clear();
                devViewerTxt.SelectionColor = White;
                devViewerTxt.SelectionFont = new Font("Arial", 10);

                fopenIO = QUtils.ShowOpenFileDlg("Select Developer File", ".txt", "Text File|*.txt|BATCH file|*.bat|INI File|*.ini|All Files|*.*");
                devFileNameTxt.Text = Path.GetFileName(fopenIO.FileName);

                if (!String.IsNullOrEmpty(fopenIO.FileData))
                {
                    devViewerTxt.Text = fopenIO.FileData;
                    RichViewerUpdateFormat();
                    devFileSizeTxt.Text = "File Size: " + fopenIO.FileLength / 1024 + "Kb";
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void saveDevFileBtn_Click(object sender, EventArgs e)
        {
            string devViewerData = devViewerTxt.Text;
            if (!String.IsNullOrEmpty(devViewerData))
            {
                var dlgResult = QUtils.ShowDialog("Do you want to save the file '" + devFileNameTxt.Text + "'?");
                if (dlgResult == DialogResult.Yes)
                {
                    if (String.IsNullOrEmpty(devFileNameTxt.Text))
                        QUtils.ShowLogError(MethodBase.GetCurrentMethod().Name, "Dev file name is invalid.");
                    else
                        QUtils.SaveFile(devFileNameTxt.Text, devViewerData);

                    if (devClearContentsCb.Checked) devViewerTxt.Clear();
                    SetStatusText(devFileNameTxt.Text + " file data saved successfully");
                }
            }
        }

        private void devViewerTxt_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.S))
            {
                e.IsInputKey = true;
                saveDevFileBtn_Click(sender, (EventArgs)e);
            }
            else if (e.KeyData == (Keys.Control | Keys.F))
            {
                e.IsInputKey = true;
                RichViewerUpdateFormat();
            }
            else if (e.KeyData == (Keys.Control | Keys.R))
            {
                ((RichTextBox)sender).Text = QUtils.LoadFile(devFileNameTxt.Text);
            }
        }

        private void devViewerTxt_TextChanged(object sender, EventArgs e)
        {
            if (devAutoFormatCb.Checked)
            {
                RichViewerUpdateFormat();
            }
        }

        private void createUpdateBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string devPath = QUtils.editorCurrPath;
                string dbgPath = devPath.Replace("DevMode", "Debug");
                string rlsPath = devPath.Replace("DevMode", "Release");
                string cachePathBin = cachePath + "\\bin";
                string devPathBin = devPath + "\\bin";
                string dbgPathBin = dbgPath + "\\bin";
                string editorx86 = QUtils.editorAppName + "_x86" + QUtils.FileExtensions.Exe;
                string editorExe = QUtils.editorAppName + QUtils.FileExtensions.Exe;
                string changelogsPath = QUtils.editorCurrPath + "\\" + QUtils.editorChangeLogs + QUtils.FileExtensions.Text;
                string versionPath = QUtils.editorCurrPath + "\\" + QUtils.versionFileName + QUtils.FileExtensions.Text;
                string readmePath = QUtils.editorCurrPath + "\\" + QUtils.editorReadme + QUtils.FileExtensions.Text;
                string changeLogsData = QUtils.LoadFile(QUtils.editorChangeLogs + QUtils.FileExtensions.Text);

                var dlgRes = QUtils.ShowDialog("Did you Cleaned and Batch Build the new Project ?");
                if (dlgRes == DialogResult.Yes)
                {
                    //Check if correct version provided.
                    var result = CheckEditorVersion(devVersionTxt.Text, appEditorSubVersion);
                    if (result != 0)
                    {
                        QUtils.ShowLogError(MethodBase.GetCurrentMethod().Name, "Updater Version not same as Editor.\nEditor version v" + appEditorSubVersion + " Provided version is v" + devVersionTxt.Text);
                        return;
                    }

                    var dlgResult = QUtils.ShowDialog("Do you want to create new update for Editor version 'v" + devVersionTxt.Text + "?");
                    if (dlgResult == DialogResult.Yes)
                    {
                        var changeLogIndex = changeLogsData.IndexOf("version " + devVersionTxt.Text);
                        if (changeLogIndex == -1)
                        {
                            QUtils.ShowLogError(MethodBase.GetCurrentMethod().Name, "Updater changelogs not found for version v'" + devVersionTxt.Text + "'");
                            return;
                        }
                        changeLogsData = changeLogsData.Substring(changeLogIndex);
                        QUtils.ShowLogInfo(MethodBase.GetCurrentMethod().Name, "Confirm the changelogs:\n" + changeLogsData);

                        //Delete previous updated from Dev path.
                        QUtils.FileIODelete(QUtils.editorUpdater);

                        QUtils.FileIOCopy(rlsPath + "\\" + editorExe, cachePath + "\\" + editorExe);
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Moving file '" + rlsPath + "\\" + editorExe + "' to " + cachePath + "\\" + editorExe + "'");

                        //Moving bin folder to cache.
                        QUtils.DirectoryIOCopy(devPathBin, cachePathBin);
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Moving bin folder from '" + dbgPathBin + "' to '" + cachePathBin + "'");

                        //Archive all path to .zip
                        string zipCmd = "7z a -tzip " + editorUpdater + " " + cachePath + "\\" + editorExe + " " + cachePathBin + " " + changelogsPath + " " + versionPath + " " + readmePath;
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Running zip command '" + zipCmd + "'");
                        string shellOut = QUtils.ShellExec(zipCmd, true);
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Shell output: '" + shellOut + "'");

                        //Remove Updater from Cache.
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Remove Updater from Cache.");
                        QUtils.FileIODelete(cachePath + "\\" + editorExe);
                        QUtils.FileIODelete(cachePath + "\\" + editorx86);
                        QUtils.DirectoryIODelete(cachePathBin);
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Remove Updater from Cache....DONE!");

                        //Check if update created successfully.
                        if (File.Exists(editorUpdater))
                        {
                            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Creating new updated versions file.");
                            //Create new version files for updater.
                            QUtils.SaveFile("Updater-" + devVersionTxt.Text + ".txt", devVersionTxt.Text);
                            QUtils.SaveFile(QUtils.versionFileName + ".txt", devVersionTxt.Text);
                            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Creating new updated versions file...DONE!");

                            float fileSize = new FileInfo(editorUpdater).Length / 1024;
                            devFileNameTxt.Text = editorUpdater;
                            devFileSizeTxt.Text = "File Size: " + fileSize + "Kb";
                            QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Update created successfully.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                QUtils.ShowException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void uploadUpdateBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string updaterMask = "Updater-", updaterVersionTag = updaterMask + QUtils.appEditorSubVersion + QUtils.FileExtensions.Text, updaterVerTag = updaterVersionTag;
                if (!File.Exists(QUtils.editorUpdater) || !File.Exists(updaterVersionTag)) { QUtils.ShowLogError(MethodBase.GetCurrentMethod().Name, "Updater file not found in current directory."); return; }

                float fileSize = new FileInfo(QUtils.editorUpdater).Length / 1024;
                //Get the remote version accordinly to current version.
                var updaterVersion = updaterVerTag.Replace(updaterMask, String.Empty).Replace(QUtils.FileExtensions.Text, String.Empty);
                var version = (updaterVersion[updaterVersion.Length - 1] - 1) - 0x30;
                var remoteVersion = updaterVersion.Slice(0, updaterVersion.Length - 1) + version.ToString();

                var dlgResult = QUtils.ShowDialog("Do you want to upload new update for Editor version 'v" + devVersionTxt.Text + "'\nUpdater Size:" + fileSize + "Kbs\nRemote version '" + remoteVersion + "'");
                if (dlgResult == DialogResult.Yes)
                {
                    string updateUrlPath = "/" + QServer.updateDir + "/" + QUtils.editorUpdater;
                    string updateVersionUrlPath = "/" + QServer.updateDir + "/" + updaterVersionTag;
                    string remoteVersionUrlPath = "/" + QServer.updateDir + "/" + updaterMask + remoteVersion + QUtils.FileExtensions.Text;

                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Url: '" + updateUrlPath + "' file '" + devFileNameTxt.Text + "'");
                    var status = QServer.Upload(updateUrlPath, QUtils.editorUpdater);

                    if (status)
                    {
                        QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Editor files were uploaded successfully.");
                        QServer.Delete(remoteVersionUrlPath);
                        status = true;
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Removed previous version file successfully.");
                    }
                    if (status)
                    {
                        status = QServer.Upload(updateVersionUrlPath, updaterVersionTag);
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Uploading new version file for server.");
                    }

                    if (status)
                    {
                        devFileNameTxt.Text = editorUpdater;
                        devFileSizeTxt.Text = "File Size: " + fileSize + "Kb";
                        QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Update uploaded successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void aiCountTxt_TextChanged(object sender, EventArgs e)
        {
            HandleInvalidTextFmt(sender, 1, MAX_AI_COUNT, MethodBase.GetCurrentMethod().Name);
        }

        private void maxSpawnsTxt_TextChanged(object sender, EventArgs e)
        {
            HandleInvalidTextFmt(sender, 1, MAX_AI_COUNT, MethodBase.GetCurrentMethod().Name);
        }

        private void nodesInfoCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) nodesHilightCb.Checked = nodesObjectsCb.Checked = false;
        }

        private void movementSpeedTxt_TextChanged(object sender, EventArgs e)
        {
            HandleInvalidTextFmt(sender, 10.0f, float.PositiveInfinity, MethodBase.GetCurrentMethod().Name);
        }

        private void framesTxt_ValueChanged(object sender, EventArgs e)
        {
            var framesTxt = ((NumericUpDown)sender).Value.ToString();
            int frame = gameFPS = Convert.ToInt32(framesTxt);
            
			//Check for Max FPS.
            if (frame < MIN_FPS || frame > MAX_FPS)
            {
				QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Frames value is out of range.");
                ((NumericUpDown)sender).Value = frame = gameFPS = 30;
            }
        }

        private void aiJsonLoadBtn_Click(object sender, EventArgs e)
        {
            try
            {
                //Reset before Load File.
                aiJsonEditorTxt.Text = String.Empty;
                aiJsonEditorTxt.Clear();
                aiJsonEditorTxt.SelectionColor = Black;
                aiJsonEditorTxt.SelectionFont = new Font("Arial", 10);

                string aiJsonPath = QUtils.qedAiJsonPath;
                if (Directory.Exists(QUtils.qedAiJsonPath + "\\level" + gameLevel))
                    aiJsonPath = QUtils.qedAiJsonPath + "\\level" + gameLevel;

                fopenIO = QUtils.ShowOpenFileDlg("Select JSON File", ".json", "JSON File|*.json|Text file|*.txt", true, aiJsonPath);
                aiFileNameTxt.Text = Path.GetFileName(fopenIO.FileName);

                if (!String.IsNullOrEmpty(fopenIO.FileData))
                {
                    aiJsonEditorTxt.Text = fopenIO.FileData;
                    RichViewerUpdateFormat();
                    aiFileSizeTxt.Text = "File Size: " + fopenIO.FileSize + " Kb";
                }

                JToken parsedJson = JToken.Parse(aiFileNameTxt.Text);
                var beautified = parsedJson.ToString(Formatting.Indented);
                aiFileNameTxt.Text = beautified;

            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void gamePathBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (QUtils.ShowGamePathDialog() == DialogResult.OK)
                {

                    QUtils.FileIODelete(QUtils.editorAppName + QUtils.FileExtensions.Ini);

                    //Create new config.
                    QUtils.CreateConfig();

                    //Read paths from config.
                    QUtils.ParseConfig();

                    //Initialize app data for QEditor.
                    QUtils.InitEditorAppData();

                    if (QUtils.CheckShortcutExist())
                    {
                        if (QUtils.RemoveGameShortcut())
                        {
                            QUtils.Sleep(1);
                            if (QUtils.CreateGameShortcut())
                                startGameBtn_Click(sender, e);
                            else QUtils.ShowLogError(MethodBase.GetCurrentMethod().Name, "Error in creating new game shortcut");
                        }
                        else QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Error in removing existing shortcut");
                    }
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void aiJsonSaveAiBtn_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(fopenIO.FileData))
            {
                QUtils.ShowLogError(MethodBase.GetCurrentMethod().Name, "JSON Editor data is empty.", "JSON Editor.");
                return;
            }

            var dlgMsg = QUtils.ShowDialog("Do you want to save your all A.I data to A.I's list?");
            if (dlgMsg == DialogResult.Yes)
            {
                var humanAi = ReadHumanAiJSON(fopenIO.FileName);
                humanAiList.Add(humanAi);
                SetStatusText("Human A.I added to list.");
                if (aiJsonClearDataCb.Checked) aiJsonEditorTxt.Clear();
            }
        }

        private void aiJsonAddAiBtn_Click(object sender, EventArgs e)
        {
            if (humanAiList.Count == 0) QUtils.ShowLogError("AddHumanSoldierJSON", "No A.I JSON Files were added to list yet.", "JSON Editor.");
            else if (humanAiList.Count >= 1)
            {
                try
                {
                    foreach (var humanAi in humanAiList)
                    {
                        var aiModelName = QObjects.FindModelName(humanAi.model);
                        var dlgResult = QUtils.ShowDialog("You are about to Add " + aiModelName + "A.I confirm ?\nThis is manual editing so be carefuly about data you edit.", "JSON Editor");

                        if (dlgResult == DialogResult.Yes)
                        {
                            QUtils.AddLog("AddHumanSoldierJSON", "Level " + gameLevel + ", Model Name: " + aiModelName);
                            var qscData = QAI.AddHumanSoldier(humanAi);
                            if (String.IsNullOrEmpty(qscData))
                            {
                                QUtils.ShowLogStatus("AddHumanSoldierJSON", "Error: Adding " + aiModelName + " A.I to level '" + gameLevel + "'");
                                QUtils.aiScriptId = QUtils.aiScriptId > QUtils.LEVEL_FLOW_TASK_ID ? (QUtils.aiScriptId - 3) : QUtils.aiScriptId;//Reset scriptId on error.
                                return;
                            }
                            //Add task detection only if selected.
                            if (taskDetectionAiCb.Checked) qscData += QAI.AddAiTaskDetection(qscData);

                            if (!String.IsNullOrEmpty(qscData)) compileStatus = QCompiler.Compile(qscData, QUtils.gamePath, true, true); ;

                            if (compileStatus) SetStatusText("AI " + aiModelName + " Added successfully");
                        }
                    }
                }
                catch (Exception ex)
                {
                    QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
                }
            }

            humanAiList.Clear();
        }

        private void aiJsonSaveBtn_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(aiJsonEditorTxt.Text))
                QUtils.ShowLogError(MethodBase.GetCurrentMethod().Name, "File contents are empty or invalid.", "JSON Editor.");

            var dlgMsg = QUtils.ShowDialog("Do you want to save all contents of '" + aiFileNameTxt.Text + "'?");
            if (dlgMsg == DialogResult.Yes)
            {
                var data = aiJsonEditorTxt.Text;
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "File name: " + aiFileNameTxt.Text + " Path: " + fopenIO.FileName);
                QUtils.SaveFile(fopenIO.FileName, data);
                if (aiJsonClearDataCb.Checked) aiJsonEditorTxt.Clear();
                QUtils.ShowInfo("File " + aiFileNameTxt.Text + " was saved successfully");
            }
        }

        private void aiJsonEditorTxt_TextChanged(object sender, EventArgs e)
        {
            if (aiJsonAutoFmtCb.Checked)
                RichViewerUpdateFormat();
        }

        private void aiJsonEditorTxt_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.S))
            {
                e.IsInputKey = true;
                aiJsonSaveBtn_Click(sender, (EventArgs)e);
            }
            else if (e.KeyData == (Keys.Control | Keys.F))
            {
                e.IsInputKey = true;
                RichViewerUpdateFormat();
            }
            else if (e.KeyData == (Keys.Control | Keys.R))
            {
                ((RichTextBox)sender).Text = QUtils.LoadFile(Path.GetFullPath(aiFileNameTxt.Text));
            }
        }

        private void saveAIBtn_Click(object sender, EventArgs e)
        {
            string aiModelName = null, aiModel = null, aiWeaponMode = null, aiType = null, aiWeapon = null;
            int aiCount = 1, maxSpawns = 1, teamId = TEAM_ID_ENEMY;

            Invoke((Action)(() =>
            {
                aiModelName = QAI.GetAiModelNamesList(gameLevel)[aiModelSelectDD.SelectedIndex];
                aiModel = QAI.GetAiModelId4Name(aiModelName);
                aiWeapon = QUtils.weaponId + QUtils.weaponList[aiWeaponDD.SelectedIndex].Keys.ElementAt(0);
                aiGraphId = QUtils.aiGraphIdStr[aiGraphIdDD.SelectedIndex];
                aiType = QUtils.aiTypes[aiTypeDD.SelectedIndex];
                aiCount = Convert.ToInt32(aiCountTxt.Text);
                maxSpawns = Convert.ToInt32(maxSpawnsTxt.Text);
                teamId = Convert.ToInt32(teamIdText.Text);
            }));

            //Convert HumanAI obj to JSON.
            var humanAi = new HumanAi(aiCount, aiType, aiGraphId, aiWeapon, aiModel, guardGeneratorCb.Checked, maxSpawns, teamId, aiInvincibleCb.Checked, aiAdvanceViewCb.Checked);
            var humanJSON = JsonConvert.SerializeObject(humanAi, Formatting.Indented);

            var inputDlgMsg = DialogMsgBox.ShowBox("Enter A.I File name", aiModelName + "_" + gameLevel + QUtils.FileExtensions.Json, MsgBoxButtons.YesNo, true);
            if (inputDlgMsg == DialogResult.Yes)
            {
                var aiJsonFile = qedAiJsonPath + "\\level" + gameLevel + (aiFriendlyCb.Checked ? @"\friendly\" : @"\enemies\");
                if (!Directory.Exists(aiJsonFile))
                {
                    Directory.CreateDirectory(aiJsonFile);
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Created directory '" + aiJsonFile + "'");
                }

                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "AiJson Path: " + qedAiJsonPath + " AiJson File: " + aiJsonFile + " level: " + gameLevel);

                if (!Directory.Exists(qedAiJsonPath + "\\level" + gameLevel))
                    Directory.CreateDirectory(qedAiJsonPath + "\\level" + gameLevel);

                //Save the JSON A.I file.
                var inputFile = DialogMsgBox.GetInputBoxData() + QUtils.FileExtensions.Json;
                aiJsonFile += inputFile;
                QUtils.SaveFile(aiJsonFile, humanJSON);
                SetStatusText("File '" + inputFile + "' saved successfully.");
            }
        }

        private void autoRefreshGameCb_CheckedChanged(object sender, EventArgs e)
        {
            autoRefreshGameCb.Checked = !autoRefreshGameCb.Checked;
            QUtils.gameRefresh = autoRefreshGameCb.Checked;
            if (autoRefreshGameCb.Checked)
            {
                internalsAttachTimer.Start();
                levelRunTimer.Start();
                DialogMsgBox.ShowBox("Game Automatic Finder", "Game auto refresh enabled\nTimer Interval - 30s");
            }
            else
            {
                internalsAttachTimer.Stop();
                levelRunTimer.Stop();
                SetStatusText("Game auto refresh disabled");
            }
        }

        private void aiJsonEditModeCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                DialogMsgBox.ShowBox("JSON Editor", "This is manual editing so be careful\nEdit at your own risk.[BETA]");
                aiJsonEditorTxt.ReadOnly = false;
            }
            else aiJsonEditorTxt.ReadOnly = true;
        }

        private void internalCompilerCb_CheckedChanged(object sender, EventArgs e)
        {
            if (internalCompilerCb.Checked)
            {
                var dlgResult = QUtils.ShowDialog("Do you want to change Compiler to Internal?\nCompiler - Internal [Fast]\nRequires - Internals.dll", "Select Game Compiler");

                if (dlgResult == DialogResult.Yes)
                {
                    internalCompilerCb.Checked = QUtils.internalCompiler = true;
                    externalCompilerCb.Checked = QUtils.externalCompiler = false;
                    SetStatusText("Compiler changed to internal.");
                    compilerTypeLbl.Text = "internal";
                }
                else internalCompilerCb.Checked = false;
            }
            else if (!externalCompilerCb.Checked) internalCompilerCb.Checked = true;
        }

        private void externalCompilerCb_CheckedChanged(object sender, EventArgs e)
        {
            if (externalCompilerCb.Checked)
            {
                var dlgResult = QUtils.ShowDialog("Do you want to change Compiler to External?\nCompiler - External [Slow]\nRequires - GConv/DConv Tools.", "Select Game Compiler");

                if (dlgResult == DialogResult.Yes)
                {
                    if (QCompiler.CheckQCompilerExist())
                    {
                        externalCompilerCb.Checked = QUtils.externalCompiler = true;
                        internalCompilerCb.Checked = QUtils.internalCompiler = false;
                        SetStatusText("Compiler changed to external.");
                        compilerTypeLbl.Text = "external";
                    }
                    else
                    {
                        internalCompilerCb.Checked = QUtils.internalCompiler = true;
                        externalCompilerCb.Checked = false;
                    }
                }
                else externalCompilerCb.Checked = false;
            }
            else if (!internalCompilerCb.Checked) externalCompilerCb.Checked = true;
        }

        private void resetScriptsFileBtn_Click(object sender, EventArgs e)
        {
            QUtils.ResetScriptFile(gameLevel);
            SetStatusText("Reset of file 'objects.qsc' for level" + gameLevel + " done.");
        }

        private void aiEditorTabs_Selected(object sender, TabControlEventArgs e)
        {
            //A.I Editor.
            if (e.TabPage.Name == "aiEditorMainTab")
            {
                try
                {
                    UpdateUIComponent(aiGraphIdDD, QUtils.aiGraphIdStr);
                    if (aiTypeDD.Items.Count > 0 && aiModelSelectDD.Items.Count > 0 && aiWeaponDD.Items.Count > 0)
                        aiTypeDD.SelectedIndex = aiModelSelectDD.SelectedIndex = aiWeaponDD.SelectedIndex = 0;
                }
                catch (Exception ex) { QUtils.LogException(e.TabPage.Name.ToUpper(), ex); }
            }

            else if (e.TabPage.Name == "aiScriptEditor")
            {
                try
                {
                    DialogMsgBox.ShowBox("Script Editor.", "Script editor coming soon in next updates");
                }
                catch (Exception ex) { QUtils.LogException(e.TabPage.Name.ToUpper(), ex); }
            }

            else if (e.TabPage.Name == "aiPatrolPathEditor")
            {
                try
                {
                    DialogMsgBox.ShowBox("PatrolPath Editor.", "PatrolPath editor coming soon in next updates");
                }
                catch (Exception ex) { QUtils.LogException(e.TabPage.Name.ToUpper(), ex); }
            }

        }

        private void markWeaponsCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) allWeaponsCb.Checked = false;
        }

        private void allWeaponsCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) markWeaponsCb.Checked = false;
        }

        private void saveWeaponGroupBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string weaponGroupFileName = weaponGroupFileTxt.Text;
                if (weaponsGroupList.Count == 0)
                {
                    ShowLogStatus(MethodBase.GetCurrentMethod().Name, "No Weapons were added into the Group.");
                    return;
                }

                if (String.IsNullOrEmpty(weaponGroupFileName))
                {
                    ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Weapon group name cannot be empty.");
                    return;
                }

                string weaponsGroupJsonFile = weaponGroupFileName;
                string weaponJsonData = JsonConvert.SerializeObject(weaponsGroupList);
                var weaponsGroupDir = qWeaponsGroupPath + @"\" + weaponsGroupJsonFile + QUtils.FileExtensions.Json;
                QUtils.SaveFile(weaponsGroupDir, weaponJsonData);
                SetStatusText("Weapon Group '" + weaponsGroupJsonFile + "'  saved successfully");

                if (markWeaponsCb.Checked) markWeaponsCb.Checked = false;
                weaponsGroupList.Clear();
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void loadWeaponGroupBtn_Click(object sender, EventArgs e)
        {
            try
            {
                fopenIO = QUtils.ShowOpenFileDlg("Select JSON File", ".json", "JSON File|*.json|Text file|*.txt", true, qWeaponsGroupPath);
                var fileName = Path.GetFileName(fopenIO.FileName);
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "File '" + fileName + "' selected for WeaponGroup");
                string qscData = null;

                if (!String.IsNullOrEmpty(fopenIO.FileData))
                {
                    weaponGroupFileTxt.Text = fileName;
                    string json = fopenIO.FileData;
                    var weaponsGroup = JsonConvert.DeserializeObject<List<WeaponGroup>>(json);

                    foreach (var weapon in weaponsGroup)
                    {
                        var weaponModel = QUtils.weaponId + weapon.Weapon;
                        int weaponAmmo = weapon.Ammo;

                        qscData = QHuman.AddWeapon(weaponModel, weaponAmmo, true, true);
                        if (!String.IsNullOrEmpty(qscData))
                            QUtils.SaveFile(qscData);
                    }
                }
                if (!String.IsNullOrEmpty(qscData))
                    compileStatus = QCompiler.Compile(qscData, QUtils.gamePath, false, true);
                if (compileStatus)
                    SetStatusText("Weapon Group '" + fileName + "'  added successfully");
                else
                    SetStatusText("Error: Weapon Group '" + fileName + "' cannot be added.");
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void aiFriendlyCb_CheckedChanged(object sender, EventArgs e)
        {
            teamIdText.Value = (aiFriendlyCb.Checked) ? TEAM_ID_FRIENDLY : TEAM_ID_ENEMY;
        }

        private void showAppDataCachePathBtn_Click(object sender, EventArgs e)
        {
            QUtils.ShowPathExplorer(QUtils.cachePath);
            QUtils.ShowPathExplorer(QUtils.igiEditorQEdPath);
        }

        private bool UpdateWeaponProperties(bool updateWeaponDetails = false, bool updateWeaponUI = false, bool updateWeaponSFX = false, bool updateWeaponPower = false)
        {
            bool status = false;
            string qscData = QUtils.LoadFile(weaponConfigQSC);
            var weapon = QUtils.weaponDataList[weaponCfgDD.SelectedIndex];

            var qscDataSplit = qscData.Split(new string[] { QUtils.taskNew }, StringSplitOptions.None);
            string weaponSelected = weaponList[weaponCfgDD.SelectedIndex].Keys.ElementAt(0);

            //Trim the weapon data.
            weapon.weaponName = weapon.weaponName.Replace("\"", String.Empty);
            weapon.description = weapon.description.Replace("\"", String.Empty);
            weapon.soundSingle = weapon.soundSingle.Replace("\"", String.Empty);
            weapon.soundLoop = weapon.soundLoop.Replace("\"", String.Empty);

            if (updateWeaponDetails)
            {
                //Get Weapon description data.
                string weaponName = weaponNameTxt.Text;
                string weaponDescription = weaponDescriptionTxt.Text;

                //Update the Weapon description data.
                weapon.weaponName = weaponName;
                weapon.description = weaponDescription;
            }

            if (updateWeaponUI)
            {
                //Get Weapon UI data.
                string weaponSightType = weaponSightTypeDD.Text;
                string weaponDisplayType = weaponDisplayTypeDD.Text;

                //Update the Weapon UI data.
                weapon.crosshairType = QUtils.sightDisplayType + weaponSightType;
                weapon.ammoDispType = QUtils.ammoDisplayType + weaponDisplayType;
            }

            if (updateWeaponSFX)
            {
                //Get Weapon SFX data.
                string weaponSFX1 = weaponSfx1DD.Text;
                string weaponSFX2 = weaponSfx2DD.Text;

                //Update the Weapon SFX data.
                weapon.soundSingle = weaponSFX1;
                weapon.soundLoop = weaponSFX2;
            }


            if (updateWeaponPower)
            {
                //Get Weapon Power data.
                Single weaponDamage = Single.Parse(weaponDamageTxt.Text);
                Single weaponPower = Single.Parse(weaponPowerTxt.Text);
                Single weaponRange = Single.Parse(weaponRangeTxt.Text);
                Int32 weaponBullets = Int32.Parse(weaponBulletsTxt.Text);
                Int32 weaponRoundPerMinute = Int32.Parse(weaponRoundPerMinuteTxt.Text);
                Int32 weaponRoundPerClip = Int32.Parse(weaponRoundPerClipTxt.Text);

                //Update the Weapon Power data.
                weapon.damage = weaponDamage;
                weapon.power = weaponPower;
                weapon.bullets = weaponBullets;
                weapon.roundsPerMinute = weaponRoundPerMinute;
                weapon.roundsPerClip = weaponRoundPerClip;
                weapon.weaponRange = weaponRange;
            }

            bool hasItemReal32 = false;

            if (weaponSelected.Contains("MP5") || weaponSelected.Contains("M16")
                || weaponSelected.Contains("DRAG") || weaponSelected.Contains("GRENADE")
               || weaponSelected.Contains("PROXI") || weaponSelected.Contains("MEDIPACK")
               || weaponSelected.Contains("BINO")
                )
            {
                hasItemReal32 = true;
            }

            foreach (var data in qscDataSplit)
            {
                if (data.Contains(weaponSelected))
                {
                    int qtaskIndex = qscData.IndexOf(data);
                    int newlineIndex = qscData.IndexOf(QUtils.taskNew, qtaskIndex);

                    string objectTask = "(-1," + "\"WeaponConfig\",\"" + weapon.weaponName + "\"," + weapon.weaponId + "," + weapon.scriptId + ",\"" + weapon.weaponName
                        + "\"," + weapon.manufacturer + ",\"" + weapon.description + "\"," + weapon.typeEnum + "," + weapon.crosshairType + "," + weapon.ammoDispType
                       + "," + weapon.mass + "," + weapon.caliberId + "," + weapon.damage + "," + weapon.power + "," + weapon.reloadTime + "," + weapon.muzzleVelocity
                        + "," + weapon.bullets + "," + weapon.roundsPerMinute + "," + weapon.roundsPerClip + "," + weapon.burst + "," + weapon.minRandSpeed
                         + "," + weapon.maxRandSpeed + "," + weapon.fixViewX + "," + weapon.fixViewZ + "," + weapon.randViewX + "," + weapon.randViewZ
                          + "," + weapon.typeStr + "," + weapon.weaponRange + "," + weapon.weaponUsers + "," + weapon.weaponLength + "," + weapon.barrelLength
                           + "," + weapon.gunModel + "," + weapon.casingModel + "," + weapon.animStand + "," + weapon.animMove + "," + weapon.animFire1 + "," + weapon.animFire2 + "," + weapon.animFire3
                            + "," + weapon.animReload + "," + weapon.animUpperbodystand + "," + weapon.animUpperbodywalk + "," + weapon.animUpperbodycrouch + "," + weapon.animUpperbodycrouchrun
                            + "," + weapon.animUpperbodyrun + "," + weapon.animUpperbodyfire + "," + weapon.animUpperbodyreload + ",\"" + weapon.soundSingle + "\",\"" + weapon.soundLoop
                            + "\"," + weapon.detectionRange + "," + weapon.projectileTaskType + "," + weapon.weaponTaskType + "," + weapon.emptyOnClear.ToString().ToUpper() + ((hasItemReal32) ? "," : "),") + "\n";
                    qscData = qscData.Remove(qtaskIndex, newlineIndex - qtaskIndex).Insert(qtaskIndex, objectTask);
                    break;
                }
            }
            QUtils.SaveFile(weaponConfigQSC, qscData);//Save the file at the end.

            status = QCompiler.Compile(weaponConfigQSC, QUtils.weaponsGamePath, 0x0);//Start compiling.
            return status;
        }

        private void updateWeaponPropertiesBtn_Click(object sender, EventArgs e)
        {
            bool status = UpdateWeaponProperties(true, true, true, true);
            if (status)
            {
                QInternals.WeaponConfigRead();
                QUtils.ShowLogStatus("", "Weapon properties updated success");
            }
        }

        private void weaponCfgDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var weapon = QUtils.weaponDataList[weaponCfgDD.SelectedIndex];

                //Set Weapon description data.
                weaponNameTxt.Text = weapon.weaponName.Replace("\"", String.Empty);
                weaponDescriptionTxt.Text = weapon.description.Replace("\"", String.Empty);

                //Set Weapon UI data.
                weaponSightTypeDD.Text = weapon.crosshairType.Replace(QUtils.sightDisplayType, String.Empty);
                weaponDisplayTypeDD.Text = weapon.ammoDispType.Replace(QUtils.ammoDisplayType, String.Empty);

                // Set Weapon SFX Sample & Loop.
                weaponSfx1DD.SelectedItem = weapon.soundSingle.Replace("\"", String.Empty);
                weaponSfx2DD.SelectedItem = weapon.soundLoop.Replace("\"", String.Empty);

                // Set Weapon Power data.
                weaponDamageTxt.Text = weapon.damage.ToString();
                weaponPowerTxt.Text = weapon.power.ToString();
                weaponRangeTxt.Text = weapon.weaponRange.ToString();
                weaponBulletsTxt.Value = weapon.bullets;
                weaponRoundPerMinuteTxt.Value = weapon.roundsPerMinute;
                weaponRoundPerClipTxt.Value = weapon.roundsPerClip;
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void updateWeaponDetailsBtn_Click(object sender, EventArgs e)
        {
            bool status = UpdateWeaponProperties(true);
            if (status)
            {
                QInternals.WeaponConfigRead();
                QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Weapon details properties updated success");
            }
        }

        private void updateWeaponUIBtn_Click(object sender, EventArgs e)
        {
            bool status = UpdateWeaponProperties(false, true);
            if (status)
            {
                QInternals.WeaponConfigRead();
                QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Weapon UI properties updated success");
            }
        }

        private void updateWeaponSFXBtn_Click(object sender, EventArgs e)
        {
            bool status = UpdateWeaponProperties(false, false, true);
            if (status)
            {
                QInternals.WeaponConfigRead();
                QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Weapon SFX properties updated success");
            }
        }

        private void updateWeaponPowerDamageBtn_Click(object sender, EventArgs e)
        {
            bool status = UpdateWeaponProperties(false, false, false, true);
            if (status)
            {
                QInternals.WeaponConfigRead();
                QUtils.ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Weapon Power properties updated success");
            }
        }


        private void currentWeaponCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                weaponDD.SelectedIndex = QWeapon.WeaponUpdateCurrent();
                Application.DoEvents();
            }
        }

        private void weaponEditorMainTab_Click(object sender, EventArgs e)
        {
            if (currentWeaponCb.Checked)
            {
                weaponDD.SelectedIndex = QWeapon.WeaponUpdateCurrent();
                Application.DoEvents();
            }
        }

        private void resetWeaponBtn_Click(object sender, EventArgs e)
        {
            int weaponIndex = 0;
            if (currentWeaponCb.Checked)
            {
                weaponIndex = QWeapon.WeaponUpdateCurrent();
            }
            else
            {
                weaponIndex = weaponDD.SelectedIndex;
            }
            var weapon = QUtils.weaponDataList[weaponIndex];

            bool status = UpdateWeaponProperties();
            if (status)
            {
                QInternals.WeaponConfigRead();
                QUtils.ShowLogStatus("", "Weapon reset success");
            }
        }

        private void weaponCfgEditor_Click(object sender, EventArgs e)
        {
            if (currentWeaponCb.Checked)
            {
                weaponCfgDD.SelectedIndex = QWeapon.WeaponUpdateCurrent();
            }
            else
            {
                weaponCfgDD.SelectedIndex = weaponDD.SelectedIndex;
            }
            weaponCfgDD_SelectedIndexChanged(sender, e);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            aboutBtn_Click(sender, e);
        }

        private void liveEditorCb_Click(object sender, EventArgs e)
        {
            liveEditorCb.Checked = !liveEditorCb.Checked;
            string modeStatus = liveEditorCb.Checked ? "Enabled" : "Disabled";
            SetStatusText("Editor mode status is now  '" + modeStatus + " " + liveEditorCb.Text + "'");

            if (liveEditorCb.Checked)
            {
                liveEditorCb.ForeColor = SpringGreen;
                addBuildingBtn.Text = "Restore Building";
                addObjectBtn.Text = "Restore Object";
            }
            else
            {
                liveEditorCb.ForeColor = DeepSkyBlue;
                addBuildingBtn.Text = "Add Building";
                addObjectBtn.Text = "Add Object";
            }
            removeWeaponBtn.Enabled = !liveEditorCb.Checked;
        }

        private void editorOnlineCb_Click(object sender, EventArgs e)
        {
            if (editorOnlineCb.Checked)
            {
                editorOnlineCb.Text = "Online";
                editorOnline = true;
            }
            else
            {
                editorOnlineCb.Text = "Offline";
                editorOnline = false;
            }
            SetStatusText("Editor connection status is now '" + (!editorOnline).ToString() + "'");
            editorOnlineCb.Checked = !editorOnline;
        }

        private void posCoordCb_Click(object sender, EventArgs e)
        {
            posCoordCb.Checked = !posCoordCb.Checked;
            if (posCoordCb.Checked) posMetersCb.Checked = false; else if (!posMetersCb.Checked) posCoordCb.Checked = true;
        }

        private void posMetersCb_Click(object sender, EventArgs e)
        {
            posMetersCb.Checked = !posMetersCb.Checked;
            if (posMetersCb.Checked) posCoordCb.Checked = false; else if (!posCoordCb.Checked) posMetersCb.Checked = true;
        }

        private void startGameBtnMenu_Click(object sender, EventArgs e)
        {
            try
            {
                gameLevel = Convert.ToInt32(levelStartTxtMenu.Text.ToString());
                StartGameLevelNow(gameLevel);
                refreshGame_Click(sender, e);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void startWindowedGameBtn_Click(object sender, EventArgs e)
        {
            try
            {
                int level = Convert.ToInt32(levelStartTxt.Text);
                gameLevel = Convert.ToInt32(levelStartTxt.Text.ToString());

                RefreshUIComponents(gameLevel);
                InitEditorPaths(gameLevel);
                QUtils.graphAreas.Clear();
                CleanUpAiFiles();
                StartGameLevel(level, true);
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void editorModeCb_Click(object sender, EventArgs e)
        {
            editorModeCb.Checked = !editorModeCb.Checked;
            string modeStatus = editorModeCb.Checked ? "Enabled" : "Disabled";
            SetStatusText("Editor mode status is now  '" + modeStatus + " " + editorModeCb.Text + "'");

            if (editorModeCb.Checked)
            {
                QInternals.HumanFreeCam();
                QInternals.StatusMessageShow("Editor mode enabled. use Arrows keys to move ALT/SPACE change height");
                playModeCb.Checked = false;
            }
            else if (!playModeCb.Checked) editorModeCb.Checked = true;
        }

        private void playModeCb_Click(object sender, EventArgs e)
        {
            playModeCb.Checked = !playModeCb.Checked;
            string modeStatus = playModeCb.Checked ? "Enabled" : "Disabled";
            SetStatusText("Editor mode status is now  '" + modeStatus + " " + playModeCb.Text + "'");

            if (playModeCb.Checked)
            {
                GT.GT_SendKeyStroke("HOME");
                QUtils.Sleep(0.5f);
                QInternals.StatusMessageShow("Play mode enabled - Play level.");
                editorModeCb.Checked = false;
            }
            else if (!editorModeCb.Checked) playModeCb.Checked = true;
        }

        private void musicVolumeUpdateBtn_Click(object sender, EventArgs e)
        {
            var musicVolume = musicVolumeUpdateTxt.Text;
            float volume = Convert.ToSingle(musicVolume);
            if (volume < 0.0f || volume > 10.0f) musicVolume = "5.0";
            QInternals.MusicVolumeSet(musicVolume);
            SetStatusText("Music volume has been set to " + musicVolume);
        }

        private void sfxVolumeUpdateBtn_Click(object sender, EventArgs e)
        {
            var sfxVolume = sfxVolumeUpdateTxt.Text;
            float volume = Convert.ToSingle(sfxVolume);
            if (volume < 0.0f || volume > 10.0f) sfxVolume = "5.0";
            QInternals.MusicSFXVolumeSet(sfxVolume);
            SetStatusText("SFX volume has been set to " + sfxVolume);
        }

        private void framesTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setFramesBtn_Click(sender, e);
            }
        }

        private void musicVolumeUpdateTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                musicVolumeUpdateBtn_Click(sender, e);
            }
        }

        private void sfxVolumeUpdateTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                sfxVolumeUpdateBtn_Click(sender, e);
            }
        }

        private void shareAppLogsBtn_Click(object sender, EventArgs e)
        {
            string nppCmd = (QUtils.nppInstalled) ? "notepad++ -titleAdd=\"IGI Editor Logs\" -nosession -notabbar -alwaysOnTop -lcpp " : "notepad ";
            var appLogFile = Path.GetFullPath(editorAppName + ".log");
            string appLogPath = (File.Exists(appLogFile)) ? appLogFile : QUtils.cachePathAppLogs + editorAppName + ".log";

            string mailToUrl = @"mailto:igiproz.hm@gmail.com?subject=IGI%20Editor%20Logs&body=Hi%2Ci%20have%20encountered%20error"
            + @"%20while%20using%20editor%20please%20check%20the%20logs%20attached.%0D%0APlease%20attach%20the%20Logs%20file%20located%20at"
            + @"%20'" + appLogPath + @"'%20location%20in%20the%20attachment%20to%20this%20email.";
            QUtils.ShellExecUrl(mailToUrl);
            QUtils.ShowWarning("Please attach the log file located at '" + appLogPath + "' with this email.");
        }

        private void viewAppLogsBtn_Click(object sender, EventArgs e)
        {
            string nppCmd = (QUtils.nppInstalled) ? "notepad++ -titleAdd=\"IGI Editor Logs\" -nosession -notabbar -alwaysOnTop -lcpp " : "notepad ";
            var appLogFile = Path.GetFullPath(editorAppName + ".log");
            string appLogPath = (File.Exists(appLogFile)) ? appLogFile : QUtils.cachePathAppLogs + editorAppName + ".log";

            if (File.Exists(appLogFile))
            {
                QUtils.ShellExec(nppCmd + appLogFile, false, false);
                QUtils.ShowPathExplorer(QUtils.editorCurrPath);
            }
            else
            {
                QUtils.ShellExec(nppCmd + QUtils.appLogFileTmp + editorAppName + ".log", false, false);
                QUtils.ShowPathExplorer(QUtils.cachePathAppLogs);
            }

        }

        private void updateIntervalTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                updateCheckerCb_CheckedChanged(sender, e);
            }
        }

        private void updateCheckerCb_Click(object sender, EventArgs e)
        {
            updateCheckerCb.Checked = !updateCheckerCb.Checked;
        }

        private void updateIntervalTxt_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var updateTimeTxt = updateIntervalTxt.ToString();
                QUtils.updateTimeInterval = Convert.ToInt32(updateTimeTxt);
                //Check for Max Update time
                if (QUtils.updateTimeInterval < 0 || QUtils.updateTimeInterval > MAX_UPDATE_TIME)
                {
                    updateIntervalTxt.Text = "5"; QUtils.updateTimeInterval = 5;
                }
            }
            catch (Exception ex)
            {
                QUtils.updateTimeInterval = MAX_UPDATE_TIME;
                updateIntervalTxt.Text = QUtils.updateTimeInterval.ToString();
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void saveWeaponProps_Click(object sender, EventArgs e)
        {
            string weaponNameTxtValue = weaponNameTxt.Text;
            string weaponDescriptionTxtValue = weaponDescriptionTxt.Text;
            string weaponSightTypeDDValue = weaponSightTypeDD.Text;
            string weaponDisplayTypeDDValue = weaponDisplayTypeDD.Text;
            string weaponSfx1DDValue = weaponSfx1DD.Text;
            string weaponSfx2DDValue = weaponSfx2DD.Text;
            string weaponDamageTxtValue = weaponDamageTxt.Text;
            string weaponPowerTxtValue = weaponPowerTxt.Text;
            string weaponRangeTxtValue = weaponRangeTxt.Text;
            string weaponBulletsTxtValue = weaponBulletsTxt.Text;
            string weaponRoundPerMinuteTxtValue = weaponRoundPerMinuteTxt.Text;
            string weaponRoundPerClipTxtValue = weaponRoundPerClipTxt.Text;

            Dictionary<string, string> weaponProperties = new Dictionary<string, string>
    {
        { "weaponNameTxt", weaponNameTxtValue },
        { "weaponDescriptionTxt", weaponDescriptionTxtValue },
        { "weaponSightTypeDD", weaponSightTypeDDValue },
        { "weaponDisplayTypeDD", weaponDisplayTypeDDValue },
        { "weaponSfx1DD", weaponSfx1DDValue },
        { "weaponSfx2DD", weaponSfx2DDValue },
        { "weaponDamageTxt", weaponDamageTxtValue },
        { "weaponPowerTxt", weaponPowerTxtValue },
        { "weaponRangeTxt", weaponRangeTxtValue },
        { "weaponBulletsTxt", weaponBulletsTxtValue },
        { "weaponRoundPerMinuteTxt", weaponRoundPerMinuteTxtValue },
        { "weaponRoundPerClipTxt", weaponRoundPerClipTxtValue }
    };

            string weaponPropertiesJson = JsonConvert.SerializeObject(weaponProperties, Formatting.Indented);
            var dialogMsgResult = DialogMsgBox.ShowBox("Save Weapon File", "Enter file name", MsgBoxButtons.YesNo, showInput: true, inputHidden: false);
            if (dialogMsgResult == DialogResult.Yes)
            {
                string fileName = DialogMsgBox.GetInputBoxData();
                if (!string.IsNullOrEmpty(fileName))
                {
                    string directoryPath = QUtils.qWeaponsModPath;
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    string filePath = Path.Combine(directoryPath, fileName + ".json");
                    QUtils.SaveFile(filePath, weaponPropertiesJson);
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Weapon properties saved to file: " + fileName);
                }
            }
        }

        private void loadWeaponProps_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the file weapon file name.
                var fopenIO = QUtils.ShowOpenFileDlg("Select Weapon File", ".json", "JSON File|*.json|Text file|*.txt", true, QUtils.qWeaponsModPath);
                string fileName = fopenIO.FileName;

                if (File.Exists(fileName))
                {
                    string weaponPropertiesJson = QUtils.LoadFile(fileName);
                    Dictionary<string, string> weaponProperties = JsonConvert.DeserializeObject<Dictionary<string, string>>(weaponPropertiesJson);

                    weaponNameTxt.Text = weaponProperties["weaponNameTxt"];
                    weaponDescriptionTxt.Text = weaponProperties["weaponDescriptionTxt"];
                    weaponSightTypeDD.Text = weaponProperties["weaponSightTypeDD"];
                    weaponDisplayTypeDD.Text = weaponProperties["weaponDisplayTypeDD"];
                    weaponSfx1DD.Text = weaponProperties["weaponSfx1DD"];
                    weaponSfx2DD.Text = weaponProperties["weaponSfx2DD"];
                    weaponDamageTxt.Text = weaponProperties["weaponDamageTxt"];
                    weaponPowerTxt.Text = weaponProperties["weaponPowerTxt"];
                    weaponRangeTxt.Text = weaponProperties["weaponRangeTxt"];
                    weaponBulletsTxt.Text = weaponProperties["weaponBulletsTxt"];
                    weaponRoundPerMinuteTxt.Text = weaponProperties["weaponRoundPerMinuteTxt"];
                    weaponRoundPerClipTxt.Text = weaponProperties["weaponRoundPerClipTxt"];

                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Weapon properties loaded from file: " + fileName);
                }
                else
                {
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Weapon properties file not found: " + fileName);
                }
            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        static bool ConvertToTga(string inputFilePath, string resolution)
        {
            bool status = false;
            int.TryParse(resolution.Split('x')[0].Trim(), out int width);
            int.TryParse(resolution.Split('x')[1].Trim(), out int height);

            // Run TGAConv to convert TGA files to PNG
            string inputFileWithoutExt = Path.Combine(Path.GetDirectoryName(inputFilePath), Path.GetFileNameWithoutExtension(inputFilePath));
            string tgaConvPath = Path.Combine(QUtils.qTools, @"TGAConv");
            string tgaConvExePath = Path.Combine(tgaConvPath, "tgaconv.exe");

            string tgaConvArgs = $"{inputFilePath} -ToTga --resize {width} {height}";
            string tgaConvDir = Path.Combine(QUtils.qTools, @"TGAConv");
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Running TGAConv: {tgaConvExePath} {tgaConvArgs} in directory {tgaConvDir}");
            QUtils.ShellExec($"cd {tgaConvDir} && {tgaConvExePath} {tgaConvArgs}");

            string tgaFilePath = Path.Combine(tgaConvPath, inputFileWithoutExt + ".tga");
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Input TGA Path: " + tgaFilePath);
            string destTgaFilePath = Path.Combine(tgaConvPath, Path.GetFileName(tgaFilePath));

            if (File.Exists(tgaFilePath) && !File.Exists(destTgaFilePath))
            {
                QUtils.FileMove(tgaFilePath, destTgaFilePath);
            }

            if (!File.Exists(destTgaFilePath))
            {
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Error: TGAConv failed to convert {inputFilePath} to TGA format");
                return status;
            }
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"TGAConv conversion of {inputFilePath} to TGA format completed successfully");
            status = true;
            return status;
        }



        private string ConvertTextureImage(string textureFilePath, bool resourceFile = false)
        {
            try
            {
                // Clear the PictureBox
                textureBox.Image = null;
                string sourceDir = null;
                string destDir = null;

                // Move all files from source path to DConv input directory
                if (!resourceFile)
                {
                    sourceDir = Path.GetDirectoryName(textureFilePath);
                }
                else
                {
                    sourceDir = textureFilePath;
                }
                destDir = Path.Combine(QUtils.qTools, @"DConv\input");

                // Copy all files to DConv directory.
                Directory.CreateDirectory(destDir);
                Directory.GetFiles(sourceDir).ToList().ForEach(f => File.Copy(f, Path.Combine(destDir, Path.GetFileName(f)), true));

                // Run DConv to convert files to TGA
                string dconvPath = Path.Combine(QUtils.qTools, @"DConv\dconv.exe");
                string dconvArgs = "tex convert input output";
                string dconvDir = Path.Combine(QUtils.qTools, @"DConv");
                QUtils.ShellExec($"cd {dconvDir} && {dconvPath} {dconvArgs}");
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "DConv conversion completed");

                // Move TGA files from DConv output directory to TGAConv directory
                string tgaConvPath = Path.Combine(QUtils.qTools, @"TGAConv");
                destDir = Path.Combine(tgaConvPath, "");
                foreach (string file in Directory.GetFiles(Path.Combine(QUtils.qTools, @"DConv\output"), "*.tga"))
                {
                    string fileName = Path.GetFileName(file);
                    string destination = Path.Combine(destDir, fileName);
                    QUtils.FileMove(file, destination);
                    //QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Moved TGA file to TGAConv directory: {destination}");
                }

                // Run TGAConv to convert TGA files to PNG
                string tgaConvExePath = Path.Combine(tgaConvPath, "tgaconv.exe");
                string tgaConvArgs = "*.tga -ToPng";
                string tgaConvDir = Path.Combine(QUtils.qTools, @"TGAConv");
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Running TGAConv: {tgaConvExePath} {tgaConvArgs} in directory {tgaConvDir}");
                QUtils.ShellExec($"cd {tgaConvDir} && {tgaConvExePath} {tgaConvArgs}");
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "TGAConv conversion completed");

                // Return the path to the TGAConv directory
                return tgaConvPath;
            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
                return null;
            }
        }


        private void SetTextureImage(string textureFile)
        {
            string sourceFileNameWithoutExt = Path.GetFileNameWithoutExtension(textureFile);
            // Convert the texture image.
            //string convPath = ConvertTextureImage(textureFile);

            // Load output TGA file into picture box
            //string pngFilePath = Path.Combine(convPath, $"{sourceFileNameWithoutExt}.png");
            if (File.Exists(textureFile))
            {
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Output PNG file: {textureFile}");
                Bitmap bitmap = new Bitmap(textureFile);
                textureBox.Image = bitmap;

                // Set image in PictureBox
                textureBox.Image = bitmap;
                textureFileName.Text = Path.GetFileName(textureFile);
                textureFileResolution.Text = $"{bitmap.Width} x {bitmap.Height}";
                textureFileSize.Text = $"{new FileInfo(textureFile).Length} bytes";
            }
            else
            {
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "No output PNG file found");
            }
        }


        private void selectTexturesBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // Clean the temp data first.
                QUtils.CleanUpTmpFiles();
                textureSelectedPath = null;
                texFiles = null;
                texIndex = 0;

                var folderBrowser = new OpenFileDialog();
                folderBrowser.ValidateNames = false;
                folderBrowser.CheckFileExists = false;
                folderBrowser.CheckPathExists = true;
                folderBrowser.FileName = "Folder Selection.";
                folderBrowser.Title = "Select Texture path";

                var folderBrowserDlg = folderBrowser.ShowDialog();
                if (folderBrowserDlg == DialogResult.OK)
                {
                    textureSelectedPath = Path.GetDirectoryName(folderBrowser.FileName) + Path.DirectorySeparatorChar;
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"selectedPath: {textureSelectedPath}");
                    string outputPath = null;

                    if (folderBrowser.FileName.Contains(".res"))
                    {
                        QUtils.ShowWarning("Resource file needs to be unpacked first.");
                        UnpackResourceFile(folderBrowser.FileName);
                        SetStatusText($"File {folderBrowser.FileName} unpacked success");
                        var basePathName = Path.GetFileName(Path.GetDirectoryName(folderBrowser.FileName));
                        textureSelectedPath += Path.DirectorySeparatorChar + basePathName;
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"After Unpacking new path: {textureSelectedPath}");

                        outputPath = ConvertTextureImage(textureSelectedPath, true);
                        texFiles = Directory.GetFiles(outputPath, "*.png");
                    }
                    else
                    {
                        outputPath = ConvertTextureImage(textureSelectedPath);
                        texFiles = Directory.GetFiles(outputPath, "*.png");
                    }

                    if (texFiles.Length > 0)
                    {
                        SetStatusText("All textures were loaded successfully.");
                        SetTextureImage(texFiles[0]);
                    }
                    else
                    {
                        SetStatusText("Textures failed to load from path");
                    }
                }
            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void ConvertTextureImage(string inputFilePath, string outputDirectoryPath, string convertType)
        {
            try
            {
                // Get the full path of the image file
                string imagePath = textureBox.ImageLocation;

                // Delete the file
                if (!string.IsNullOrEmpty(imagePath))
                {
                    File.Delete(imagePath);
                    textureBox.Image = null;
                    textureFileName.Text = "";
                    textureFileResolution.Text = "";
                    textureFileSize.Text = "";
                }

                string sourceFileNameWithoutExt = Path.GetFileNameWithoutExtension(inputFilePath);
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Selected output directory: {outputDirectoryPath}");

                // Run TGAConv to convert JPG/PNG to TGA.
                string tgaConvPath = Path.Combine(QUtils.qTools, @"TGAConv");
                string tgaResolutionSize = textureFileResolution.Text;
                bool convertStatus = ConvertToTga(inputFilePath, tgaResolutionSize);

                if (!convertStatus)
                {
                    SetStatusText("Error while converting Textures.");
                    return;
                }

                string makeTexCmd = null;
                string makeScriptPath = null;
                string inputConvertPath = null;
                string outputConvertPath = null;

                if (convertType == "texture")
                {
                    // Generate MakeTex script
                    makeScriptPath = Path.Combine(QUtils.qTools, "maketex.qsc");
                    string tgaFilePath = Path.Combine(tgaConvPath, $"{sourceFileNameWithoutExt}.tga");
                    string texFilePath = Path.Combine(outputDirectoryPath, $"{sourceFileNameWithoutExt}.tex");
                    makeTexCmd = $"MakeTexture(\"{$"{sourceFileNameWithoutExt}.tga"}\", \"{$"{sourceFileNameWithoutExt}.tex"}\");";
                    File.WriteAllText(makeScriptPath, makeTexCmd + "\r\n");
                    inputConvertPath = Path.GetDirectoryName(tgaFilePath);
                    outputConvertPath = Path.GetDirectoryName(texFilePath);
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Added MakeTex command: {makeTexCmd}");
                }

                else if (convertType == "sprite")
                {
                    // Generate MakeTex script
                    makeScriptPath = Path.Combine(QUtils.qTools, "makespr.qsc");
                    string tgaFilePath = Path.Combine(tgaConvPath, $"{sourceFileNameWithoutExt}.tga");
                    string sprFilePath = Path.Combine(outputDirectoryPath, $"{sourceFileNameWithoutExt}.spr");
                    makeTexCmd = $"MakeSprite(\"{$"{sourceFileNameWithoutExt}.tga"}\", \"{$"{sourceFileNameWithoutExt}.spr"}\");";
                    File.WriteAllText(makeScriptPath, makeTexCmd + "\r\n");
                    inputConvertPath = Path.GetDirectoryName(tgaFilePath);
                    outputConvertPath = Path.GetDirectoryName(sprFilePath);
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Added MakeTex command: {makeTexCmd}");
                }

                else if (convertType == "pic")
                {
                    // Generate MakeTex script
                    makeScriptPath = Path.Combine(QUtils.qTools, "makepic.qsc");
                    string tgaFilePath = Path.Combine(tgaConvPath, $"{sourceFileNameWithoutExt}.tga");
                    string picFilePath = Path.Combine(outputDirectoryPath, $"{sourceFileNameWithoutExt}.pic");
                    makeTexCmd = $"MakePicture(\"{$"{sourceFileNameWithoutExt}.tga"}\", \"{$"{sourceFileNameWithoutExt}.pic"}\");";
                    File.WriteAllText(makeScriptPath, makeTexCmd + "\r\n");
                    inputConvertPath = Path.GetDirectoryName(tgaFilePath);
                    outputConvertPath = Path.GetDirectoryName(picFilePath);
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Added MakeTex command: {makeTexCmd}");
                }

                // Run GConv to generate game resource file
                string gconvPath = Path.Combine(QUtils.qTools, @"GConv\gconv.exe");
                string gconvArgs = $"\"{makeScriptPath}\" -InputPath={inputConvertPath} -OutputPath={outputConvertPath}";
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Running GConv: {gconvPath} {gconvArgs}");
                QUtils.ShellExec($"{gconvPath} {gconvArgs}");
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "GConv conversion completed");

                // Clean up directories
                File.Delete(makeScriptPath);
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Cleaning up directories");

                string textureBoxImagePath = texFiles[texIndex];
                string textureBoxImage = Path.GetFileName(textureBoxImagePath);
                outputConvertPath = outputDirectoryPath + Path.ChangeExtension(textureBoxImage, "tex");

                string destTexFile = sourceFileNameWithoutExt + ".tex";

                // Get the directory path of the output file
                string outputDirectory = Path.GetDirectoryName(outputConvertPath);

                // Combine the output directory with the new file name to create the new file path
                string newFilePath = Path.Combine(outputDirectory, destTexFile);

                // Check if the new file already exists, and delete it if it does
                if (File.Exists(outputConvertPath))
                {
                    File.Delete(outputConvertPath);
                }

                // Rename the output file to the new file name
                File.Move(newFilePath, outputConvertPath);


                SetStatusText($"Resource {sourceFileNameWithoutExt} saved as texture successfully.");
            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }


        private void saveTextureBtn_Click(object sender, EventArgs e)
        {

        }

        private void packResourceBtn_Click(object sender, EventArgs e)
        {
            // Select QSC file using file dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Resource script files (*.qsc)|*.qsc|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string scriptPath = openFileDialog.FileName;
                string sourceFileNameWithoutExt = Path.GetFileNameWithoutExtension(scriptPath);
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Selected GConv script file: {scriptPath}");
                PackResourceFile(scriptPath);
            }
        }

        private void PackResourceFile(string resourceFile)
        {
            // Get the input path from the QSC file
            string inputPath = Path.GetDirectoryName(resourceFile);
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Input path: {inputPath}");
            string sourceFileNameWithoutExt = Path.GetFileNameWithoutExtension(resourceFile);

            // Run GConv to process the script
            string gconvPath = Path.Combine(QUtils.qTools, @"GConv\gconv.exe");
            string gconvDir = Path.Combine(QUtils.qTools, @"GConv");
            string gconvArgs = $"\"{resourceFile}\" -InputPath=\"{inputPath}\"";
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Running GConv: {gconvPath} {gconvArgs}");
            QUtils.ShellExec($"cd {gconvDir} && {gconvPath} {gconvArgs}");
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "GConv script execution completed");
            SetStatusText($"Resource {sourceFileNameWithoutExt} packed successfully.");
        }

        private void UnpackResourceFile(string resourceFile)
        {

            // Create input and output directories
            string inputDirectoryPath = Path.GetDirectoryName(resourceFile);
            string outputDirectoryPath = inputDirectoryPath;
            string sourceFileNameWithoutExt = Path.GetFileNameWithoutExtension(resourceFile);

            // Generate decompile script
            string decompileScriptPath = Path.Combine(QUtils.qTools, "decompile.qsc");
            string decompileCmd = $"ExtractResource(\"{Path.GetFileName(resourceFile)}\");";
            File.WriteAllText(decompileScriptPath, decompileCmd);
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Created decompile script: {decompileCmd}");

            // Run GConv to decompile the resource file
            string gconvPath = Path.Combine(QUtils.qTools, @"GConv\gconv.exe");
            string gconvArgs = $"\"{decompileScriptPath}\" -InputPath=\"{inputDirectoryPath}\" -OutputPath=\"{outputDirectoryPath}\"";
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Running GConv: {gconvPath} {gconvArgs}");
            QUtils.ShellExec($"{gconvPath} {gconvArgs}");
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "GConv decompilation completed");

            // Delete decompile script
            File.Delete(decompileScriptPath);
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Deleted decompile script: {decompileScriptPath}");
            SetStatusText($"Resource {sourceFileNameWithoutExt} unpacked successfully.");
        }

        private void unpackResourceBtn_Click(object sender, EventArgs e)
        {
            // Select resource file using file dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Resource files (*.res)|*.res|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string sourcePath = openFileDialog.FileName;
                string sourceFileNameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Selected resource file: {sourcePath}");
                UnpackResourceFile(sourcePath);
            }
        }


        private Point lastPoint;
        private bool isDrawing = false;

        private void textureBox_MouseDown(object sender, MouseEventArgs e)
        {
            isDrawing = true;
            lastPoint = e.Location;
        }

        private void textureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                using (Graphics g = textureBox.CreateGraphics())
                {
                    g.DrawLine(Pens.Black, lastPoint, e.Location);
                }
                lastPoint = e.Location;
            }
        }

        private void textureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDrawing = false;
            if (textureBox == null || textureBox.Image == null) return;

            Bitmap bitmap = new Bitmap(textureBox.Image);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawLine(Pens.Black, lastPoint, e.Location);
            }
            textureBox.Image = bitmap;
        }

        private void clearTempToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.CleanUpTmpFiles();
            SetStatusText("Temp data cleared success.");
        }

        private void nextTextureBtn_Click(object sender, EventArgs e)
        {
            texIndex++;
            if (texIndex >= texFiles.Length)
            {
                texIndex = 0;
            }
            SetTextureImage(texFiles[texIndex]);
        }

        private void prevTextureBtn_Click(object sender, EventArgs e)
        {
            texIndex--;
            if (texIndex < 0)
            {
                texIndex = texFiles.Length - 1;
            }
            SetTextureImage(texFiles[texIndex]);
        }

        private void replaceTextureBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // Clear the PictureBox
                //textureBox.Image = null;

                // Select image file using file dialog
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Image files (*.jpg, *.png)|*.jpg;*;*.jpeg;.png;|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Selecting the source image file.
                    string inputFilePath = openFileDialog.FileName;
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Selected image file: {inputFilePath}");

                    // Get filename and file extension
                    string filename = Path.GetFileName(inputFilePath);


                    // Get output directory path
                    string outputDirectoryPath = textureSelectedPath;

                    // Get the first file in the output directory
                    string[] files = Directory.GetFiles(outputDirectoryPath);
                    string firstFile = files.Length > 0 ? files[0] : string.Empty;

                    // Get the extension of the first file
                    string extension = Path.GetExtension(firstFile);

                    // Determine convert type based on file extension.
                    string convertType = ".sprite";
                    switch (extension)
                    {
                        case ".spr":
                            convertType = "sprite";
                            break;
                        case ".tex":
                            convertType = "texture";
                            break;
                        case ".pic":
                            convertType = "pic";
                            break;
                    }

                    // Convert image to texture or sprite or pic
                    ConvertTextureImage(inputFilePath, outputDirectoryPath, convertType);

                    // Load output image into picture box
                    string outputFilePath = Path.Combine(outputDirectoryPath, $"{filename}");
                    if (File.Exists(outputFilePath))
                    {
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Output PNG file: {outputFilePath}");
                        Bitmap bitmap = new Bitmap(outputFilePath);
                        textureBox.Image = bitmap;
                        textureFileName.Text = filename;
                        textureFileResolution.Text = $"{bitmap.Width}x{bitmap.Height}";
                        textureFileSize.Text = $"{new FileInfo(outputFilePath).Length / 1024.0:F2} KB";
                        SetStatusText($"File {filename} loaded as {convertType} successfully.");
                    }
                    else
                    {
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "No output PNG file found");
                    }
                }
            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var voidLevelPath = QUtils.cfgVoidPath + "\\objects_void_" + QUtils.gGameLevel + QUtils.FileExtensions.Qsc;
            QUtils.FileCopy(voidLevelPath, QUtils.objectsQsc);

            var qscData = QUtils.LoadFile();

            QUtils.levelFlowData = File.ReadLines(QUtils.objectsQsc).Last();
            compileStatus = QCompiler.CompileEx(qscData);

            if (compileStatus) SetStatusText("Level cleared out success");
        }

        private void graphsMarkCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) graphsAllCb.Checked = false;
            else QUtils.graphdIdsMarked.Clear();//Clear marked GraphIds.
        }

        private void levelStartTxt_ValueChanged(object sender, EventArgs e)
        {
            int level = Convert.ToInt32(levelStartTxt.Value);
            //Check for Max Level.
            if (level <= 0 || level > GAME_MAX_LEVEL)
            {
                levelStartTxt.Value = level = 1;
            }

            QUtils.gGameLevel = gameLevel = QUtils.gameFound ? QMemory.GetRunningLevel() : level;
            RefreshGame(false, false);
        }

        private void missionsOnlineDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!QUtils.editorOnline)
            {
                QUtils.ShowWarning("Editor is not in online mode to get missions list from server.");
                return;
            }

            if (QUtils.qServerMissionDataList.Count > 0)
            {
                int index = missionsOnlineDD.SelectedIndex;
                missionData = QUtils.qServerMissionDataList[index];

                missionSizeLbl.Text = "Size " + missionData.MissionSize.ToString() + " Kb";
                missionLevelLbl.Text = "Level: " + missionData.MissionLevel.ToString();
                missionAuthorLbl.Text = "Author: " + missionData.MissionAuthor.ToString();
            }
        }

        private void autoTeleportNodeCb_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                stopTraversingNodesBtn.Enabled = true;
                teleportToNodeBtn.Text = "Traverse Nodes";
                manualTeleportNodeCb.Checked = false;
            }
        }

        private void downloadMissionBtn_Click(object sender, EventArgs e)
        {
            downloadMissionWorker.RunWorkerAsync();
        }

        private void uploadMissionBtn_Click(object sender, EventArgs e)
        {
            uploadMissionWorker.RunWorkerAsync();
        }

        private void nodeIdDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (nodeIdDD.SelectedIndex == -1) nodeIdDD.SelectedIndex = 0;
            nodeId = QUtils.aiGraphNodeIdStr[nodeIdDD.SelectedIndex];
            graphNode = QGraphs.GetGraphNodeData(aiGraphId, nodeId);

            //Setting Node properties.
            nodeCriteriaTxt.Text = graphNode.MGraphVertexNodes.LastOrDefault().NodeCriteria;

            if (nodeIdOffsetCb.Checked)
            {
                nodeXTxt.Text = graphNode.MGraphVertexNodes.LastOrDefault().NodePos.x.ToString("0.0000");
                nodeYTxt.Text = graphNode.MGraphVertexNodes.LastOrDefault().NodePos.y.ToString("0.0000");
                nodeZTxt.Text = graphNode.MGraphVertexNodes.LastOrDefault().NodePos.z.ToString("0.0000");
            }
            else
            {
                nodeXTxt.Text = graphPos.x + graphNode.MGraphVertexNodes.LastOrDefault().NodePos.x.ToString("R");
                nodeYTxt.Text = graphPos.y + graphNode.MGraphVertexNodes.LastOrDefault().NodePos.y.ToString("R");
                nodeZTxt.Text = graphPos.z + graphNode.MGraphVertexNodes.LastOrDefault().NodePos.z.ToString("R");
            }

            //Get Node Real Position.
            nodeRealPos = new Real64();
            nodeRealPos.x = graphPos.x + graphNode.MGraphVertexNodes.LastOrDefault().NodePos.x;
            nodeRealPos.y = graphPos.y + graphNode.MGraphVertexNodes.LastOrDefault().NodePos.y;
            nodeRealPos.z = graphPos.z + graphNode.MGraphVertexNodes.LastOrDefault().NodePos.z + QUtils.viewPortDelta;
        }

        private void appSupportBtn_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(QUtils.supportDiscordLink);
            System.Diagnostics.Process.Start(QUtils.supportYoutubeLink);
            System.Diagnostics.Process.Start(QUtils.supportVKLink);
        }

        private void StartGameLevel(int level, bool windowed = true)
        {
            try
            {
                if (level <= 0)
                {
                    QUtils.AddLog("IGIEditorUI.StartLevel", $"Invalid level [{level}] received, defaulting to 1");
                    level = 1;
                }
                //Reset only if checked.
                if (QUtils.gameReset || autoResetCb.Checked)
                {
                    QUtils.RestoreLevel(level);
                    QUtils.ResetScriptFile(level);
                }

                //Start new level.
                QMemory.StartLevel(level, windowed);
                QUtils.gameFound = true;
                QUtils.Sleep(5);

                //Load level details and update UI.
                LoadLevelDetails(level);
                RefreshUIComponents(level);

                GenerateAIScriptId(true);
                QUtils.aiScriptFiles.Clear();
                QUtils.humanAiList.Clear();

                RefreshUIComponents(level);
                QUtils.AttachInternals();
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        private void RefreshUIComponents(int level, bool objItems = true, bool aiItems = true, bool missionItems = true)
        {
            try
            {
                InitUIComponents(level, false, objItems, aiItems, missionItems);
                UpdateUIComponent(buildingSelectDD, QUtils.buildingListStr);
                UpdateUIComponent(objectSelectDD, QUtils.objectRigidListStr);
                UpdateUIComponent(aiModelSelectDD, QUtils.aiModelsListStr);
                UpdateUIComponent(aiGraphIdDD, QUtils.aiGraphIdStr);
                UpdateUIComponent(graphIdDD, QUtils.aiGraphIdStr);
                UpdateUIComponent(nodeIdDD, QUtils.aiGraphNodeIdStr);
            }
            catch (Exception ex) { }
        }

        //Generic Update UI method for DropDowns,TextBox etc.
        private void UpdateUIComponent<T>(ComboBox itemDD, List<T> dataSrcList)
        {
            try
            {
                itemDD.DataSource = null;
                itemDD.Items.Clear();
                itemDD.DataSource = dataSrcList;
                itemDD.Invalidate();
                itemDD.Update();
                itemDD.Refresh();
                if (itemDD.Items.Count > 0)
                    itemDD.SelectedIndex = 0;
                Application.DoEvents();
            }
            catch (Exception ex) { QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        private static void LoadImgBoxWeb(string url, PictureBox imgBox)
        {
            if (!editorOnline) { editorRef.SetStatusText("Resource error Check your internet connection."); return; }
            try
            {
                var request = WebRequest.Create(url);

                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    imgBox.Image = Bitmap.FromStream(stream);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The remote name could not be resolved"))
                {
                    QUtils.ShowError("Resource error Please check your internet connection and try Again");
                }
                else throw new Exception(ex.Message);
            }
        }

        internal void GenerateAIScriptId(bool fromBackup = false)
        {
            QUtils.aiScriptId = QTask.GenerateTaskID(true, fromBackup);
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Generated Id " + QUtils.aiScriptId + " fromBackup " + fromBackup);
        }

        private void HandleInvalidTextFmt<T>(object sender, T minCount, T maxCount, string methodName)
        {

            object itemCount = null;
            try
            {
                var count = ((TextBox)sender).Text;

                if (typeof(T) == typeof(int))
                {
                    itemCount = Convert.ToInt32(count);
                    //Check for Max Items count.
                    if ((int)itemCount < (int)((object)minCount) || (int)itemCount > (int)((object)maxCount))
                    {
                        itemCount = minCount;
                        ((TextBox)sender).Text = itemCount.ToString();
                    }

                }
                else if (typeof(T) == typeof(float))
                {
                    itemCount = float.Parse(count);
                    if ((float)itemCount < (float)((object)minCount) || (float)itemCount > (float)((object)maxCount))
                    {
                        itemCount = minCount;
                        ((TextBox)sender).Text = itemCount.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                itemCount = minCount;
                ((TextBox)sender).Text = itemCount.ToString();
                QUtils.LogException(methodName, ex);
            }
        }

        internal void PopulateImageBox(string modelId, PictureBox imgBox)
        {
            var imgTmpPath = QUtils.cachePathAppImages + "\\" + modelId + QUtils.FileExtensions.Png;

            //Load image from Cache.
            if (File.Exists(imgTmpPath))
            {
                using (var bmpTemp = new Bitmap(imgTmpPath))
                {
                    imgBox.Image = new Bitmap(bmpTemp);
                }
            }
        }

        internal void AddRigidObject(string model, bool rigidObj = false, Real64 objectPos = null, bool hasOrientation = false, int taskId = -1, string taskNote = "", bool objCompile = true, float alpha = -9.9f, float beta = -9.9f, float gamma = -9.9f)
        {
            try
            {
                string qscData = null;

                if (hasOrientation)
                {
                    alpha = alpha == fltInvalidVal ? float.Parse(alphaTxt.Text) : alpha;
                    beta = beta == fltInvalidVal ? float.Parse(betaTxt.Text) : beta;
                    gamma = gamma == fltInvalidVal ? float.Parse(gammaTxt.Text) : gamma;

                    if (rigidObj)
                    {
                        var orientation = new Real32(alpha, beta, gamma);
                        qscData = QObjects.AddRigidObj(model, objectPos, orientation, false, taskNote);
                    }
                    else
                    {
                        var orientation = new Real32(alpha, beta, gamma);
                        qscData = QBuildings.AddBuilding(model, objectPos, orientation, false, taskId);
                    }
                }
                else
                {
                    if (rigidObj)
                        qscData = QObjects.AddRigidObj(model, objectPos, false, taskNote);
                    else
                        qscData = QBuildings.AddBuilding(model, objectPos, false, taskId, taskNote);
                }

                //Compile the data with QCompiler.
                if (objCompile)
                {
                    if (!String.IsNullOrEmpty(qscData)) compileStatus = QCompiler.Compile(qscData, QUtils.gamePath, true);
                }
                else
                {
                    QUtils.SaveFile(qscData, true);
                }
            }
            catch (NullReferenceException ex)
            {
                QUtils.ShowError("Values are null while adding object " + ex.StackTrace);
            }
            catch (Exception ex)
            {
                QUtils.ShowError(MethodBase.GetCurrentMethod().Name, ex.Message);
            }
        }
    }
}
