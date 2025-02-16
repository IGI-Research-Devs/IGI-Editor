using QLibc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IGIEditor
{
    class QHuman
    {

        internal static string AddWeapon(string weapon, int ammo, bool autoModel = true, bool supressErr = false)
        {
            string qscData = QUtils.LoadFile();
            if (!autoModel)
                weapon = QUtils.weaponId + weapon;

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Trying to add weapon : '" + weapon + "' with ammo : " + ammo);

            string idIndexStr = "Task_New(0";
            string gunIndexStr = "Task_New(-1, \"Gun\"";
            int idIndex = qscData.IndexOf(idIndexStr);
            int gunIndex = qscData.IndexOf(gunIndexStr, idIndex);

            if (CheckWeaponExist(weapon))
            {
                if (!supressErr)
                    QLog.ShowError("Weapon : " + weapon + " already exist for human");
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Weapon : " + weapon + " does exist for human");
                return null;
            }

            string gun = AddWeapon(weapon, ammo);
            qscData = qscData.Insert(gunIndex, gun);
            return qscData;
        }


        internal static string AddWeapon(string weapon, int ammo)
        {
            //Primary ammo slot.
            string ammoIdPrimary = GetAmmo4Weapon(weapon);
            string ammoIdSecondary = null;

            //Secondary ammo slot.
            if (weapon.Contains("M16A2"))
                ammoIdSecondary = GetAmmo4Weapon("M203");

            string gunStr = "Gun", weaponStr = (weapon.Replace(QUtils.weaponId, String.Empty));

            //Exceptions for special weapons like Dragunov,MP5 with zoom functionality.
            if (weapon.Contains("MP5SD") || weapon.Contains("DRAGUNOV")
                || weapon.Contains("M16A2") || weapon.Contains("SPAS12"))
                gunStr += weaponStr;

            //For Mine types.
            if (weapon.Contains("PROXIMITYMINE"))
                gunStr = "ProximityMine";

            //For Binocular.
            if (weapon.Contains("BINOCULARS"))
                gunStr = "Binocular";

            string gunTask = "Task_New(-1, \"" + gunStr + "\", \"WEAPON\"," + "\"" + weapon + "\"" + ",0)," + "\n";
            string weaponTask = gunTask;

            //If ammo not found then don't add ammo task.
            if (!String.IsNullOrEmpty(ammoIdPrimary))
            {
                string ammoTaskPrimary = "Task_New(-1, \"AddAmmo\", \"AMMO\"," + "\"" + ammoIdPrimary + "\"" + "," + ammo + ")," + "\n";
                string ammoTaskSecondary = "Task_New(-1, \"AddAmmo\", \"AMMO\"," + "\"" + ammoIdSecondary + "\"" + "," + ammo + ")," + "\n";
                weaponTask += ammoTaskPrimary;

                if (!String.IsNullOrEmpty(ammoIdSecondary))
                    weaponTask += ammoTaskSecondary;
            }

            return weaponTask;
        }

        internal static string RemoveWeapon(string weapon, bool autoModel, bool checkExist = true)
        {
            string qscData = QUtils.LoadFile();
            if (!autoModel)
                weapon = QUtils.weaponId + weapon;

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Trying to remove weapon : " + weapon);
            if (!CheckWeaponExist(weapon) && checkExist)
            {
                QLog.ShowLogError(MethodBase.GetCurrentMethod().Name, "Weapon: " + weapon + " doesn't exist for human");
                return null;
            }

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Weapon found to remove weapon : " + weapon);
            string idIndexStr = "Task_New(0";
            int idIndex = qscData.IndexOf(idIndexStr);
            var qscTemp = qscData.Substring(idIndex).Split('\n');
            string gunSubStr = null;

            foreach (var data in qscTemp)
            {
                if (data.Contains(weapon))
                {
                    gunSubStr = data;
                    break;
                }
            }

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Weapon string : " + gunSubStr);
            var gunIndex = qscData.LastIndexOf(gunSubStr);

            qscData = qscData.Remove(gunIndex, gunSubStr.Length);
            qscData = Regex.Replace(qscData, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
            qscData = qscData.Replace("\t", String.Empty);

            return qscData;
        }

        internal static string RemoveWeapons()
        {
            string qscData = null;
            var humanData = GetHumanTaskList();
            foreach (var weapon in humanData.weaponsList)
            {
                qscData = RemoveWeapon(weapon, true, false);
                QUtils.SaveFile(QUtils.objectsQsc, qscData);
            }
            return qscData;
        }


        private static string GetAmmo4Weapon(string weapon)
        {
            string ammoId = null;
            if (weapon == null) { QLog.ShowError("GetAmmo4Weapon : Weapon name not provided"); return null; }
            weapon = weapon.Replace(QUtils.weaponId, String.Empty);

            foreach (var ammo in QUtils.ammoList)
            {
                if (ammo.Key.Contains(weapon))
                {
                    ammoId = ammo.Value;
                    break;
                }
            }
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "weapon : " + weapon + " with ammo : " + ammoId);

            return ammoId;
        }

        internal static List<Dictionary<string, int>> GetWeaponsList()
        {
            var weaponsList = new List<Dictionary<string, int>>();
            foreach (var weapon in QUtils.weaponDataList)
            {
                var weaponObj = new Dictionary<string, int>();
                string weaponName = weapon.scriptId.Replace(QUtils.weaponId, String.Empty).Replace("\"", String.Empty);
                weaponObj.Add(weaponName, weapon.weaponId);
                weaponsList.Add(weaponObj);
            }
            return weaponsList;
        }

        private static bool CheckWeaponExist(string weapon)
        {
            weapon = weapon.Replace(QUtils.weaponId, String.Empty);
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Weapon : " + weapon);

            var humanData = GetHumanTaskList();
            bool found = false;
            foreach (var humanWeapon in humanData.weaponsList)
            {
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Weapon_List : " + humanWeapon);
                if (humanWeapon.Contains(weapon))
                {
                    found = true;
                    break;
                }
            }
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "returned : " + found.ToString());
            return found;
        }

        internal static QUtils.HTask GetHumanTaskList(bool fromBackup = false)
        {
            //Declare types to store position to qtask.
            QUtils.HTask htask = new QUtils.HTask();
            htask.qtask = new QUtils.QScriptTask();
            htask.weaponsList = new List<string>();

            Real32 orientation = new Real32();
            Real64 position = new Real64();

            string inputQscPath = QUtils.cfgQscPath + QUtils.gGameLevel + "\\" + QUtils.objectsQsc;

            string qscData = (fromBackup) ? QUtils.LoadFile(inputQscPath) : QUtils.LoadFile();

            //if (qscData.IsNonASCII()) qscData = QCryptor.Decrypt(QUtils.objectsQsc);

            string idIndexStr = "Task_New(0";
            int idIndex = qscData.IndexOf(idIndexStr);
            string qscTemp = qscData.Substring(idIndex);
            var taskNew = qscTemp.Split(',');

            //Parse all the data.
            position.x = Double.Parse(taskNew[(int)QTASKINFO.QTASK_POSX]);
            position.y = Double.Parse(taskNew[(int)QTASKINFO.QTASK_POSY]);
            position.z = Double.Parse(taskNew[(int)QTASKINFO.QTASK_POSZ]);
            orientation.alpha = float.Parse(taskNew[(int)QTASKINFO.QTASK_ALPHA]);
            htask.team = Convert.ToInt32(taskNew[(int)QTASKINFO.QTASK_GAMMA].Trim());

            //Adding position and orientation to qtask.
            htask.qtask.position = position;
            htask.qtask.orientation = orientation;

            string weaponRegex = "[A-Z]{6}_[A-Z]{2}_[A-Z0-9]*";
            var qscSub = qscData.Substring(idIndex).Split('\n');
            //int weaponsIndex = 0;
            //int maxWeapons = 0x18;

            foreach (var data in qscSub)
            {
                var matchData = Regex.Match(data, weaponRegex);
                if (matchData.Success)
                    htask.weaponsList.Add(matchData.Value);

                //Break after reaching max weapons limit.
                int endCount = data.Count(c => c == ')');
                if (endCount > 1) break;
                //if (weaponsIndex > maxWeapons) break;
                //weaponsIndex++;
            }
            return htask;
        }

        static internal Real32 GetPositionCoord(bool addLog = true)
        {
            uint posBaseAddr = (uint)QMemory.GetHumanBaseAddress(false) + (uint)0x24;

            IntPtr xPosAddr = (IntPtr)posBaseAddr + 0x0;
            IntPtr yPosAddr = (IntPtr)posBaseAddr + 0x8;
            IntPtr zPosAddr = (IntPtr)posBaseAddr + 0x10;

            var xpos = GT.GT_ReadFloat(xPosAddr);
            var ypos = GT.GT_ReadFloat(yPosAddr);
            var zpos = GT.GT_ReadFloat(zPosAddr);

            var position = new Real32(xpos, ypos, zpos);
            if (addLog)
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "posBaseAddr:" + posBaseAddr + " xpos : " + xpos + " ypos : " + ypos + " zpos : " + zpos + " position: " + position);
            return position;
        }

        static internal Real64 GetPositionInMeter(bool addLog = true)
        {
            uint posBaseAddr = (uint)0x005CA138;
            IntPtr xPosAddr = (IntPtr)posBaseAddr + 0x0;
            IntPtr yPosAddr = (IntPtr)posBaseAddr + 0x8;
            IntPtr zPosAddr = (IntPtr)posBaseAddr + 0x10;

            var xpos = GT.GT_ReadDouble(xPosAddr);
            var ypos = GT.GT_ReadDouble(yPosAddr);
            var zpos = GT.GT_ReadDouble(zPosAddr);

            double x = Convert.ToDouble(Decimal.Truncate(Convert.ToDecimal(xpos)));
            double y = Convert.ToDouble(Decimal.Truncate(Convert.ToDecimal(ypos)));
            double z = Convert.ToDouble(Decimal.Truncate(Convert.ToDecimal(zpos)));

            //Fix this angle for Ground reference.
            var position = new Real64(x, y, z - QMemory.deltaToGround);
            if (addLog)
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "posBaseAddr:" + posBaseAddr + " xpos : " + xpos + " ypos : " + ypos + " zpos : " + zpos + " position: " + position);
            return position;
        }

        internal static string UpdatePositionInMeter(Real64 position, float angle = 0.0f)
        {
            var humanData = GetHumanTaskList();
            string qscData = QUtils.LoadFile();

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with position : X:" + position.x + " Y: " + position.y + " Z: " + position.z + ", Alpha : " + angle);
            string humanAngle = angle == 0.0f ? humanData.qtask.orientation.alpha.ToString() : angle.ToString("0.0");

            string humanTaskId = "Task_New(0";
            int qtaskIndex = qscData.IndexOf(humanTaskId);
            int newlineIndex = qscData.IndexOf("\n", qtaskIndex);

            string humanTask = "Task_New(0,\"HumanPlayer\",\"Jones\"," + position.x + "," + position.y + "," + position.z + "," + humanAngle + ",\"000_01_1\",0,";
            qscData = qscData.Remove(qtaskIndex, newlineIndex - qtaskIndex).Insert(qtaskIndex, humanTask);
            return qscData;
        }

        internal static string UpdatePositionOffset(Real64 position, float alpha = 0.0f)
        {
            var humanData = GetHumanTaskList();
            bool xVal = (position.x == 0.0f) ? false : true;
            bool yVal = (position.y == 0.0f) ? false : true;
            bool zVal = (position.z == 0.0f) ? false : true;

            int xLen = xVal ? position.x.ToString().Length : 0;
            int yLen = yVal ? position.y.ToString().Length : 0;
            int zLen = zVal ? position.z.ToString().Length : 0;

            QLog.AddLog("Human " + MethodBase.GetCurrentMethod().Name, "length : X:" + xLen + " Y: " + yLen + " Z: " + zLen);
            QLog.AddLog("Human " + MethodBase.GetCurrentMethod().Name, "called with offset : X:" + position.x + " Y: " + position.y + " Z: " + position.z);


            //Check for length error.
            if (xLen > 3 || yLen > 3 || zLen > 3)
                throw new ArgumentOutOfRangeException("Offsets are out of range");

            int[] meterOffsets = { 100000, 1000000, 10000000 };

            //Add meter offset to distance. (M/S) .
            if (xVal) position.x = humanData.qtask.position.x + meterOffsets[xLen - 1];
            if (yVal) position.y = humanData.qtask.position.y + meterOffsets[yLen - 1];
            if (zVal) position.z = humanData.qtask.position.z + meterOffsets[zLen - 1];

            string humanXPos = position.x == 0.0f ? humanData.qtask.position.x.ToString("0.0") : position.x.ToString("0.0");
            string humanYPos = position.y == 0.0f ? humanData.qtask.position.y.ToString("0.0") : position.y.ToString("0.0");
            string humanZPos = position.z == 0.0f ? humanData.qtask.position.z.ToString("0.0") : position.z.ToString("0.0");
            string humanAlpha = alpha == 0.0f ? humanData.qtask.orientation.alpha.ToString() : alpha.ToString("0.0");


            string qscData = QUtils.LoadFile();

            QLog.AddLog("Human " + MethodBase.GetCurrentMethod().Name, "calculated positions with offsets : X:" + position.x + " Y: " + position.y + " Z: " + position.z);

            string humanTaskId = "Task_New(0";
            int qtaskIndex = qscData.IndexOf(humanTaskId);
            int newlineIndex = qscData.IndexOf("\n", qtaskIndex);

            string humanTask = "Task_New(0,\"HumanPlayer\",\"Jones\"," + humanXPos + "," + humanYPos + "," + humanZPos + "," + humanAlpha + ",\"000_01_1\",0,";
            qscData = qscData.Remove(qtaskIndex, newlineIndex - qtaskIndex).Insert(qtaskIndex, humanTask);
            return qscData;
        }

        internal static string UpdateTeamId(int teamId)
        {
            var humanData = GetHumanTaskList();
            string qscData = QUtils.LoadFile();

            var position = humanData.qtask.position;
            float angle = humanData.qtask.orientation.alpha;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with position : X:" + position.x + " Y: " + position.y + " Z: " + position.z + ", Alpha : " + angle);
            string humanAngle = angle == 0.0f ? humanData.qtask.orientation.alpha.ToString() : angle.ToString("0.0");

            string humanTaskId = "Task_New(0";
            int qtaskIndex = qscData.IndexOf(humanTaskId);
            int newlineIndex = qscData.IndexOf("\n", qtaskIndex);

            string humanTask = "Task_New(0,\"HumanPlayer\",\"Jones\"," + position.x + "," + position.y + "," + position.z + "," + humanAngle + ",\"000_01_1\"," + teamId + ",";
            qscData = qscData.Remove(qtaskIndex, newlineIndex - qtaskIndex).Insert(qtaskIndex, humanTask);
            return qscData;
        }


        internal static string UpdateOrientation(float alpha)
        {
            var humanData = GetHumanTaskList();
            QLog.AddLog("Human " + MethodBase.GetCurrentMethod().Name, "called with alpha : " + alpha);
            return UpdatePositionInMeter(humanData.qtask.position, alpha);
        }

        internal static void UpdateHumanPlayerSpeed(double movSpeed = 1.75f, double forwardSpeed = 17.5f, double upwardSpeed = 27.0f, double inAirSpeed = 0.5f)
        {
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "movSpeed: " + movSpeed + " forwardSpeed: " + forwardSpeed + " upwardSpeed: " + upwardSpeed + " inAirSpeed: " + inAirSpeed);
            UpdateHumanPlayerParams(movSpeed, forwardSpeed, upwardSpeed, inAirSpeed, QUtils.peekLRLen, QUtils.peekCrouchLen, QUtils.peekTime, QUtils.healthScale, QUtils.healthScaleFence);
        }

        internal static void UpdateHumanPlayerHealth(double healthScale = 3.0f, double healthScaleFence = 0.5f, int healthScaleFall = 0)
        {
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, " healthScale: " + healthScale + " healthScaleFence: " + healthScaleFence + " healthScaleFall: " + healthScaleFall);
            UpdateHumanPlayerParams(QUtils.movSpeed, QUtils.forwardSpeed, QUtils.upwardSpeed, QUtils.inAirSpeed, QUtils.peekLRLen, QUtils.peekCrouchLen, QUtils.peekTime, healthScale, healthScaleFence);
            if (healthScaleFall != -1) QMemory.UpdateHumanHealth((QUtils.HEALTH_ACTION)healthScaleFall);
        }

        internal static void UpdateHumanPlayerPeek(double peekLRLen = 0.8500000238418579f, double peekCrouchLen = 0.8500000238418579f, double peekTime = 0.25f)
        {
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "peekLRLen: " + peekLRLen + " peekCrouchLen: " + peekCrouchLen + " peekTime: " + peekTime);
            UpdateHumanPlayerParams(QUtils.movSpeed, QUtils.forwardSpeed, QUtils.upwardSpeed, QUtils.inAirSpeed, peekLRLen, peekCrouchLen, peekTime, QUtils.healthScale, QUtils.healthScaleFence);
        }

        internal static void UpdateHumanPlayerParams(double movementSpeed = 1.75f, double forwardJumpSpeed = 17.5f, double upwardJumpSpeed = 27, double inAirSpeed = 0.5f, double peekLeftRightLen = 0.8500000238418579f, double peekCrouchLen = 0.8500000238418579f, double peekTimeLen = 0.25f, double healthDamageScale = 3.0f, double healthFenceDamageScale = 0.5f)
        {
            var humanPlayerFile = QUtils.cfgHumanplayerPathQsc + @"\humanplayer" + QUtils.FileExtensions.Qsc;
            string humanPlayerData = QUtils.LoadFile(humanPlayerFile);
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "movementSpeed: " + movementSpeed + " forwardSpeed: " + forwardJumpSpeed + " upwardJumpSpeed: " + upwardJumpSpeed + " inAirSpeed: " + inAirSpeed + " peekLeftRightLen: " + peekLeftRightLen + " peekCrouchLen: " + peekCrouchLen + " peekLen: " + peekTimeLen + " healthDamageScale: " + healthDamageScale + " healthFenceDamageScale: " + healthFenceDamageScale);

            //Add movement speed param.
            if (humanPlayerData.Contains(QUtils.movementSpeedMask))
                humanPlayerData = humanPlayerData.ReplaceFirst(QUtils.movementSpeedMask, movementSpeed.ToString());

            //Add forward speed param.
            if (humanPlayerData.Contains(QUtils.forwardSpeedMask))
                humanPlayerData = humanPlayerData.ReplaceFirst(QUtils.forwardSpeedMask, forwardJumpSpeed.ToString());

            //Add upward speed param.
            if (humanPlayerData.Contains(QUtils.upwardSpeedMask))
                humanPlayerData = humanPlayerData.ReplaceFirst(QUtils.upwardSpeedMask, upwardJumpSpeed.ToString());

            //Add in air speed param.
            if (humanPlayerData.Contains(QUtils.inAirSpeedMask))
                humanPlayerData = humanPlayerData.ReplaceFirst(QUtils.inAirSpeedMask, inAirSpeed.ToString());

            //Add peek Left Right Length param.
            if (humanPlayerData.Contains(QUtils.peekLeftRightLenMask))
                humanPlayerData = humanPlayerData.ReplaceFirst(QUtils.peekLeftRightLenMask, peekLeftRightLen.ToString());

            //Add peek crouch length param.
            if (humanPlayerData.Contains(QUtils.peekCrouchLenMask))
                humanPlayerData = humanPlayerData.ReplaceFirst(QUtils.peekCrouchLenMask, peekCrouchLen.ToString());

            //Add peek time param.
            if (humanPlayerData.Contains(QUtils.peekTimeMask))
                humanPlayerData = humanPlayerData.ReplaceFirst(QUtils.peekTimeMask, peekTimeLen.ToString());

            //Add health damage scale param.
            if (humanPlayerData.Contains(QUtils.healthScaleMask))
                humanPlayerData = humanPlayerData.ReplaceFirst(QUtils.healthScaleMask, healthDamageScale.ToString());

            //Add health fence damage scale param.
            if (humanPlayerData.Contains(QUtils.healthFenceMask))
                humanPlayerData = humanPlayerData.ReplaceFirst(QUtils.healthFenceMask, healthFenceDamageScale.ToString());

            string humanFileName = "humanplayer.qsc";
            var outputHumanPlayerPath = QUtils.gameAbsPath + "\\humanplayer\\";

            QUtils.SaveFile(humanFileName, humanPlayerData);
            bool status = QCompiler.Compile(humanFileName, outputHumanPlayerPath, 0x0);
            QUtils.FileIODelete(humanFileName);

            if (status) QInternals.HumanplayerLoad();
        }
    }
}
