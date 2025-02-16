using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IGIEditor
{
    [Serializable]
    class QServer
    {
        internal static string serverBaseURL = @"ftp://igiresearchdevelopers.orgfree.com/";
        internal static string missionDir = "QMissions";
        internal static string graphsDir = "QGraphs";
        internal static string updateDir = "QUpdater";
        internal static string resourceDir = "QResources";

        internal struct QServerData
        {
            string persmission;
            string ownerGroup;
            string fileSize;
            string fileName;

            public QServerData(string persmission, string ownerGroup, string fileSize, string fileName)
            {
                this.persmission = persmission;
                this.ownerGroup = ownerGroup;
                this.fileSize = fileSize;
                this.fileName = fileName;
            }

            public string FileName { get => fileName; set => fileName = value; }
            public string FileSize { get => fileSize; set => fileSize = value; }
            public string OwnerGroup { get => ownerGroup; set => ownerGroup = value; }
            public string Persmission { get => persmission; set => persmission = value; }
        }
        [Serializable]
        internal struct QMissionsData
        {
            string missionName;
            float missionSize;
            string missionAuthor;
            int missionLevel;

            public QMissionsData(string missionName, float missionSize, string missionAuthor, int missionLevel)
            {
                this.missionName = missionName;
                this.missionSize = missionSize;
                this.missionAuthor = missionAuthor;
                this.missionLevel = missionLevel;
            }

            public string MissionName { get => missionName; set => missionName = value; }
            public float MissionSize { get => missionSize; set => missionSize = value; }
            public string MissionAuthor { get => missionAuthor; set => missionAuthor = value; }
            public int MissionLevel { get => missionLevel; set => missionLevel = value; }
        }

        private static QFtpClient GetQFtpClient()
        {
            string qHash1, qHash2, qHashTmp;
            qHash1 = qHashTmp = serverBaseURL.Slice(serverBaseURL.IndexOf("://") + 3, serverBaseURL.LastIndexOf(@"/"));
            qHash2 = qHashTmp = qHash1.Substring(0, 3) + "#p@ro@z##" + "#.@@h###m@";
            qHash2 = qHash2.Replace("@", String.Empty).Replace("#", String.Empty);
            var qftpClient = new QFtpClient(serverBaseURL, qHash1, qHash2);
            return qftpClient;
        }

        internal static bool Upload(string remoteFile, string localFile)
        {
            bool status = false;
            try
            {
                var qftpClient = GetQFtpClient();
                qftpClient.upload(remoteFile, localFile);
                status = true;
            }
            catch (Exception ex)
            {
                QLog.ShowLogException("Server" + MethodBase.GetCurrentMethod().Name, ex);
                status = false;
            }
            return status;
        }

        internal static bool Download(string remoteFile, string localFile, string destPath)
        {
            bool status;
            try
            {
                var qftpClient = GetQFtpClient();
                qftpClient.download(remoteFile, localFile);
                QUtils.ShellExec("move /Y " + localFile + " " + destPath);
                status = true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("File unavailable")) QLog.ShowLogError(MethodBase.GetCurrentMethod().Name, "File '" + localFile + "' was not found on server.");
                else
                    QLog.ShowLogException("Server" + MethodBase.GetCurrentMethod().Name, ex);
                status = false;
            }
            return status;
        }

        internal static bool Delete(string remoteFile)
        {
            bool status = false;
            try
            {
                var qftpClient = GetQFtpClient();
                qftpClient.delete(remoteFile);
                status = true;
            }
            catch (Exception ex)
            {
                QLog.ShowLogException("Server" + MethodBase.GetCurrentMethod().Name, ex);
                status = false;
            }
            return status;
        }


        internal static List<QServerData> GetDirList(string dirName, bool useCache = false, List<string> extensions = null)
        {
            if (useCache)
            {
                if (QUtils.qServerDataList.Count > 0)
                    return QUtils.qServerDataList;
            }

            var qftpClient = GetQFtpClient();
            var dirs = qftpClient.directoryListDetailed(dirName);
            var qServerData = new List<QServerData>();

            foreach (var dir in dirs)
            {
                var qServer = new QServerData();
                var dirData = dir.Split(' ');
                int dirDataSize = dirData.Length;
                try
                {
                    qServer.FileName = extensions.Any(dirData[dirDataSize - 1].Contains) ? dirData[dirDataSize - 1] : dirData[dirDataSize - 1] + dirData[dirDataSize];
                    if (dirName == missionDir && !qServer.FileName.Contains(QUtils.FileExtensions.Mission)) continue;
                    qServer.Persmission = dirData[0];
                    qServer.OwnerGroup = dirData[5];

                    for (int i = 17; i <= 19; i++)
                    {
                        var dataSize = dirData[i];
                        if (dataSize.Length > 3 && dataSize.All(char.IsDigit))
                        {
                            qServer.FileSize = dirData[i];
                            break;
                        }
                    }
                    qServerData.Add(qServer);
                }
                catch (Exception) { }
            }
            QUtils.qServerDataList = qServerData;
            return qServerData;
        }

        internal static List<QMissionsData> GetMissionsData(bool useCache = false)
        {
            var missionsDataList = new List<QMissionsData>();
            try
            {
                if (useCache)
                {
                    if (QUtils.qServerMissionDataList.Count > 0)
                    {
                        QLog.AddLog(MethodBase.GetCurrentMethod().Name, " using cache method.");
                        return QUtils.qServerMissionDataList;
                    }
                }
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "using normal method.");
                var dirList = GetDirList(missionDir, useCache, new List<string>() { QUtils.FileExtensions.Mission });

                foreach (var dir in dirList)
                {
                    try
                    {
                        string dirFileName = dir.FileName;
                        string missionName = dirFileName.Slice(0, dirFileName.IndexOf("@"));
                        float missionSize = 0.0f;

                        if (!String.IsNullOrEmpty(dir.FileSize))
                            missionSize = (float)Convert.ToInt32(dir.FileSize) / 1000;

                        string missionAuthor = dirFileName.Slice(dirFileName.IndexOf("@") + 1, dirFileName.IndexOf("#"));

                        string missionLevelStr = dirFileName.Slice(dirFileName.IndexOf("#") + 1, dirFileName.IndexOf("."));
                        int missionLevel = Convert.ToInt32(missionLevelStr);

                        var missionData = new QMissionsData(missionName, missionSize, missionAuthor, missionLevel);
                        missionsDataList.Add(missionData);
                    }
                    catch (Exception ex)
                    {
                        QLog.LogException(MethodBase.GetCurrentMethod().Name, ex);
                    }
                }
            }

            catch (Exception ex)
            {
                QLog.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }

            QUtils.qServerMissionDataList = missionsDataList;
            return missionsDataList;
        }

    }
}
