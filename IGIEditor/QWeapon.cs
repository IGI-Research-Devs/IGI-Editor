using QLibc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IGIEditor
{
    enum WEAPONCFG
    {
        //ID(s) in //Int32
        WEAPON_ID = 3,
        //Properties in String256
        SCRIPT_ID,
        WEAPON_NAME,
        WEAPON_MANUFACTURER,
        WEAPON_DESCRIPTION,

        //Properties in EnumInt32
        WEAPON_TYPE_ENUM,
        WEAPON_CROSSHAIR_TYPE,
        AMMO_DISPLAY_TYPE,

        //Properties in int32.
        WEAPON_MASS,//in grams.
        WEAPON_CALIBER_ID,

        //Properties in Real32.
        WEAPON_DAMAGE,
        WEAPON_POWER,
        WEAPON_RELOAD_TIME,
        WEAPON_MUZZLE_VEL,//Muzzle velocity.

        //Properties in Int32.
        WEAPON_BULLETS,
        WEAPON_RPM, //Rounds per minute.
        WEAPON_CLIPS, //Magazine
        WEAPON_BURST,

        //Properties in Real32.
        WEAPON_MIN_RAND_SPEED,
        WEAPON_MAX_RAND_SPEED,
        WEAPON_FIX_VIEW_CHANGE_X, //Fix view X-Axis
        WEAPON_FIX_VIEW_CHANGE_Z, //Fix view Z-Axis
        WEAPON_RAND_VIEW_CHANGE_X, //Rand view Z-Axis
        WEAPON_RAND_VIEW_CHANGE_Z, //Rand view Z-Axis

        WEAPON_TYPE_STR, //String256
        WEAPON_RANGE,//Real32
        WEAPON_USERS,//String256

        //Properties in int32.
        WEAPON_LENGTH,
        BARREL_LENGTH,

        //Properties in String16
        WEAPON_GUN_MODEL,
        WEAPON_CASING_MODEL,

        //Animations in int32.
        WEAPON_STAND_ANIM,
        WEAPON_MOVE_ANIM,
        WEAPON_FIRE_ANIM1,
        WEAPON_FIRE_ANIM2,
        WEAPON_FIRE_ANIM3,
        WEAPON_RELOAD_ANIM,
        WEAPON_UPPERBODYSTAND_ANIM,
        WEAPON_UPPERBODYWALK_ANIM,
        WEAPON_UPPERBODYCROUCH_ANIM,
        WEAPON_UPPERBODYCROUCHRUN_ANIM,
        WEAPON_UPPERBODYRUN_ANIM,
        WEAPON_UPPERBODYFIRE_ANIM,
        WEAPON_UPPERBODYRELOAD_ANIM,

        //Sound in String16
        WEAPON_SOUND_SINGLE,
        WEAPON_SOUND_LOOP,

        WEAPON_DETECTION_RANGE,//EnumReal32
        WEAPON_PROJECTILE_TASK_TYPE,//EnumInt32
        WEAPON_TASK_TYPE,//EnumInt32
        WEAPON_EMPTY_ON_CLEAR,//bool8
    }

    public class Weapon
    {
        //Properties in int32.
        public Int32 weaponId, mass, caliberId, bullets, roundsPerMinute, roundsPerClip, burst, weaponLength, barrelLength,
            animStand, animMove, animFire1, animFire2, animFire3, animReload, animUpperbodystand,
            animUpperbodywalk, animUpperbodycrouch, animUpperbodycrouchrun, animUpperbodyrun, animUpperbodyfire, animUpperbodyreload;

        //Properties in Real32.
        public Single damage, power, reloadTime, muzzleVelocity, minRandSpeed, maxRandSpeed, weaponRange, fixViewX, fixViewZ, randViewX, randViewZ;

        //Properties in String256
        public string scriptId, weaponName, manufacturer, description, typeStr, weaponUsers, gunModel, casingModel, soundSingle, soundLoop;

        //Properties in EnumInt32
        public string typeEnum, crosshairType, ammoDispType, projectileTaskType, weaponTaskType;

        //Properties in EnumReal32
        public string detectionRange;

        //Properties in bool8
        public bool emptyOnClear;
    };

    class QWeapon
    {
        private static string weaponsConfigIn = QUtils.weaponsCfgQscPath;
        private static string weaponsConfigOut = QUtils.weaponsGamePath;
        private static string weaponCfgTask = "WeaponConfig";
        //private static string weaponCfgTask = "Task_New(-1, \"WeaponConfig\",";

        internal static List<String> GetWeaponSFXList()
        {
            var sfxList = new List<string>()
             {
                 "ak47_loop_e","ak47_reload_1","ak47_reload_2","ak47_reload_3",
                 "glock_reload_1","glock_reload_2","glock_reload_3","glock_shot_1","glock_shot_2",
                 "jackh_loop","jackh_loop_e","jackh_reload_1","jackh_reload_2","jackh_reload_3","jackh_reload_4",
                 "knife_1","knife_2",
                 "m16_loop","m16_loop_e","m16_reload_1","m16_reload_2","m16_reload_3",
                 "m203_launch_1","m2hb_loop","m2hb_loop_e","mil_loop",
                 "minimi_loop","minimi_loop_e","minimi_reload_1","minimi_reload_2",
                 "missile_away_01","missile_imp_01","missile_loop_01",
                 "mp5sd_loop","mp5sd_loop_e","mp5_reload_5",
                 "rpg_launch_1","stab_0",
                 "sentrygun_cut","sentrygun_cut2",
                 "spas12_bulins_1","spas12_bulins_2","spas12_bulins_3","spas12_bulins_4",
                 "spas12_reload_1","spas12_reload_2","spas12_shot_1",
                 "uzi_loop","uzix2_loop","uzi_loop_e","uzi_reload_1","uzi_reload_2","uzi_reload_3",
                 "colt_shot_1","deagle_shot_1","svddrag_shot_1",
                 "grenade_shot_1","bin_zoom_1"
                 };
            return sfxList;
        }

        internal static List<Weapon> GetWeaponTaskList(bool advanceData = false)
        {
            var qscData = LoadWeaponsConfig();
            var weaponTaskList = ParseWeaponConfig(qscData, advanceData);
            return weaponTaskList;
        }

        internal static int WeaponUpdateCurrent()
        {
            int weaponIndex = 0;
            IntPtr weaponAddress = QMemory.GetWeaponAddress();
            int currentWeaponId = (int)GT.GT_ReadInt(weaponAddress);

            string weaponName = GT.GT_ReadString((IntPtr)0x00943A18);
            weaponName = weaponName.Replace("weapons/strings/", String.Empty).Replace(" ", String.Empty);

            foreach (var weaponDDName in QUtils.weaponDDList.Select((value, index) => new { index, value }))
            {
                if (weaponName.ToUpper().Contains(weaponDDName.value.ToUpper()))
                {
                    weaponIndex = weaponDDName.index;
                    break;
                }
            }
            return weaponIndex;
        }

        private static string LoadWeaponsConfig()
        {
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "weaponsConfigIn : " + weaponsConfigIn);
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "weaponsConfigOut : " + weaponsConfigOut);
            return QUtils.LoadFile(weaponsConfigIn);
        }

        //Parse the Objects.
        private static List<Weapon> ParseWeaponConfig(string qscData, bool advanceData = false)
        {
            //Remove all whitespaces.
            qscData = qscData.Replace("\t", String.Empty);
            var qscDataSplit = qscData.Split(new string[] { QUtils.taskNew }, StringSplitOptions.None);

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "started parsing.");

            var weaponList = new List<Weapon>();
            foreach (var data in qscDataSplit)
            {
                if (data.Contains(weaponCfgTask))
                {
                    var startIndex = data.IndexOf(',') + 1;
                    var endIndex = data.IndexOf(',', startIndex);
                    var taskName = data.Slice(startIndex, endIndex).Trim().Replace("\"", String.Empty);

                    if (taskName == weaponCfgTask)
                    {
                        Weapon weapon = new Weapon();

                        var taskNew = data.Split(',');
                        int taskIndex = 0;//Skip Task_New("Task_Id","TaskType", "Task_Note"

                        foreach (var task in taskNew)
                        {
                            //QLog.AddLog(MethodBase.GetCurrentMethod().Name, " taskName : " + taskName + " taskIndex " + taskIndex + " data  : " + task.Trim());

                            if (taskIndex == (int)WEAPONCFG.WEAPON_ID)
                                weapon.weaponId = Convert.ToInt32(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.SCRIPT_ID)
                                weapon.scriptId = task.Trim();

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_NAME)
                                weapon.weaponName = task.Trim();

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_MANUFACTURER)
                                weapon.manufacturer = task.Trim();

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_DESCRIPTION)
                                weapon.description = task.Trim();

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_TYPE_ENUM)
                                weapon.typeEnum = task.Trim();

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_CROSSHAIR_TYPE)
                                weapon.crosshairType = task.Trim();

                            else if (taskIndex == (int)WEAPONCFG.AMMO_DISPLAY_TYPE)
                                weapon.ammoDispType = task.Trim();

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_USERS)
                                weapon.weaponUsers = task.Trim();

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_MASS)
                                weapon.mass = Convert.ToInt32(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_CALIBER_ID)
                                weapon.caliberId = Convert.ToInt32(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_DAMAGE)
                                weapon.damage = Single.Parse(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_POWER)
                                weapon.power = Single.Parse(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_RELOAD_TIME)
                                weapon.reloadTime = Single.Parse(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_MUZZLE_VEL)
                                weapon.muzzleVelocity = Single.Parse(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_RANGE)
                                weapon.weaponRange = Single.Parse(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_BULLETS)
                                weapon.bullets = Convert.ToInt32(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_RPM)
                                weapon.roundsPerMinute = Convert.ToInt32(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_CLIPS)
                                weapon.roundsPerClip = Convert.ToInt32(task.Trim());

                            else if (taskIndex == (int)WEAPONCFG.WEAPON_BURST)
                                weapon.burst = Convert.ToInt32(task.Trim());

                            //Parse advance data such as sounds animations and models.
                            if (advanceData)
                            {
                                //Length and gun models and sounds.
                                if (taskIndex == (int)WEAPONCFG.WEAPON_LENGTH)
                                    weapon.weaponLength = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.BARREL_LENGTH)
                                    weapon.barrelLength = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_GUN_MODEL)
                                    weapon.gunModel = task.Trim();

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_CASING_MODEL)
                                    weapon.casingModel = task.Trim();

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_SOUND_SINGLE)
                                    weapon.soundSingle = task.Trim();

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_SOUND_LOOP)
                                    weapon.soundLoop = task.Trim();

                                //Speed & view change.
                                else if (taskIndex == (int)WEAPONCFG.WEAPON_MIN_RAND_SPEED)
                                    weapon.minRandSpeed = Single.Parse(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_MAX_RAND_SPEED)
                                    weapon.maxRandSpeed = Single.Parse(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_FIX_VIEW_CHANGE_X)
                                    weapon.fixViewX = Single.Parse(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_FIX_VIEW_CHANGE_Z)
                                    weapon.fixViewZ = Single.Parse(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_RAND_VIEW_CHANGE_X)
                                    weapon.randViewX = Single.Parse(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_RAND_VIEW_CHANGE_Z)
                                    weapon.randViewZ = Single.Parse(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_TYPE_STR)
                                    weapon.typeStr = task.Trim();

                                //Animations of fire reload/walk etc.
                                else if (taskIndex == (int)WEAPONCFG.WEAPON_FIRE_ANIM1)
                                    weapon.animFire1 = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_FIRE_ANIM2)
                                    weapon.animFire2 = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_FIRE_ANIM3)
                                    weapon.animFire3 = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_STAND_ANIM)
                                    weapon.animStand = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_MOVE_ANIM)
                                    weapon.animMove = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_RELOAD_ANIM)
                                    weapon.animReload = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_UPPERBODYWALK_ANIM)
                                    weapon.animUpperbodywalk = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_UPPERBODYSTAND_ANIM)
                                    weapon.animUpperbodystand = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_UPPERBODYRUN_ANIM)
                                    weapon.animUpperbodyrun = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_UPPERBODYRELOAD_ANIM)
                                    weapon.animUpperbodyreload = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_UPPERBODYFIRE_ANIM)
                                    weapon.animUpperbodyfire = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_UPPERBODYCROUCH_ANIM)
                                    weapon.animUpperbodycrouch = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_UPPERBODYCROUCHRUN_ANIM)
                                    weapon.animUpperbodycrouchrun = Convert.ToInt32(task.Trim());

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_DETECTION_RANGE)
                                    weapon.detectionRange = task.Trim();

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_PROJECTILE_TASK_TYPE)
                                    weapon.projectileTaskType = task.Trim();

                                else if (taskIndex == (int)WEAPONCFG.WEAPON_TASK_TYPE)
                                    weapon.weaponTaskType = task.Trim();

                                //Empty weapon selected.
                                else if (taskIndex == (int)WEAPONCFG.WEAPON_EMPTY_ON_CLEAR)
                                    weapon.emptyOnClear = Convert.ToBoolean(task.Replace(")", String.Empty).Trim());
                            }


                            taskIndex++;
                        }
                        weaponList.Add(weapon);
                    }
                }
            }
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "returned list with length :  " + weaponList.Count);
            return weaponList;
        }
    }

}
