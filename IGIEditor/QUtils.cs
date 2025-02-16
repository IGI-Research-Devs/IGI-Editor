using IWshRuntimeLibrary;
using Microsoft.Win32;
using Newtonsoft.Json;
using QLibc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using static IGIEditor.QServer;
using File = System.IO.File;
using FileIO = Microsoft.VisualBasic.FileIO;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;
using System.Runtime.InteropServices;
using IGIEditor; // Added to reference QLog
using static IGIEditor.QLog;

namespace IGIEditor
{
    internal class QUtils
    {
        internal class QScriptTask
        {
            internal Int32 id;
            internal string name;
            internal string note;
            internal Real64 position;
            internal Real32 orientation;
            internal string model;
        };

        internal class HTask
        {
            internal int team;
            internal QScriptTask qtask;
            internal List<string> weaponsList;
        };

        internal class FOpenIO
        {
            string fileName;
            string fileData;
            long fileLength;
            float fileSize;
            public FOpenIO() { FileName = FileData = null; FileLength = 0; FileSize = 0; }

            public FOpenIO(string fileName, string fileData, long fileLength, float fileSize)
            {
                this.FileName = fileName;
                this.FileData = fileData;
                this.FileLength = fileLength;
                this.FileSize = fileSize;
            }

            public string FileName { get => fileName; set => fileName = value; }
            public string FileData { get => fileData; set => fileData = value; }
            public long FileLength { get => fileLength; set => fileLength = value; }
            public float FileSize { get => fileSize; set => fileSize = value; }
        }

        [SerializableAttribute]
        internal class WeaponGroup
        {
            string name;
            int ammo;

            public string Weapon { get => name; set => name = value; }
            public int Ammo { get => ammo; set => ammo = value; }
        }

        #region Git-Config
        // WARNING - DO NOT EDIT.
        private const string gitUserName = "IGI-Research-Devs";
        private const string gitUserFile = "IGI1Editor_Users.xml";
        #endregion

        #region Task Decl
        internal static string taskNew = "Task_New";
        internal static string taskDecl = "Task_DeclareParameters";
        internal static string objectsQsc = "objects.qsc";
        internal static string objectsQvm = "objects.qvm";
        internal static string weaponConfigQSC = "weaponconfig.qsc";
        internal static string weaponConfigQVM = "weaponconfig.qvm";
        internal static string weaponsModQvm = "weaponconfig-mod.qvm";
        #endregion

        #region Task Ids
        internal static int qtaskObjId;
        internal static int qtaskId;
        internal static int anyaTeamTaskId = -1;
        internal static int ekkTeamTaskId = -1;
        internal static int aiScriptId = 0;
        internal static int gGameLevel = 1;
        internal static int GAME_MAX_LEVEL = 14;
        internal static int currGameLevel = 1;
        internal static int updateTimeInterval = 10;
        internal static int gameFPS = 30;
        internal static int healthScaleFall = 0;
        #endregion

        #region Log & Custom Scripts
        internal static string editorLogFile = "app.log";
        internal static string qLibLogsFile = "QLibc_logs.log";
        internal static string aiIdleFile = "aiIdle.qvm";
        internal static string objectsModelsList;
        internal static string aiIdlePath;
        internal static string customScriptFile = "ai_custom_script.qsc";
        internal static string customPatrolFile = "ai_custom_path.qsc";
        internal static string customScriptPathQEd;
        internal static string customPatrolPathQEd;
        internal static string appLogFileTmp = @"%tmp%\IGIEditorCache\AppLogs\";
        internal static string nativesFile = @"\IGI-Natives.json";
        internal static string modelsFile = @"\IGI-Models.json";
        internal static string internalsLogFile = @"\IGI-Internals.log";
        #endregion

        #region Booleans
        internal static bool gameFound = false;
        internal static bool gameProfileLoaded = false;
        internal static bool gamePathSet = false;
        internal static bool logEnabled = false;
        internal static bool keyExist = false;
        internal static bool keyFileExist = false;
        internal static bool attachStatus = false;
        internal static bool customAiSelected = false;
        internal static bool editorOnline = true;
        internal static bool gameReset = false;
        internal static bool appLogs = false;
        internal static bool editorUpdateCheck = false;
        internal static bool nppInstalled = false;
        internal static bool shortcutCreated = false;
        internal static bool shortcutExist = false;
        internal static bool gameMusicEnabled = false;
        internal static bool gameAiIdleMode = false;
        internal static bool gameDebugMode = false;
        internal static bool gameDisableWarns = true;
        internal static bool gameRefresh = false;
        internal static bool internalCompiler = false;
        internal static bool externalCompiler = false;
        #endregion

        #region App Version
        internal static string versionFileName = "VERSION";
        internal static string appEditorSubVersion = "0.8.5.1";
        internal static float viewPortDelta = 10000.0f;
        #endregion

        #region Support Links
        internal static string supportDiscordLink = @"https://discord.gg/9T8tzyhvp6";
        internal static string supportYoutubeLink = @"https://www.youtube.com/channel/UChGryl0a0dii81NfDZ12LwA";
        internal static string supportVKLink = @"https://vk.com/id679925339";
        #endregion

        #region Game and Data Path.
        internal static string gamePath;
        internal static string appdataPath;
        internal static string igiEditorQEdPath;
        internal static string editorCurrPath;
        internal static string deviceIdDLLPath;
        internal static string gameAbsPath;
        internal static string cfgGamePath;
        internal static string cfgHumanplayerPathQsc;
        internal static string cfgHumanplayerPathQvm;
        internal static string cfgQscPath;
        internal static string cfgAiPath;
        internal static string cfgQvmPath;
        internal static string cfgVoidPath;
        internal static string cfgQFilesPath;
        internal static string qMissionsPath;
        internal static string qGraphsPath;
        internal static string qWeaponsPath;
        internal static string qWeaponsGroupPath;
        internal static string qWeaponsModPath;
        internal static string qQVMPath;
        internal static string qQSCPath;
        internal static string cfgWeaponsPath;
        internal static string weaponsModQvmPath;
        internal static string weaponsOrgCfgPath;
        internal static string weaponsGamePath;
        internal static string humanplayerGamePath;
        internal static string menusystemGamePath;
        internal static string missionsGamePath;
        internal static string commonGamePath;
        internal static string weaponsCfgQscPath;
        internal static string qfilesPath = @"\QFiles";
        internal static string qEditor = "QEditor";
        internal static string qconv = "QConv";
        internal static string qCompiler;
        internal static string qTools;
        internal static string qfiles = "QFiles";
        internal static string qGraphs = "QGraphs";
        internal static string iniCfgFile;
        internal static string editorAppName;
        internal static string editorUpdater;
        internal static string cachePath;
        internal static string cachePathAppLogs;
        internal static string nativesFilePath;
        internal static string modelsFilePath;
        internal static string internalsLogPath;
        internal static string qedAiJsonPath;
        internal static string qedAiScriptPath;
        internal static string qedAiPatrolPath;
        internal static string cachePathAppImages;
        internal static string currPathAppImages;
        internal static string editorUpdaterDir = "IGIEditor_Update";
        internal static string editorUpdaterAbsDir;
        internal static string editorUpdaterFile;
        internal static string updaterBatchFile;
        internal static string editorChangeLogs = "CHANGELOGS";
        internal static string editorLicence = "LICENCE";
        internal static string editorReadme = "README";
        internal static string autoUpdaterFile = "AutoUpdater";
        internal static string autoUpdaterBatch;
        internal static string igiQsc = "IGI_QSC";
        internal static string igiQvm = "IGI_QVM";
        internal static string graphsPath;
        internal static string cfgGamePathEx = @"\missions\location0\level";
        internal static string weaponsDirPath = @"\weapons";
        internal static string humanplayerQvm = "humanplayer.qvm";
        internal static string humanplayerQsc = "humanplayer.qsc";
        internal static string humanplayerPath = @"\humanplayer";
        internal static string aiGraphTask = "AIGraph";
        internal static string menuSystemDir = "menusystem";
        internal static string menuSystemPath;
        internal static string internalsDllFile;
        internal static string internalsDll = "IGI-Internals.dll";
        internal static string internalsDllPath = @"bin\IGI-Internals.dll";
        internal static string qLibcPath = @"lib\GTLibc_x86.so";
        internal static string internalDllInjectorPath = @"bin\IGI-Injector.exe";
        internal static string internalDllGTInjectorPath = @"bin\IGI-Injector-GT.exe";
        internal static string qedQscPath = @"\IGI_QSC";
        internal static string qedQvmPath = @"\IGI_QVM";
        internal static string qedAiPath = @"\AIFiles";
        internal static string qedVoidPath = @"\Void";
        internal static string qedAiJson = @"\AI-Json";
        internal static string qedAiScript = @"\AI-Script";
        internal static string qedAiPatrol = @"\AI-Path";
        internal static string inputMissionPath = @"\missions\location0\level";
        internal static string inputHumanplayerPath = @"\humanplayer";
        internal static string inputWeaponsPath = @"\weapons";
        internal static string PATH_SECTION = "PATH";
        internal static string EDITOR_SECTION = "EDITOR";
        internal static string GAME_SECTION = "GAME";
        internal static string objects = "objects";
        internal static string objectsAll = "objectsAll";
        internal static string weapons = "weapons";
        #endregion

        #region Extensions Constants
        public static class FileExtensions
        {
            public const string Qvm = ".qvm";
            public const string Qsc = ".qsc";
            public const string Dat = ".dat";
            public const string Csv = ".csv";
            public const string Json = ".json";
            public const string Txt = ".txt";
            public const string Xml = ".xml";
            public const string Dll = ".dll";
            public const string Mission = ".igimsf";
            public const string Jpg = ".jpg";
            public const string Png = ".png";
            public const string Rar = ".rar";
            public const string Zip = ".zip";
            public const string Exe = ".exe";
            public const string Bat = ".bat";
            public const string Ini = ".ini";
            public const string Spr = ".spr";
            public const string Text = ".txt";
            public const string Pic = ".pic";
        }

        internal const string sightDisplayType = "SIGHTDISPLAYTYPE_";
        internal const string ammoDisplayType = "AMMODISPLAYTYPE_";
        #endregion

        #region Address_Ptr
        internal static IntPtr viewPortAddrX = (IntPtr)0x00BCAB08;
        internal static IntPtr viewPortAddrY = (IntPtr)0x00BCAB10;
        internal static IntPtr viewPortAddrZ = (IntPtr)0x00BCAB18;
        #endregion

        #region AI_Soldier Constants
        internal const int TEAM_ID_FRIENDLY = 0;
        internal const int TEAM_ID_ENEMY = 1;
        internal const int MAX_AI_COUNT = 100;
		internal const int MIN_FPS = 10;
        internal const int MAX_FPS = 240;
        internal const int MAX_UPDATE_TIME = 120;
        internal const int MAX_HUMAN_CAM = 5;
        internal const int LEVEL_FLOW_TASK_ID = 10;
        internal const int HUMANPLAYER_TASK_ID = 0;
        internal const int MAX_MINIMAL_ID_DIFF = 10;
        #endregion

        #region Error Constants
        internal const string CAPTION_CONFIG_ERR = "Config - Error";
        internal const string CAPTION_FATAL_SYS_ERR = "Sytem-Fatal - Error";
        internal const string CAPTION_APP_ERR = "Application - Error";
        internal const string CAPTION_COMPILER_ERR = "Compiler - Error";
        internal const string EDITOR_LEVEL_ERR = "EDITOR ERROR";
        internal const string alarmControl = "AlarmControl";
        internal const string stationaryGun = "StationaryGun";
        internal const string EXTERNAL_COMPILER_ERR = "External Compiler not found in QEditor directory." + "\n" + "Try switching to External compiler from settings";
        #endregion

        #region About Info
        internal static string keyBase = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
        internal static string aboutStr = "IGI Editor is powerful editor to edit game levels" + "\n" + "Offers upto " + GAME_MAX_LEVEL + " level\nVersion: v"
            + appEditorSubVersion + " BETA.\n\nTools/Language: C#(5.0) VS-Studio/Code\nCreated by HeavenHM\n\nCredits & People\nUI Designing - Dark\nResearch data - Dimon, Yoejin, GM123.\nQScript/DConv Tools - Artiom.\nTester - Orwa\nTexture Editor - Neoxaero/Dark\nIGI-VK Community.";
        #endregion

        #region Mask Constants
        internal static string patroIdleMask = "xxxx";
        internal static string patroAlarmMask = "yyyy";
        internal static string alarmControlMask = "xx";
        internal static string gunnerIdMask = "xxx";
        internal static string viewGammaMask = "yyy";
        #endregion

        #region Angle Constants
        internal static float fltInvalidAngle = -9.9999f;
        internal static float fltInvalidVal = -9.9f;
        #endregion

        #region HumanPlayer data
        internal static string movementSpeedMask = "movSpeed";
        internal static string forwardSpeedMask = "forwardSpeed";
        internal static string upwardSpeedMask = "upSpeed";
        internal static string inAirSpeedMask = "iAirSpeed";
        internal static string throwBaseVelMask = "throwBaseVel";
        internal static string healthScaleMask = "healthScale";
        internal static string healthFenceMask = "healthFence";
        internal static string peekLeftRightLenMask = "peekLRLen";
        internal static string peekCrouchLenMask = "peekCrouchLen";
        internal static string peekTimeMask = "peekTime";
        internal static double movSpeed = 1.75f;
        internal static double forwardSpeed = 17.5f;
        internal static double upwardSpeed = 27;
        internal static double inAirSpeed = 0.5f;
        internal static double peekCrouchLen = 0.8500000238418579f;
        internal static double peekLRLen = 0.8500000238418579f;
        internal static double peekTime = 0.25;
        internal static double healthScale = 3.0f;
        internal static double healthScaleFence = 0.5f;
        #endregion

        #region Misc Data
        internal static List<string> aiScriptFiles = new List<string>();
        internal static string aiEnenmyTask = null;
        internal static string aiFriendTask = null;
        internal static string levelFlowData;
        internal static string missionLevelFile = "mission_level.txt";
        internal static string missionDescFile = "mission_desc.txt";
        internal static string missionListFile = @"\MissionsList.dat";
        #endregion

        internal static List<string> objTypeList = new List<string>() { "Building", "EditRigidObj", "Terminal", "Elevator", "ExplodeObject", "AlarmControl", "Generator", "Radio" };
        private static Random rand = new Random();
        internal static QIniParser qIniParser;
        internal enum QTYPES { BUILDING = 1, RIGID_OBJ = 2 };
        internal enum GRAPH_VISUAL { OBJECTS = 1, HILIGHT = 2 };
        internal enum UPDATE_ACTION { DOWNLOAD = 1, EXTRACT = 2, UPDATE = 3 };
        internal enum HEALTH_ACTION { NONE = 0, TEMPORARY = 1, PERMANENT = 2, RESTORE = 3 };
        internal static Dictionary<int, string> graphAreas = new Dictionary<int, string>();

        #region Dictionary items
        //List of Dictionary items.
        internal static List<Dictionary<string, int>> weaponList = new List<Dictionary<string, int>>();
        internal static List<Dictionary<string, string>> buildingList = new List<Dictionary<string, string>>();
        internal static List<Dictionary<string, string>> objectRigidList = new List<Dictionary<string, string>>();
        internal static List<Dictionary<string, int>> weaponsGroupListx = new List<Dictionary<string, int>>();
        internal static List<WeaponGroup> weaponsGroupList = new List<WeaponGroup>();
        internal static List<string> buildingListStr = new List<string>();
        internal static List<string> objectRigidListStr = new List<string>();
        internal static List<string> aiModelsListStr = new List<string>();
        internal static List<string> missionNameListStr = new List<string>();
        internal static List<int> aiGraphIdStr = new List<int>();
        internal static List<int> graphdIdsMarked = new List<int>();
        internal static List<int> aiGraphNodeIdStr = new List<int>();
        internal static List<int> weaponMarkedIds = new List<int>();
        internal static List<QGraphs.GraphNode> graphNodesList = new List<QGraphs.GraphNode>();
        internal static List<int> qIdsList = new List<int>();
        internal static List<HumanAi> humanAiList = new List<HumanAi>();
        internal static List<Weapon> weaponDataList = new List<Weapon>();
        internal static List<string> weaponSFXList = new List<string>();
        internal static List<string> weaponDDList = new List<string>();
        #endregion

        //Server data list.
        internal static List<QServerData> qServerDataList = new List<QServerData>();
        internal static List<QMissionsData> qServerMissionDataList = new List<QMissionsData>();

        #region Game weapon ammo
        //Weapons variables.
        internal const string weaponId = "WEAPON_ID_";
        internal static Dictionary<string, string> ammoList = new Dictionary<string, string>(){
        {"SPAS12/JACKHAMMER","AMMO_ID_12"},
        {"COLT","AMMO_ID_357"},//AMMO_ID_44 Original slot fix.
        {"M2HB","AMMO_ID_127"},
        {"DESERTEAGLE","AMMO_ID_357"},
        {"M16A2/MINIMI/T80","AMMO_ID_556"},
        {"AK47","AMMO_ID_762"},
        {"GLOCK/UZI/UZIX2/MP5SD","AMMO_ID_919"},
        {"RPG18","AMMO_ID_1000"},
        {"DRAGUNOV","AMMO_ID_DRAGUNOV"},
        {"FLASHBANG","AMMO_ID_FLASHBANG"},
        {"GRENADE","AMMO_ID_GRENADE"},
        {"M203","AMMO_ID_M203"},
        {"MEDIPACK","AMMO_ID_MEDIPACK"},
        {"PROXIMITYMINE","AMMO_ID_PROXIMITYMINE"}};
        #endregion

        #region Game weapon data
        internal enum GAME_WEAPON
        {
            WEAPON_ID_GLOCK = 1, //Weapon Type: Pistol.
            WEAPON_ID_COLT = 21,//Weapon Type: Revolver.
            WEAPON_ID_DESERTEAGLE = 3,//Weapon Type: Pistol.
            WEAPON_ID_MP5SD = 7,//Weapon Type: SMG.
            WEAPON_ID_UZI = 6,//Weapon Type: SMG.
            WEAPON_ID_UZIX2 = 13,//Weapon Type: SMG.
            WEAPON_ID_M16A2 = 4,//Weapon Type: Rifle.
            WEAPON_ID_AK47 = 5,//Weapon Type: Rifle.
            WEAPON_ID_MINIMI = 10,//Weapon Type: HMG (Machine Gun).
            WEAPON_ID_SPAS12 = 8,//Weapon Type: Shotgun.
            WEAPON_ID_JACKHAMMER = 9,//Weapon Type: Shotgun.
            WEAPON_ID_DRAGUNOV = 11,//Weapon Type: Sniper.
            WEAPON_ID_FLASHBANG = 15,//Weapon Type: Grenade.
            WEAPON_ID_GRENADE = 14,//Weapon Type: Grenade.
            WEAPON_ID_T80 = 43,//Weapon Type: Launcher.
            WEAPON_ID_SENTRY = 44,//Weapon Type: HMG (Machine Gun).
            WEAPON_ID_RPG18 = 12,//Weapon Type: Launcher.
            WEAPON_ID_PROXIMITYMINE = 41,//Weapon Type: Grenade.
            WEAPON_ID_MIL = 41,//Weapon Type: HMG (Machine Gun).
            WEAPON_ID_MEDIPACK = 19,//Weapon Type: MediPack.
            WEAPON_ID_KNIFE = 20,//Weapon Type: Knife.
            WEAPON_ID_M2HB = 42,//Weapon Type: HMG (Machine Gun).
            WEAPON_ID_BINOCULARS = 18,//Weapon Type: Binoculars.
            WEAPON_ID_APC = 40,//Weapon Type: Launcher.
        };
        #endregion

        //Base server url for Downloading resources.
        internal static string baseServerUrl = @"http://igiresearchdevelopers.orgfree.com";

        internal static List<string> aiTypes = new List<string>() {
            "AITYPE_RPG", "AITYPE_GUNNER", "AITYPE_SNIPER",
            "AITYPE_ANYA", "AITYPE_EKK", "AITYPE_PRIBOI",
            "AITYPE_CIVILIAN", "AITYPE_PATROL_AK", "AITYPE_GUARD_AK",
            "AITYPE_SECURITY_PATROL_UZI", "AITYPE_MAFIA_PATROL_UZI", "AITYPE_MAFIA_GUARD_AK",
            "AITYPE_SPETNAZ_PATROL_AK", "AITYPE_SPETNAZ_GUARD_AK" };


        internal static bool InitEditorAppData()
        {
            bool initStatus = true;
            string initErrReason = String.Empty;
            appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            igiEditorQEdPath = appdataPath + Path.DirectorySeparatorChar + qEditor;

            editorAppName = AppDomain.CurrentDomain.FriendlyName.Replace(".exe", String.Empty);
            iniCfgFile = editorAppName + ".ini";
            editorLogFile = editorAppName + ".log";
            editorCurrPath = Directory.GetCurrentDirectory();
            deviceIdDLLPath = editorCurrPath + Path.DirectorySeparatorChar + "DeviceId.dll";

            //Set new Input QSC & QVM path releative to appdata.
            objectsModelsList = igiEditorQEdPath + Path.DirectorySeparatorChar + "IGIModels" + QUtils.FileExtensions.Json;
            qMissionsPath = igiEditorQEdPath + @"\QMissions";
            qGraphsPath = igiEditorQEdPath + @"\QGraphs";
            qWeaponsPath = igiEditorQEdPath + @"\QWeapons";
            qWeaponsGroupPath = qWeaponsPath + "\\" + "Group";
            qWeaponsModPath = qWeaponsPath + "\\" + "Mod";
            weaponsGamePath = gameAbsPath + @"\weapons";
            humanplayerGamePath = gameAbsPath + @"\humanplayer";
            menusystemGamePath = gameAbsPath + @"\menusystem";
            missionsGamePath = gameAbsPath + @"\missions";
            commonGamePath = gameAbsPath + @"\common";
            qCompiler = igiEditorQEdPath + @"\QCompiler";
            qTools = qCompiler + @"\Tools";
            qQVMPath = igiEditorQEdPath + qfilesPath + qedQvmPath;
            qQSCPath = igiEditorQEdPath + qfilesPath + qedQscPath;
            aiIdlePath = igiEditorQEdPath + Path.DirectorySeparatorChar + "aiIdle.qvm";
            cfgQvmPath = igiEditorQEdPath + qfilesPath + qedQvmPath + inputMissionPath;
            cfgQscPath = igiEditorQEdPath + qfilesPath + qedQscPath + inputMissionPath;
            cfgHumanplayerPathQsc = igiEditorQEdPath + qfilesPath + qedQscPath + inputHumanplayerPath;
            cfgHumanplayerPathQvm = igiEditorQEdPath + qfilesPath + qedQvmPath + inputHumanplayerPath;
            cfgWeaponsPath = igiEditorQEdPath + qfilesPath + qedQvmPath + inputWeaponsPath;
            cfgAiPath = igiEditorQEdPath + qedAiPath;
            qedAiJsonPath = igiEditorQEdPath + qedAiPath + qedAiJson;
            qedAiScriptPath = igiEditorQEdPath + qedAiPath + qedAiScript;
            qedAiPatrolPath = igiEditorQEdPath + qedAiPath + qedAiPatrol;
            customScriptPathQEd = qedAiScriptPath + "\\" + customScriptFile;
            customPatrolPathQEd = qedAiPatrolPath + "\\" + customPatrolFile;
            cfgVoidPath = igiEditorQEdPath + qedVoidPath;
            cfgQFilesPath = igiEditorQEdPath + qfilesPath;
            menuSystemPath = gameAbsPath + menuSystemDir;
            cachePath = Path.GetTempPath() + "IGIEditorCache";
            cachePathAppLogs = cachePath + @"\AppLogs";
            cachePathAppImages = cachePath + @"\AppImages";
            currPathAppImages = editorCurrPath + @"\AppImages";
            missionListFile = cachePath + missionListFile;
            nativesFilePath = cachePath + nativesFile;
            modelsFilePath = cachePath + modelsFile;
            internalsLogPath = cachePath + internalsLogFile;
            weaponsModQvmPath = cfgWeaponsPath + @"\" + weaponsModQvm;
            weaponsOrgCfgPath = cfgWeaponsPath + @"\" + weaponConfigQVM;
            weaponsCfgQscPath = qQSCPath + @"\weapons\" + weaponConfigQSC;
            editorUpdaterFile = cachePath + @"\" + editorUpdaterDir + QUtils.FileExtensions.Zip;
            editorUpdaterAbsDir = cachePath + @"\" + editorUpdaterDir;
            autoUpdaterBatch = editorAppName + "-" + autoUpdaterFile + FileExtensions.Bat;
            editorUpdater = editorAppName + "_" + "Update" + QUtils.FileExtensions.Zip;

            //Init Temp path for Cache.
            if (!Directory.Exists(cachePath))
            {
                CreateCacheDir();
            }

            //Init QEditor path for Appdata.
            MoveQEditorAppdata();
            MoveAppImagesCache();

            if (!Directory.Exists(igiEditorQEdPath)) { initErrReason = "QEditor"; initStatus = false; }
            else if (!Directory.Exists(qMissionsPath)) { initErrReason = @"QEditor\QMissions"; initStatus = false; }
            else if (!Directory.Exists(qQVMPath)) { initErrReason = @"QEditor\QFiles\IGI_QVM"; initStatus = false; }
            else if (!Directory.Exists(qQSCPath)) { initErrReason = @"QEditor\QFiles\IGI_QSC"; initStatus = false; }
            else if (!Directory.Exists(cfgAiPath)) { initErrReason = @"QEditor\AIFiles"; initStatus = false; }
            else if (!Directory.Exists(cfgVoidPath)) { initErrReason = @"QEditor\Void"; initStatus = false; }
            else if (!Directory.Exists(cfgQFilesPath)) { initErrReason = @"QEditor\QFiles"; initStatus = false; }
            else if (!Directory.Exists(cfgQFilesPath)) { initErrReason = @"QEditor\QWeapons"; initStatus = false; }

            initErrReason = "'" + initErrReason + "' Directory is missing";
#if !DEV_MODE
            //Show error if 'QEditor' path has invalid structure..
            if (!initStatus) ShowSystemFatalError("Editor Appdata directory is invalid Error: (0x0000000F)\nReason: " + initErrReason + "\nPlease re-install new copy from Setup file.");
#endif

            if (String.IsNullOrEmpty(gameAbsPath)) return false;

            return initStatus;
        }

        internal static void MoveQEditorAppdata()
        {
            string qEditorPath = editorCurrPath + @"\" + qEditor;
            if (Directory.Exists(qEditorPath))
            {
                DirectoryIOMove(qEditorPath, igiEditorQEdPath);
            }
        }

        internal static void MoveAppImagesCache()
        {
            if (Directory.Exists(currPathAppImages))
            {
                DirectoryIOMove(currPathAppImages, cachePathAppImages);
            }

        }

        internal static void CreateCacheDir()
        {
            Directory.CreateDirectory(cachePath);
            Directory.CreateDirectory(cachePathAppLogs);
            Directory.CreateDirectory(cachePathAppImages);
        }


        internal static void SwitchEditorUI()
        {
            GT.ShowAppForeground(QUtils.editorAppName);
        }

        internal static DialogResult ShowGamePathDialog()
        {
            var folderBrowser = new OpenFileDialog();
            folderBrowser.ValidateNames = false;
            folderBrowser.CheckFileExists = false;
            folderBrowser.CheckPathExists = true;
            folderBrowser.FileName = "Folder Selection.";
            folderBrowser.Title = "Select Game path";

            var dlgResult = folderBrowser.ShowDialog();
            if (dlgResult == DialogResult.OK)
            {
                var newGameAbsPath = Path.GetDirectoryName(folderBrowser.FileName) + Path.DirectorySeparatorChar;

                if (!String.IsNullOrEmpty(gameAbsPath) && gameAbsPath == newGameAbsPath && CheckShortcutExist())
                {
                    QLog.ShowLogError("GamePathDialog", "Selected path is same as current game path\nChoose a diffrent path or try again.");
                    gamePathSet = shortcutCreated = shortcutExist = false;
                    return dlgResult;
                }

                //Delete Previous Config.
                QUtils.FileIODelete(QUtils.iniCfgFile);

                gameAbsPath = newGameAbsPath;
                cfgGamePath = (!String.IsNullOrEmpty(gameAbsPath)) ? (gameAbsPath.Trim() + QMemory.gameName + ".exe") : null;
                bool status = File.Exists(cfgGamePath);

                if (!status)
                {
                    gamePathSet = shortcutCreated = shortcutExist = false;
                    ShowSystemFatalError("Error occurred while setting game path.");
                }
                else
                {
                    ShowInfo("Game path was saved successfully.");
                    CreateGameShortcut();
                    //Create New Config.
                    QUtils.CreateConfig();

                    //Read New Config.
                    QUtils.ParseConfig();

                    gamePathSet = true;
                }
            }
            return dlgResult;
        }

        internal static FOpenIO ShowOpenFileDlg(string title, string defaultExt, string filter, bool initDir = false, string initialDirectory = "", bool openFileData = true, bool exceptionOnEmpty = true)
        {
            var fopenIO = new FOpenIO();

            try
            {
                var fileBrowser = new OpenFileDialog();
                fileBrowser.ValidateNames = false;
                fileBrowser.CheckFileExists = false;
                fileBrowser.CheckPathExists = true;
                fileBrowser.Title = title;
                fileBrowser.DefaultExt = defaultExt;
                fileBrowser.Filter = filter;
                if (initDir)
                    fileBrowser.InitialDirectory = initialDirectory;

                if (fileBrowser.ShowDialog() == DialogResult.OK)
                {
                    fopenIO.FileName = fileBrowser.FileName;
                    fopenIO.FileLength = new FileInfo(fopenIO.FileName).Length;
                    fopenIO.FileSize = ((float)fopenIO.FileLength / (float)1024);
                    if (openFileData)
                    {
                        fopenIO.FileData = QUtils.LoadFile(fopenIO.FileName);
                        if (String.IsNullOrEmpty(fopenIO.FileData) && exceptionOnEmpty) throw new FileLoadException("File '" + fopenIO.FileName + "' is invalid or data is empty.");
                    }
                }
            }
            catch (Exception ex)
            {
                QLog.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return fopenIO;
        }


        internal static void CreateConfig()
        {
            gameAbsPath = (gameAbsPath is null) ? LocateExecutable(QMemory.gameName + ".exe") : gameAbsPath;
            gameFound = true;

            if (String.IsNullOrEmpty(cfgGamePath))
            {
                QLog.ShowWarning("Game path could not be detected automatically! Please select game path in config file", CAPTION_CONFIG_ERR);
                gameFound = false;
            }
            else
            {
                if (!File.Exists(gameAbsPath + Path.DirectorySeparatorChar + QMemory.gameName + ".exe"))
                {
                    QLog.ShowError("Invalid path selected! Game 'IGI' not found at path '" + gameAbsPath + "'", CAPTION_FATAL_SYS_ERR);
                    gameFound = false;
                }
            }

            //Write App path to config.
            qIniParser.Write("game_path", gameAbsPath is null ? "\n" : gameAbsPath, PATH_SECTION);

            //Write App properties to config [EDITOR-SECTION].
            qIniParser.Write("game_reset", gameReset.ToString().ToLower(), EDITOR_SECTION);
            qIniParser.Write("game_refresh", gameRefresh.ToString().ToLower(), EDITOR_SECTION);
            qIniParser.Write("app_logs", appLogs.ToString().ToLower(), EDITOR_SECTION);
            qIniParser.Write("app_online", editorOnline.ToString().ToLower(), EDITOR_SECTION);
            qIniParser.Write("update_check", editorUpdateCheck.ToString().ToLower(), EDITOR_SECTION);
            qIniParser.Write("update_interval", updateTimeInterval.ToString().ToLower(), EDITOR_SECTION);
            qIniParser.Write("compiler_type", (internalCompiler) ? "internal" : "external", EDITOR_SECTION);

            //Write Game properties to config [GAME-SECTION].
            qIniParser.Write("music_enabled", gameMusicEnabled.ToString().ToLower(), GAME_SECTION);
            qIniParser.Write("ai_idle_mode", gameAiIdleMode.ToString().ToLower(), GAME_SECTION);
            qIniParser.Write("debug_mode", gameDebugMode.ToString().ToLower(), GAME_SECTION);
            qIniParser.Write("disable_warnings", gameDisableWarns.ToString().ToLower(), GAME_SECTION);
            qIniParser.Write("game_fps", gameFPS.ToString().ToLower(), GAME_SECTION);

            //if (!gameFound) Environment.Exit(1);
        }

        internal static void ParseConfig()
        {
            bool appLogsParsed = false, gameRefreshParsed = false, gameResetParsed = false, editorOnlineParsed = false, editorUpdateParsed = false, timeInterval = false;
            bool gameMusicParsed = false, gameAiIdleModeParsed = false, gameDebugModeParsed = false, gameDisableWarnParsed = false, gameFPSParsed = false, compilerParsed = false;
            try
            {
                if (File.Exists(iniCfgFile))
                {
                    //Read properties from PATH section.
                    var configPath = qIniParser.Read("game_path", PATH_SECTION);

                    string gPath = configPath.Trim();
                    if (gPath.Contains("\""))
                        gPath = configPath = gPath.Replace("\"", String.Empty);
                    if (!File.Exists(gPath + Path.DirectorySeparatorChar + QMemory.gameName + ".exe"))
                    {
                        //ShowError("Invalid path selected! Game 'IGI' not found at path '" + gPath + "'", CAPTION_FATAL_SYS_ERR);
                        //if (ShowGamePathDialog() != DialogResult.OK) ; //Prompt for Game path on invalid path.
                        //Environment.Exit(1);
                    }
                    else
                    {
                        gameAbsPath = gPath;
                        cfgGamePath = configPath.Trim() + cfgGamePathEx;
                    }

                    //Parse all data for Applications settings.
                    appLogs = bool.Parse(qIniParser.Read("app_logs", EDITOR_SECTION)); appLogsParsed = true;
                    gameReset = bool.Parse(qIniParser.Read("game_reset", EDITOR_SECTION)); gameResetParsed = true;
                    gameRefresh = bool.Parse(qIniParser.Read("game_refresh", EDITOR_SECTION)); gameRefreshParsed = true;
                    editorOnline = bool.Parse(qIniParser.Read("app_online", EDITOR_SECTION)); editorOnlineParsed = true;
                    editorUpdateCheck = bool.Parse(qIniParser.Read("update_check", EDITOR_SECTION)); editorUpdateParsed = true;
                    updateTimeInterval = int.Parse(qIniParser.Read("update_interval", EDITOR_SECTION)); timeInterval = true;
                    var compilerType = qIniParser.Read("compiler_type", EDITOR_SECTION);
                    if (compilerType.Contains("internal") || compilerType.Contains("external")) { internalCompiler = (compilerType.Contains("internal")); externalCompiler = (compilerType.Contains("external")); compilerParsed = true; } else compilerParsed = false;

                    //Parse all data for Game settings.
                    gameMusicEnabled = bool.Parse(qIniParser.Read("music_enabled", GAME_SECTION)); gameMusicParsed = true;
                    gameAiIdleMode = bool.Parse(qIniParser.Read("ai_idle_mode", GAME_SECTION)); gameAiIdleModeParsed = true;
                    gameDebugMode = bool.Parse(qIniParser.Read("debug_mode", GAME_SECTION)); gameDebugModeParsed = true;
                    gameDisableWarns = bool.Parse(qIniParser.Read("disable_warnings", GAME_SECTION)); gameDisableWarnParsed = true;
                    gameFPS = Int32.Parse(qIniParser.Read("game_fps", GAME_SECTION)); gameFPSParsed = true;

                    //Setting for Auto-Updater.
                    if (!editorOnline && editorUpdateCheck) editorUpdateCheck = false;
                }
                else
                {
                    //ShowWarning("Config file not found in current directory", CAPTION_CONFIG_ERR);
                    CreateConfig();
                }
            }
            catch (FormatException ex)
            {
                //Check for App settings.
                if (!appLogsParsed) ShowConfigError("app_logs");
                if (!gameResetParsed) ShowConfigError("game_reset");
                if (!gameRefreshParsed) ShowConfigError("game_refresh");
                if (!editorOnlineParsed) ShowConfigError("app_online");
                if (!editorUpdateParsed) ShowConfigError("update_check");
                if (!timeInterval) ShowConfigError("update_interval");
                if (!compilerParsed) ShowConfigError("compiler_type");

                //Check for Game settings.
                if (!gameMusicParsed) ShowConfigError("music_enabled");
                if (!gameAiIdleModeParsed) ShowConfigError("ai_idle_mode");
                if (!gameDebugModeParsed) ShowConfigError("debug_mode");
                if (!gameDisableWarnParsed) ShowConfigError("disable_warnings");
                if (!gameFPSParsed) ShowConfigError("game_fps");

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                ShowSystemFatalError("Exception: " + ex.Message);
            }
        }

        internal static void InitLibBin()
        {
            bool status = false;
            if (Directory.Exists("bin") && Directory.Exists("lib"))
            {
                if (File.Exists(internalsDllPath) && File.Exists(qLibcPath) && File.Exists(internalDllInjectorPath))
                {
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "LibBin and QLibc files were found on device.");
                    status = true;
                }
            }
            else
                status = false;

            if (!status) ShowSystemFatalError("Editor internal files were not found in directory (ERROR: 0xC33000F)");
        }

        internal static string GetCurrentUserName()
        {
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            userName = userName.Substring(userName.LastIndexOf('\\') + 1);
            return userName;
        }

        internal static bool CheckAppInstalled(string appName, string helpVerText = "--version")
        {
            if (appName is null)
            {
                throw new ArgumentNullException(nameof(appName));
            }

            bool installed = false;
            string appVersionFile = appName + "_info.txt";
            string appCheckCmd = appName + helpVerText + " > " + appVersionFile;
            ShellExec(appCheckCmd);
            string appVersionData = File.ReadAllText(appVersionFile);

            if (!String.IsNullOrEmpty(appVersionData)) installed = true;

            FileIODelete(appVersionFile);
            return installed;
        }

        internal static string LocateExecutable(String filename)
        {
            RegistryKey localMachine = Registry.LocalMachine;
            RegistryKey fileKey = localMachine.OpenSubKey(string.Format(@"{0}\{1}", keyBase, filename));
            object result = null;
            if (fileKey != null)
            {
                result = fileKey.GetValue(string.Empty);
                fileKey.Close();
            }

            return (string)result;
        }

        internal static bool CheckShortcutExist()
        {
            bool exist = File.Exists(QMemory.gameName + "_window.lnk") || File.Exists(QMemory.gameName + "_full.lnk");
            return exist;
        }

        internal static bool RemoveGameShortcut()
        {
            bool status = false;
            try
            {
                FileIODelete(QMemory.gameName + "_window.lnk");
                FileIODelete(QMemory.gameName + "_full.lnk");
                status = true;
            }
            catch (Exception ex)
            {
                status = false;
                QLog.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return status;
        }

        private static void CreateGameShortcut(string linkName, string pathToApp, string gameArgs = "")
        {
            try
            {
                var shell = new WshShell();
                string shortcutAddress = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + linkName + ".lnk";

                FileIODelete(shortcutAddress);

                var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "Shortcut for IGI";
                shortcut.Hotkey = "Ctrl+ALT+I";
                shortcut.Arguments = gameArgs;
                shortcut.WorkingDirectory = pathToApp;
                shortcut.TargetPath = pathToApp + Path.DirectorySeparatorChar + "igi.exe";
                shortcut.Save();
                shortcutCreated = shortcutExist = true;
            }
            catch (Exception ex)
            {
                shortcutCreated = shortcutExist = false;
                QLog.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        internal static bool CreateGameShortcut()
        {
            shortcutExist = shortcutCreated = false;
            if (!File.Exists(QMemory.gameName + "_full.lnk") || !File.Exists(QMemory.gameName + "_full.lnk"))
            {
                if (gameAbsPath.Contains("\""))
                    gameAbsPath = gameAbsPath.Replace("\"", String.Empty);
                CreateGameShortcut(QMemory.gameName + "_full", gameAbsPath);
                CreateGameShortcut(QMemory.gameName + "_window", gameAbsPath, "window");
            }
            return shortcutCreated;
        }

        //File Operation Utilities C# Version.
        internal static void FileMove(string sourcePath, string destPath)
        {
            try
            {
                if (File.Exists(sourcePath)) File.Move(sourcePath, destPath);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileCopy(string sourcePath, string destPath, bool overwirte = true)
        {
            try
            {
                if (File.Exists(sourcePath)) File.Copy(sourcePath, destPath, overwirte);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        //File Operation Utilities VB Version.
        internal static void FileIOMove(string sourcePath, string destPath, bool overwrite = true)
        {
            try
            {
                if (File.Exists(sourcePath)) FileSystem.MoveFile(sourcePath, destPath, overwrite);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }


        internal static void FileIOMove(string sourcePath, string destPath, FileIO.UIOption showUI, FileIO.UICancelOption onUserCancel)
        {
            try
            {
                if (File.Exists(sourcePath)) FileSystem.MoveFile(sourcePath, destPath, showUI, onUserCancel);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileIOCopy(string sourcePath, string destPath, bool overwrite = true)
        {
            try
            {
                if (File.Exists(sourcePath)) FileSystem.CopyFile(sourcePath, destPath, overwrite);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileIOCopy(string sourcePath, string destPath, FileIO.UIOption showUI, FileIO.UICancelOption onUserCancel)
        {
            try
            {
                if (File.Exists(sourcePath)) FileSystem.CopyFile(sourcePath, destPath);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileIODelete(string path, FileIO.UIOption showUI = FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption recycle = FileIO.RecycleOption.SendToRecycleBin, FileIO.UICancelOption onUserCancel = FileIO.UICancelOption.ThrowException)
        {
            try
            {
                if (File.Exists(path)) FileSystem.DeleteFile(path, showUI, recycle, onUserCancel);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileRename(string oldName, string newName)
        {
            try
            {
                if (File.Exists(oldName)) FileSystem.RenameFile(oldName, newName);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }


        //Directory Operation Utilities C#.
        internal static void DirectoryMove(string sourcePath, string destPath)
        {
            try
            {
                if (Directory.Exists(sourcePath)) Directory.Move(sourcePath, destPath);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void DirectoryMove(string sourcePath, string destPath, int __ignore)
        {
            var mvCmd = "mv " + sourcePath + " " + destPath;
            var moveCmd = "move " + sourcePath + " " + destPath + " /y";

            try
            {
                //#1 solution to move with same root directory.
                Directory.Move(sourcePath, destPath);
            }
            catch (IOException ex)
            {
                if (ex.Message.Contains("already exist"))
                {
                    DirectoryDelete(sourcePath);
                }
                else
                {
                    //#2 solution to move with POSIX 'mv' command.
                    ShellExec(mvCmd, true, true, "powershell.exe");
                    if (Directory.Exists(sourcePath))
                        //#3 solution to move with 'move' command.
                        ShellExec(moveCmd, true);
                }
            }
        }

        internal static void DirectoryDelete(string dirPath)
        {
            try
            {
                if (Directory.Exists(dirPath))
                {
                    DirectoryInfo di = new DirectoryInfo(dirPath);
                    foreach (FileInfo file in di.GetFiles())
                        file.Delete();
                    foreach (DirectoryInfo dir in di.GetDirectories())
                        dir.Delete(true);
                    Directory.Delete(dirPath);
                }
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        //Directory Operation Utilities VB.
        internal static void DirectoryIOMove(string sourcePath, string destPath, bool overwrite = true)
        {
            try
            {
                if (Directory.Exists(sourcePath)) FileSystem.MoveDirectory(sourcePath, destPath, overwrite);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void DirectoryIOMove(string sourcePath, string destPath, FileIO.UIOption showUI, FileIO.UICancelOption onUserCancel)
        {
            try
            {
                if (Directory.Exists(sourcePath)) FileSystem.MoveDirectory(sourcePath, destPath, showUI, onUserCancel);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void DirectoryIOCopy(string sourcePath, string destPath, bool overwirte = true)
        {
            try
            {
                if (Directory.Exists(sourcePath)) FileSystem.CopyDirectory(sourcePath, destPath, overwirte);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }


        internal static void DirectoryIOCopy(string sourcePath, string destPath, FileIO.UIOption showUI, FileIO.UICancelOption onUserCancel)
        {
            try
            {
                if (Directory.Exists(sourcePath)) FileSystem.CopyDirectory(sourcePath, destPath, showUI, onUserCancel);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void DirectoryIODelete(string path, FileIO.DeleteDirectoryOption deleteContents = FileIO.DeleteDirectoryOption.DeleteAllContents)
        {
            try
            {
                if (Directory.Exists(path)) FileSystem.DeleteDirectory(path, deleteContents);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void DirectoryRename(string oldName, string newName)
        {
            try
            {
                if (File.Exists(oldName)) FileSystem.RenameDirectory(oldName, newName);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }


        internal static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        internal static string Reverse(string str)
        {
            char[] charArray = str.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        internal static string WebReader(string url)
        {
            string strContent = null;
            try
            {
                //Config for WebReader Class for .NET 4.0.
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                var webRequest = WebRequest.Create(url);
                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                {
                    strContent = reader.ReadToEnd();
                    return strContent;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("remote"))
                    QLog.ShowError("Please check your internet connection.");
                else
                    QLog.ShowError(ex.Message, "WebReader Error");
            }
            return strContent;
        }

        internal static void WebDownload(string url, string fileName, string destPath)
        {
            if (!editorOnline) return;
            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(url, fileName);
                if (!Directory.Exists(destPath)) CreateCacheDir();
                ShellExec("move /Y " + fileName + " " + destPath);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The remote name could not be resolved"))
                {
                    QLog.ShowError("Resource error Please check your internet connection and try Again");
                }
                else
                    QLog.ShowError(ex.Message ?? ex.StackTrace);
            }
        }

        internal static void EnableEditorMode(bool enable)
        {
            IntPtr binoAddr = (IntPtr)0x00470B8F;
            IntPtr noClipAddr = (IntPtr)0x004C8806;

            if (enable)
            {
                bool idleStatus = false;
                var qscData = QHuman.UpdateTeamId(TEAM_ID_ENEMY);
                if (!String.IsNullOrEmpty(qscData)) idleStatus = QCompiler.Compile(qscData, gamePath, false, true, false);

                if (idleStatus)
                {
                    //Enable NoClip and remove Bino.
                    GT.GT_WriteNOP(noClipAddr, 3);
                    GT.GT_WriteNOP(binoAddr, 10);
                }
            }

            else
            {
                //Bino & NoClip Opcodes in hex bytes.
                byte[] binoOps = new byte[] { 0xC7, 0x85, 0xB0, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };
                byte[] noClipOps = new byte[] { 0xDC, 0x46, 0x20 };
                uint binoOpsLen = (uint)binoOps.Length;
                uint noClipOpsLen = (uint)noClipOps.Length;

                //Inject Shell for Editor mode.
                GT.GT_InjectShell(binoAddr, binoOps, binoOpsLen, 0, GT.GT_SHELL.GT_ORIGINAL_SHELL, GT.GT_OPCODE.GT_OP_SHORT_JUMP);
                GT.GT_InjectShell(noClipAddr, noClipOps, noClipOpsLen, 0, GT.GT_SHELL.GT_ORIGINAL_SHELL, GT.GT_OPCODE.GT_OP_SHORT_JUMP);

                bool idleStatus = false;
                var qData = QHuman.UpdateTeamId(TEAM_ID_FRIENDLY);
                if (!String.IsNullOrEmpty(qData)) idleStatus = QCompiler.Compile(qData, gamePath, false, true, false);
            }
        }


        internal static bool SetAIEventIdle(bool aiEvent)
        {
            //New Way - Fast Team version.
            bool idleStatus = true;
            var qData = QHuman.UpdateTeamId(aiEvent ? TEAM_ID_ENEMY : TEAM_ID_FRIENDLY);
            if (!String.IsNullOrEmpty(qData)) idleStatus = QCompiler.Compile(qData, gamePath, false, true, false);
            return idleStatus;
        }

        internal static void ShowPathExplorer(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        //Execute shell command and get std-output.
        internal static string ShellExec(string cmdArgs, bool runAsAdmin = false, bool waitForExit = true, string shell = "cmd.exe")
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.FileName = shell;
            startInfo.Arguments = "/c " + cmdArgs;
            startInfo.RedirectStandardOutput = !runAsAdmin;
            startInfo.RedirectStandardError = !runAsAdmin;
            startInfo.UseShellExecute = runAsAdmin;
            process.StartInfo = startInfo;
            if (runAsAdmin) process.StartInfo.Verb = "runas";
            process.Start();
            if (!waitForExit) return null;
            string output = (runAsAdmin) ? String.Empty : process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        internal static void ShellExecUrl(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        internal static string LoadFile()
        {
            return LoadFile(objectsQsc);
        }

        internal static string LoadFile(string fileName)
        {
            string data = null;
            if (File.Exists(fileName))
                data = File.ReadAllText(fileName);
            return data;
        }

        internal static void ResetCurrentLevel(bool restartLevel = false, bool savePosition = false)
        {
            try
            {
                if (gGameLevel <= 0)
                {
                    QLog.AddLog("QUtils.ResetCurrentLevel", $"Invalid gGameLevel [{gGameLevel}] detected, defaulting to 1", DEBUG_TYPE.Error);
                    gGameLevel = 1;
                }
                int level = QMemory.GetRunningLevel();
                if (level <= 0 || level > GAME_MAX_LEVEL) level = 1;
                QUtils.RestoreLevel(level);
                QUtils.ResetScriptFile(level);
                if (restartLevel)
                    QMemory.RestartLevel(savePosition);
                CleanUpAiFiles();
                IGIEditorUI.editorRef.GenerateAIScriptId(true);
            }
            catch (Exception ex)
            {
                LogException("QUtils.ResetCurrentLevel", ex);
            }
        }

        internal static void SaveFile(string data = null, bool appendData = false)
        {
            SaveFile(objectsQsc, data, appendData);
        }

        internal static void SaveFile(string fileName, string data, bool appendData = false)
        {
            if (appendData)
                File.AppendAllText(fileName, data);
            else
                File.WriteAllText(fileName, data);
        }

        internal static void ResetScriptFile(int gameLevel)
        {
            var inputQscPath = cfgQscPath + gameLevel + "\\" + objectsQsc;

            FileCopy(inputQscPath, objectsQsc);
            FileIOCopy(weaponsCfgQscPath, QUtils.weaponConfigQSC);

            var fileData = LoadFile(objectsQsc);
            File.WriteAllText(objectsQsc, fileData);
            levelFlowData = File.ReadLines(objectsQsc).Last();
        }

        internal static void RestoreLevel(int gameLevel)
        {
            if (gameLevel <= 0 || gameLevel > GAME_MAX_LEVEL) gameLevel = 1;
            var gPath = gamePath;

            if (gamePath.Contains(" ")) gPath = gamePath.Replace("\"", String.Empty);

            gPath = cfgGamePath + gameLevel;
            string outputQvmPath = gPath + "\\" + objectsQvm;
            string inputQvmPath = cfgQvmPath + gameLevel + "\\" + objectsQvm;

            //FileIODelete(outputQvmPath);
            FileCopy(inputQvmPath, outputQvmPath);

            var inFileData = File.ReadAllText(inputQvmPath);
            var outFileData = File.ReadAllText(outputQvmPath);

            if (inFileData != outFileData)
                ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Error in restroing level : " + gameLevel);
        }

        internal static int FindIndex(string temp, string sourceData, int sourceIndex, int qtaskIndex)
        {
            for (int i = 0; sourceIndex < temp.Length; i++)
            {
                sourceIndex = temp.IndexOf(sourceData) + i;
                if ((temp[sourceIndex - 1] == ' ' || temp[sourceIndex - 1] == ',')
                     && temp[sourceIndex + 1] == ',')
                {
                    break;
                }
            }

            //Add the index + offset.
            sourceIndex += qtaskIndex;
            return sourceIndex;
        }

        internal static int GetModelCount(string model)
        {
            if (!QObjects.CheckModelExist(model)) return 0;
            var qtaskList = QTask.GetQTaskList(false, true);
            int count = qtaskList.Count(o => String.Compare(o.model, model, true) == 0);
            ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Model count : " + count);
            return count;
        }

        internal static void EditorUpdater(string updateFile = null, UPDATE_ACTION updateAction = UPDATE_ACTION.UPDATE, string updateBatch = "updater.bat")
        {
            try
            {
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Called with File '" + updateFile + "'" + " action: " + updateAction.ToString() + " Batch File: " + updateBatch);
                ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Updating application please wait...");
                string updateUrl = QServer.serverBaseURL + QServer.updateDir;
                string localFileName = cachePath + "\\" + (String.IsNullOrEmpty(updateFile) ? editorUpdaterDir : updateFile) + QUtils.FileExtensions.Zip;
                string updateName = editorUpdaterDir;
                string localUpdateFile = cachePath + "\\" + updateName + QUtils.FileExtensions.Zip;
                string localUpdatePath = cachePath + "\\" + editorUpdaterDir;
                string localChangelogsPath = cachePath + "\\" + QUtils.editorChangeLogs + QUtils.FileExtensions.Text;
                string localReadmePath = cachePath + "\\" + QUtils.editorReadme + QUtils.FileExtensions.Text;
                string editorReadmePath = editorCurrPath + "\\" + QUtils.editorReadme + QUtils.FileExtensions.Text;
                string editorChangelogsPath = editorCurrPath + "\\" + QUtils.editorReadme + QUtils.FileExtensions.Text;
                bool status = false;

                //Extract the updater if found.
                if (File.Exists(localUpdateFile) && !Directory.Exists(localUpdatePath)
                    && (updateAction == UPDATE_ACTION.EXTRACT || updateAction == UPDATE_ACTION.UPDATE))
                {
                    ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Updater file found please wait...");
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Extracting: '" + localUpdateFile + "'");
                    status = QZip.Extract(localUpdateFile, cachePath);
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Extracting of file '" + updateFile + "' done");
                }

                //Download the updater if not found.
                else if (updateAction == UPDATE_ACTION.DOWNLOAD || updateAction == UPDATE_ACTION.UPDATE)
                {
                    bool newInstallation = true;

                    //Cleanup previous updater files on Update.
                    if (updateAction == UPDATE_ACTION.UPDATE)
                    {
                        ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Updater cleaning previous files...");
                        if (Directory.Exists(localUpdatePath)) DirectoryDelete(localUpdatePath);
                        QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Updater cleaning previous files for Action: " + updateAction.ToString());
                    }

                    //Update from backup on Update.
                    if (updateAction == UPDATE_ACTION.UPDATE)
                    {
                        QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Updating from backup file.");
                        if (File.Exists(localUpdateFile))
                        {
                            var dlgMsg = ShowDialog("Editor found updater file already exist from previous version\nDo you want to delete it and continue with new installation ?");
                            if (dlgMsg == DialogResult.OK)
                            {
                                FileIODelete(localUpdateFile);
                                newInstallation = true;
                            }
                            else
                            {
                                ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Updater file found please wait...");
                                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Extracting: backup file '" + localUpdateFile + "'");
                                status = QZip.Extract(localUpdateFile, cachePath);
                                newInstallation = false;
                                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Extracting from backup file done.");
                            }
                        }
                    }

                    //Download new updater file.
                    if (newInstallation)
                    {
                        updateUrl = updateUrl + "/" + updateName;
                        string updateNamePath = localFileName;
                        string updateUrlPath = "/" + QServer.updateDir + "/" + updateName + QUtils.FileExtensions.Zip;

                        QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Downloading new updater file '" + updateNamePath + "'");
                        ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Downloading new update please wait...");
                        QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Url: '" + updateUrl + "' file '" + updateNamePath + "'");
                        status = QServer.Download(updateUrlPath, updateNamePath, cachePath);

                        QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Downloaded new updater file '" + updateNamePath + "'");
                        //Download and extract to Cache.
                        if (updateAction == UPDATE_ACTION.EXTRACT || updateAction == UPDATE_ACTION.UPDATE)
                        {
                            if (status) status = QZip.Extract(updateNamePath, cachePath);
                            if (status)
                                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Extracting updater file '" + updateNamePath + "' done.");
                        }
                    }
                }

                //updateFile = cachePath + "\\" + updateFile;
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Updater: UpdaterFile '" + updateFile + "' exist: " + Directory.Exists(updateFile).ToString());
                //Start updating the editor files.
                if (Directory.Exists(localUpdatePath) && updateAction == UPDATE_ACTION.UPDATE)
                {
                    updaterBatchFile = localUpdatePath + "\\" + updateBatch;//Updater batch file.
                    string batchUpdaterData =
                        "@echo off\n" +
                        "timeout 5\n" + //Timeout for updating in external thread.
                        "move /y " + "\"" + localUpdatePath + "\\" + editorAppName + QUtils.FileExtensions.Exe + "\" \"" + editorCurrPath + "\\" + editorAppName + QUtils.FileExtensions.Exe + "\"" + "\n" +
                        "move /y " + "\"" + localUpdatePath + "\\" + editorAppName + "_x86" + QUtils.FileExtensions.Exe + "\" \"" + editorCurrPath + "\\" + editorAppName + "_x86" + QUtils.FileExtensions.Exe + "\"" + "\n" +
                        "timeout 5\n" + //Timeout for Restarting editor app.
                       "\"" + editorCurrPath + "\\" + editorAppName + QUtils.FileExtensions.Exe + "\"\n" +
                      (QUtils.nppInstalled ? "notepad++ - nosession - notabbar - alwaysOnTop - multiInst - lhaskell \"" : "notepad \"") + editorCurrPath + "\\" + editorChangeLogs + QUtils.FileExtensions.Text + "\"\n";

                    File.WriteAllText(updaterBatchFile, batchUpdaterData);
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Updating batch file '" + updateBatch + "' created");

                    ShowWarning("Updating the editor please wait.\nEditor will now restart to update to the newest version available.");

                    //Detach internals before updating them.
                    DetachInternals();
                    Sleep(2.5f);

                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Updating Internals detached");

                    //If update contains bin - Injector/Natives data as well.
                    if (Directory.Exists(localUpdatePath + "\\" + "bin"))
                    {
                        DirectoryIOCopy(localUpdatePath + "\\" + "bin", editorCurrPath + "\\" + "bin");
                        QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Updating 'bin' directory done.");
                    }

                    //Move Readme,Changelogs.
                    if (File.Exists(localReadmePath)) QUtils.FileIOMove(localReadmePath, editorReadmePath);
                    if (File.Exists(localChangelogsPath)) QUtils.FileIOMove(localChangelogsPath, editorChangelogsPath);

                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Updating Executing batch shell command");
                    ShellExec(updaterBatchFile, true, false);
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Updating Executing batch shell command done.");
                    ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Editor was updated successfully.");
                    Application.Exit();//Exit the application to update.
                }

                //Show Status of updater.
                if (status) ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Editor was updated successfully.");
                else ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Updater: Error occurred while updating editor (Error: 0xC00005F).");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("could not complete operation on some files and directories"))
                {
                    ShowLogError(MethodBase.GetCurrentMethod().Name, "Updater failed to move some files, Detach Internals please wait.");
                }
                else
                    LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        internal static int CheckEditorVersion(string versionOld, string versionNew)
        {
            var updateVersion = new Version(versionOld);
            var editorVersion = new Version(versionNew);

            int result = updateVersion.CompareTo(editorVersion);
            return result;
        }

        internal static void ExportCSV(string csvFile, List<QScriptTask> qtaskList, bool allObjects = true)
        {
            if (qtaskList.Count <= 0)
            {
                ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Qtask list is empty");
                return;
            }

            //Write CSV Header.
            string csvHeader = "Task_ID," + "Task_Name," + "Task_Note," + "X_Pos," + "Y_Pos," + "Z_Pos," + "Alpha," + "Beta," + "Gamma," + "Model" + "\n";
            File.AppendAllText(csvFile, csvHeader);

            //Write CSV data.
            foreach (var qtask in qtaskList)
            {
                string csvData = null;
                if (allObjects == false)
                {
                    if (objTypeList.Any(o => o.Contains(qtask.name)))
                        csvData = "" + qtask.id + "," + qtask.name + "," + qtask.note + "," + qtask.position.x + "," + qtask.position.y + "," + qtask.position.z + "," + qtask.orientation.alpha + "," + qtask.orientation.beta + "," + qtask.orientation.gamma + "," + qtask.model + "\n";
                }

                else
                {
                    csvData = "" + qtask.id + "," + qtask.name + "," + qtask.note + "," + qtask.position.x + "," + qtask.position.y + "," + qtask.position.z + "," + qtask.orientation.alpha + "," + qtask.orientation.beta + "," + qtask.orientation.gamma + "," + qtask.model + "\n";
                }
                File.AppendAllText(csvFile, csvData);
            }
        }

        internal static void ExportCSV(string csvFile, List<Weapon> weaponTaskList, bool advancedData = false)
        {
            if (weaponTaskList.Count <= 0)
            {
                ShowLogStatus(MethodBase.GetCurrentMethod().Name, "Weapon list is empty");
                return;
            }

            //Write CSV Header.
            string csvHeader = "Name," + "Id," + "WeaponType," + "AmmoType," + "Mass," + "Damage," + "Power," + "ReloadTime," + "Bullets/Round," + "RPM," + "Magazine," + "Range," + "Burts," + "Muzzle velocity" + "\n";
            File.AppendAllText(csvFile, csvHeader);

            //Write CSV data.
            foreach (var weaponTask in weaponTaskList)
            {
                string csvData = null;
                if (!advancedData)
                {
                    csvData = "" + weaponTask.weaponName + "," + weaponTask.scriptId + "," + weaponTask.typeEnum + "," + weaponTask.ammoDispType + "," + weaponTask.mass + "," + weaponTask.damage + "," + weaponTask.power + "," + weaponTask.reloadTime + "," + weaponTask.bullets + "," + weaponTask.roundsPerMinute + "," + weaponTask.roundsPerClip + "," + weaponTask.weaponRange + "," + weaponTask.burst + "," + weaponTask.muzzleVelocity + "," + "\n";
                }

                else
                {
                }
                File.AppendAllText(csvFile, csvData);
            }
        }

        internal static void ExportXML(string fileName)
        {
            var qtaskList = QTask.GetQTaskList();
            string csvFile = null;

            if (File.Exists(csvFile))
                csvFile = LoadFile(csvFile);
            else
            {
                csvFile = objects + QUtils.FileExtensions.Csv;
                ExportCSV(csvFile, qtaskList);
            }

            var lines = File.ReadAllLines(csvFile);
            string[] headers = lines[0].Split(',').Select(x => x.Trim('\"')).ToArray();

            var xml = new XElement("root",
               lines.Where((line, index) => index > 0).Select(line => new XElement("Item",
                  line.Split(',').Select((column, index) => new XElement(headers[index], column)))));

            xml.Save(fileName);
            FileIODelete(csvFile);
        }

        internal static void ExportJson(string fileName)
        {
            string xmlFile = objects + QUtils.FileExtensions.Xml;
            string xmlData = null;

            if (File.Exists(xmlFile))
                xmlData = LoadFile(xmlFile);
            else
            {
                xmlFile = objects + QUtils.FileExtensions.Xml;
                ExportXML(xmlFile);
                xmlData = LoadFile(xmlFile);
            }

            string jsonData = null;
            Sleep(1);

            if (File.Exists(xmlFile))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlData);

                jsonData = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
                SaveFile(fileName, jsonData);
            }
            else throw new FileNotFoundException("File 'objects.xml' was not found in current directory");

            FileIODelete(xmlFile);
        }

        internal static bool IsNetworkAvailable()
        {
            return IsNetworkAvailable(0);
        }


        internal static bool IsNetworkAvailable(long minimumSpeed)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // discard because of standard reasons
                if ((ni.OperationalStatus != OperationalStatus.Up) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                    continue;

                // this allow to filter modems, serial, etc.
                // I use 10000000 as a minimum speed for most cases
                if (ni.Speed < minimumSpeed)
                    continue;

                // discard virtual cards (virtual box, virtual pc, etc.)
                if ((ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                // discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
                if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
                    continue;

                return true;
            }
            return false;
        }


        internal static bool AttachInternals()
        {
#if DEV_MODE
            return true;
#endif
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Path : " + internalsDllPath);

#if DEBUG
            internalsDllFile = Path.GetFileNameWithoutExtension(internalsDll) + "-Dbg" + QUtils.FileExtensions.Dll;
            internalsDllPath = @"bin\" + internalsDllFile;
#else
            internalsDllFile = Path.GetFileNameWithoutExtension(internalsDll) + "-Rls" + QUtils.FileExtensions.Dll;
            internalsDllPath = @"bin\" + internalsDllFile;
#endif

            //Init lib and bin folders first.
            InitLibBin();

            //Injector - 1st Method.
            string internalsCmd = internalDllInjectorPath + " -i " + internalsDllFile;
            string shellOut = ShellExec(internalsCmd, true);
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Using first method.");

            Sleep(1.5f);
            var internalsAttached = CheckInternalsAttached();

            //Injector - 2nd Method.
            if (!internalsAttached)
            {
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Using second method.");
                internalsCmd = internalDllGTInjectorPath + " " + internalsDllFile;

                Sleep(1.5f);
                internalsAttached = CheckInternalsAttached();
                if (!internalsAttached)
                {
                    ShowLogError(MethodBase.GetCurrentMethod().Name, "all methods failed to work. Use Manual injection.");
                    attachStatus = false;
                    return attachStatus;
                }
            }

            if (shellOut.Contains("Error")) attachStatus = false; else attachStatus = true;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "cmd: " + internalsCmd + " status: " + attachStatus);
            return attachStatus;
        }

        internal static bool DetachInternals()
        {
            try
            {
                var internalsAttached = CheckInternalsAttached();
                if (!internalsAttached) return true;

                string dllShellCmd = internalDllInjectorPath + " -e " + internalsDllFile;

                GT.GT_SendKeys2Process(QMemory.gameName, "{END 10}");
                Sleep(5);
                internalsAttached = CheckInternalsAttached();
                if (!internalsAttached) return true;

                string shellOut = ShellExec(dllShellCmd, true);

                if (shellOut.Contains("Error")) attachStatus = false; else attachStatus = true;
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "DetachInternals() cmd: " + dllShellCmd + " status: " + attachStatus);
            }
            catch (Exception) { return false; }
            return attachStatus;
        }


        internal static string AddStatusMsg(int taskId = -1, string statusMsg = null, string varString = null, char terminator = ',', bool isCutsceneBool = false, bool sendOnce = true, float statusDuration = 5.0f)
        {
            var isCutscene = isCutsceneBool.ToString().ToUpperInvariant();
            var isSendt = sendOnce.ToString().ToUpperInvariant();
            string statusMsgTask = "Task_New(" + taskId + ",\"StatusMessage\",\"StatusMsg\",0,0,0,0,0,0,\"" + varString + "\",\"" + statusMsg + "\",\"\"," + "\"message\"," + isSendt + "," + isCutscene + "," + statusDuration + ")" + terminator + "\n";
            return statusMsgTask;
        }

        internal static bool CheckInternalsAttached()
        {
            //Get all modules inside the process
            var objModulesList = Process.GetProcessesByName(QMemory.gameName);
            try
            {
                // Populate the module collection.
                var objModules = objModulesList[0].Modules;

                // Iterate through the module collection.
                foreach (ProcessModule objModule in objModules)
                {
                    //If the module exists
                    if (File.Exists(objModule.FileName.ToString()))
                    {
                        try
                        {
                            //Get Modification date
                            var objFileInfo = objModule.FileName.ToString();
                            if (objFileInfo.Contains(internalsDllFile))
                            {
                                return true;
                            }

                        }
                        catch (Exception ex) { }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return false;
        }

        internal static void UpdateViewPort(Real64 pos)
        {
            unsafe
            {
                GT.GT_WriteMemory(viewPortAddrX, "double", pos.x.ToString());
                GT.GT_WriteMemory(viewPortAddrY, "double", pos.y.ToString());
                GT.GT_WriteMemory(viewPortAddrZ, "double", pos.z.ToString());
            }
        }

        internal static Real64 GetViewPortPos()
        {
            unsafe
            {
                var pos = new Real64();
                pos.x = GT.GT_ReadDouble(viewPortAddrX);
                pos.y = GT.GT_ReadDouble(viewPortAddrY);
                pos.z = GT.GT_ReadDouble(viewPortAddrZ);
                return pos;
            }
        }

        internal static int GameitemsCount()
        {
            var gameItemAddr = GT.GT_ReadPointerOffset((IntPtr)0x0057BA9C, 0x0);
            var gameitems = GT.GT_ReadInt(gameItemAddr);
            return (int)gameitems;
        }

        private void ClearTempFiles()
        {
            // Cleaning up directories
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Cleaning up directories");
            string[] dconvFiles = Directory.GetFiles(Path.Combine(QUtils.qTools, @"DConv\input")).Concat(Directory.GetFiles(Path.Combine(QUtils.qTools, @"DConv\output"))).ToArray();
            string[] tgaConvFiles = Directory.GetFiles(Path.Combine(QUtils.qTools, @"TGAConv")).ToArray();
            foreach (string file in dconvFiles.Concat(tgaConvFiles))
            {
                try
                {
                    if (file.Contains(".exe")) continue; // Skip the TGAConv file.
                    File.Delete(file);
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Removed file: {file} successfully.");
                }
                catch (Exception ex)
                {
                    QLog.LogException(MethodBase.GetCurrentMethod().Name, ex);
                }
            }
        }

        internal static void CleanUpAiFiles()
        {
            if (!gameReset) return;
            var outputAiPath = cfgGamePath + gGameLevel + "\\ai\\";

            if (aiScriptFiles.Count >= 1)
            {
                foreach (var scriptFile in aiScriptFiles)
                {
                    FileIODelete(outputAiPath + scriptFile);
                    FileIODelete(outputAiPath + scriptFile.Replace("qvm", "qsc"));
                }
                FileIODelete(objectsQsc);
            }
        }

        internal static void CleanUpTmpFiles()
        {
            // Cleaning up directories
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Cleaning up temp directories");
            string[] dconvFiles = Directory.GetFiles(Path.Combine(QUtils.qTools, @"DConv\input")).Concat(Directory.GetFiles(Path.Combine(QUtils.qTools, @"DConv\output"))).ToArray();
            string[] tgaConvFiles = Directory.GetFiles(Path.Combine(QUtils.qTools, @"TGAConv")).ToArray();
            foreach (string file in dconvFiles.Concat(tgaConvFiles))
            {
                try
                {
                    if (file.Contains(".exe")) continue; // Skip the TGAConv file.
                    File.Delete(file);
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, $"Removed file: {file} successfully.");
                }
                catch (Exception ex)
                {
                    QLog.LogException(MethodBase.GetCurrentMethod().Name, ex);
                }
            }
        }

        internal static void EnableLogs()
        {
            if (!logEnabled)
                logEnabled = true;
        }

        internal static void DisableLogs()
        {
            if (logEnabled)
                logEnabled = false;
        }

        internal static void AddLog(string methodName, string logMsg)
        {
            if (logEnabled)
            {
                methodName = methodName.Replace("Btn_Click", String.Empty).Replace("_SelectedIndexChanged", String.Empty).Replace("_SelectedValueChanged", String.Empty);
                File.AppendAllText(editorLogFile, "[" + DateTime.Now.ToString("yyyy-MM-dd - HH:mm:ss") + "] " + methodName + "(): " + logMsg + "\n");
            }
        }

        internal static string GenerateRandStr(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[rand.Next(s.Length)]).ToArray());
        }

        internal static void Sleep(int seconds)
        {
            Thread.Sleep(seconds * 1000);
        }

        internal static void Sleep(float seconds)
        {
            Thread.Sleep((int)seconds * 1000);
        }

        internal static bool IsNonASCII(string str)
        {
            return (Encoding.UTF8.GetByteCount(str) != str.Length);
        }
    }

    internal static class Extensions
    {
        internal static string Slice(this string source, int start, int end)
        {
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;
            return source.Substring(start, len);
        }


        internal static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        internal static string ReplaceLast(this string text, string search, string replace)
        {
            int place = text.LastIndexOf(search);

            if (place == -1)
                return text;

            string result = text.Remove(place, search.Length).Insert(place, replace);
            return result;
        }

        internal static bool IsNonASCII(this string str)
        {
            return (Encoding.UTF8.GetByteCount(str) != str.Length);
        }

        internal static bool HasBinaryContent(this string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;
            return content.Any(ch => char.IsControl(ch) && ch != '\r' && ch != '\n');
        }

        internal static double ToRadians(this double angle)
        {
            // Angle in 10th of a degree
            return (angle * Math.PI) / 180;
        }
    }

    enum QTASKINFO
    {
        QTASK_ID,
        QTASK_NAME,
        QTASK_NOTE,
        //Object pos in Real64x3.
        QTASK_POSX,
        QTASK_POSY,
        QTASK_POSZ,
        //Object orientation in Real32x9.
        QTASK_ALPHA,
        QTASK_BETA,
        QTASK_GAMMA,
        //Mode ID in String16
        QTASK_MODEL,
    }

    enum PATROLACTIONS
    {
        PATROLPATH_ANIMATION,
        PATROLPATH_DELAY,
        PATROLPATH_WALKTO,
        PATROLPATH_RUNTO,
        PATROLPATH_CROUCH,
        PATROLPATH_LOOKATNODE,
        PATROLPATH_END,
        PATROLPATH_QUIT,
        PATROLPATH_SETSPEED
    }

    enum AITYPES
    {
        AITYPE_RPG,
        AITYPE_GUNNER,
        AITYPE_SNIPER,
        AITYPE_ANYA,
        AITYPE_EKK,
        AITYPE_PRIBOI,
        AITYPE_CIVILIAN,
        AITYPE_PATROL_UZI,
        AITYPE_PATROL_AK,
        AITYPE_PATROL_SPAS,
        AITYPE_PATROL_PISTOL,
        AITYPE_GUARD_UZI,
        AITYPE_GUARD_AK,
        AITYPE_GUARD_SPAS,
        AITYPE_GUARD_PISTOL,
        AITYPE_SECURITY_PATROL_UZI,
        AITYPE_SECURITY_PATROL_SPAS,
        AITYPE_MAFIA_PATROL_UZI,
        AITYPE_MAFIA_PATROL_AK,
        AITYPE_MAFIA_PATROL_SPAS,
        AITYPE_MAFIA_GUARD_UZI,
        AITYPE_MAFIA_GUARD_AK,
        AITYPE_MAFIA_GUARD_SPAS,
        AITYPE_SPETNAZ_PATROL_UZI,
        AITYPE_SPETNAZ_PATROL_AK,
        AITYPE_SPETNAZ_PATROL_SPAS,
        AITYPE_SPETNAZ_GUARD_UZI,
        AITYPE_SPETNAZ_GUARD_AK,
        AITYPE_SPETNAZ_GUARD_SPAS
    }

    internal class AreaDim
    {
        internal float x, y, z;

        internal AreaDim() { x = y = z = 0.0f; }

        internal AreaDim(float x)
        {
            this.x = x;
            this.y = x;
            this.z = x;
        }
        internal AreaDim(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    };

    internal class Real32
    {
        internal float alpha, beta, gamma;

        internal Real32() { alpha = beta = gamma = 0.0f; }
        internal Real32(float alpha)
        {
            this.alpha = alpha;
            this.beta = this.gamma = 0.0f;
        }

        internal Real32(float alpha, float beta)
        {
            this.alpha = alpha;
            this.beta = beta;
            this.gamma = 0.0f;
        }

        internal Real32(float alpha, float beta, float gamma)
        {
            this.alpha = alpha;
            this.beta = beta;
            this.gamma = gamma;
        }
    };

    internal class Real64
    {
        internal double x, y, z;
        internal Real64() { x = y = z = 0.0f; }
        internal Real64(double x)
        {
            this.x = x;
            this.y = this.z = 0.0f;
        }
        internal Real64(double x, double y)
        {
            this.x = x;
            this.y = y;
            this.z = 0.0f;
        }

        internal Real64(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        internal Real64 Real64Operator(Real64 real1, Real64 real2, string operatorType)
        {
            Real64 real = new Real64();

            if (operatorType == "+")
            {
                real.x = real1.x + real2.x;
                real.y = real1.y + real2.y;
                real.z = real1.z + real2.z;
            }
            else if (operatorType == "-")
            {
                real.x = real1.x - real2.x;
                real.y = real1.y - real2.y;
                real.z = real1.z - real2.z;
            }
            return real;
        }

        internal bool Empty()
        {
            bool empty = (this.x == 0.0f && this.y == 0.0f && this.z == 0.0f);
            return empty;
        }
    };
}
