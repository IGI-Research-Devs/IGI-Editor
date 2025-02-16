using System;
using System.Collections.Generic;
using System.Reflection;

namespace IGIEditor
{
    class QMission
    {
        private static uint[] missionNameOffsets = { 0x0, 0x4C, 0x98, 0xEC, 0x130, 0x17C, 0x1C8, 0x218, 0x264, 0x2B8, 0x308, 0x358, 0x3A8, 0x400 };
        private static uint[] missionDescOffsets = { 0x0, 0x4B0, 0x50C, 0x56C, 0x5C8, 0x630, 0x68C, 0x6E8, 0x744, 0x798, 0x7F0, 0x850, 0x8A8, 0x904 };

        internal static List<string> GetInstalledMissionList()
        {
            var missionsList = new List<string>();
            var missionDataLines = System.IO.File.ReadAllLines(QUtils.missionListFile);
            foreach (var missionData in missionDataLines)
            {
                var missionName = missionData.Slice(9, missionData.IndexOf("Level")).Trim();
                missionsList.Add(missionName);
            }
            return missionsList;
        }

        private static IntPtr GetMissionAddr()
        {
            uint missionStaticPtr = (uint)0x006758A8;
            uint[] missionOff = { 0x1F4 };
            IntPtr missionBasePtr = QLibc.GT.GT_ReadPointerOffset(QMemory.gtGameBase, missionStaticPtr);
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, " missionBasePtr : " + missionBasePtr.ToString("X4").ToUpperInvariant());
            IntPtr missionAddr = QLibc.GT.GT_ReadPointerOffsets(missionBasePtr, missionOff, (uint)(sizeof(uint) * missionOff.Length)) + 0x54;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "missionAddr : " + missionAddr.ToString("X4").ToUpperInvariant());

            return missionAddr;
        }

        internal static string AddLevelFlow(string completeTask, string failedTask, float maxPlayTime, bool timerEnabled = false)
        {
            string levelFlowTask = "Task_New(10, \"LevelFlow\", \"\", 0, 0, 0, 0, 0, 0, 0,\"" + completeTask + "\",\"" + failedTask + "\"," + timerEnabled.ToString().ToUpper() + ", " + maxPlayTime + ");";
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "completeTask : " + completeTask + "\tfailedTask : " + failedTask + "\tmaxPlayTime : " + maxPlayTime + "\ttimerEnabled : " + timerEnabled.ToString());
            return levelFlowTask;
        }

        internal static string GetMissionInfo(int gameLevel = -1, bool name = true, bool desc = false)
        {
            string missionInfo = "";
            try
            {
                var missionNameAddr = GetMissionAddr();
                var missionAddr1 = missionNameAddr;
                var missionAddr2 = missionNameAddr;

                if (gameLevel != -1)
                {
                    if (name)
                    {
                        gameLevel = gameLevel <= 1 ? 0 : gameLevel - 1;
                        var mNameAddr = missionNameAddr + (int)missionNameOffsets[gameLevel];
                        var mName = QLibc.GT.GT_ReadString(mNameAddr);
                        missionInfo = mName;
                    }

                    if (desc)
                    {
                        gameLevel = gameLevel <= 1 ? 0 : gameLevel - 1;
                        var mDescAddr = missionNameAddr + (int)missionDescOffsets[gameLevel];
                        var mDesc = QLibc.GT.GT_ReadString(mDescAddr);
                        missionInfo = mDesc;
                    }
                }

                else
                {
                    for (int index = 0; index < QUtils.GAME_MAX_LEVEL; ++index)
                    {
                        QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Name Address : " + missionNameAddr.ToString("X4"));
                        QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Desc Address : " + missionNameAddr.ToString("X4"));

                        string mName = QLibc.GT.GT_ReadString(missionAddr1);
                        string mDesc = QLibc.GT.GT_ReadString(missionAddr2);
                        if (name) QLog.ShowInfo("Mission " + (index + 1).ToString() + " : " + mName);
                        if (desc) QLog.ShowInfo("Mission " + (index + 1).ToString() + " : " + mDesc);

                        if (index == 13) break;
                        missionAddr1 = missionNameAddr + (int)missionNameOffsets[index];
                        missionAddr2 = missionNameAddr + (int)missionDescOffsets[index];
                    }
                }
            }
            catch (Exception ex)
            {
                QLog.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return missionInfo;
        }

        internal static void ChangeMissionDetails(int gameLevel = -1, string missionNewName = null, string missionNewDesc = null)
        {
            if (gameLevel <= 0) gameLevel = QMemory.GetRunningLevel();
            var missionAddr = GetMissionAddr();
            var missionNameAddr = missionAddr + (int)missionNameOffsets[gameLevel - 1];
            var missionDescAddr = missionAddr + (int)missionDescOffsets[gameLevel - 1];

            var missionOldName = QLibc.GT.GT_ReadString(missionNameAddr);
            var missionOldDesc = QLibc.GT.GT_ReadString(missionDescAddr);

            if (missionNewName.Length > missionOldName.Length || missionNewDesc.Length > missionOldDesc.Length)
                QLog.ShowError("Mission details length mismatch");
            else if (String.IsNullOrEmpty(missionNewName) || String.IsNullOrEmpty(missionNewDesc))
                QLog.ShowError("Mission details cannot be empty");
            else
            {
                QLibc.GT.GT_WriteMemory(missionNameAddr, "string", missionNewName);
                QLibc.GT.GT_WriteMemory(missionDescAddr, "string", missionNewDesc);
                //QLog.ShowInfo("Mission details updated successfully");
            }
        }

    }
}
