using System;
using System.Reflection;

namespace IGIEditor
{
    enum BLENDTYPE
    {
        BLEND,
        BLEND_FILE,
        BLEND_SCRIPT,
    }

    enum SCRIPTTYPE
    {
        SCRIPT_0,
        SCRIPT_90,
        SCRIPT_180,
        SCRIPT_360,
        SCRIPT_CUSTOM,
    }

    class QBlender
    {
        private static string blendFile = "", blendModelsPath = @"Models\";
        private static string dataExtractorScript = "scripts\\dataExtractor.py", rotate_0Script = "scripts\\rotate_0.py", rotate_90Script = "scripts\\rotate_90.py", rotate_180Script = "scripts\\rotate_180.py", rotate_360Script = "scripts\\rotate_360.py", rotateCustomScript = "scripts\\rotateCustom.py";
        private static string runBlender = "blender";
        private static string runBlenderFile, runBlenderScript;
        private static string blenderDataFile = "blender_data.txt";

        internal static void RunBlender(int blendType, string blenderModel, int scriptType = 0)
        {
            string blendCmd = null, rotateScript = null;
            if (String.IsNullOrEmpty(blenderModel))
            {
                QLog.ShowError("Blender : run parameter file name is empty");
                return;
            }

            blendFile = blenderModel;
            var blendScript = dataExtractorScript;

            if (scriptType == (int)SCRIPTTYPE.SCRIPT_0) rotateScript = rotate_0Script;
            else if (scriptType == (int)SCRIPTTYPE.SCRIPT_90) rotateScript = rotate_90Script;
            else if (scriptType == (int)SCRIPTTYPE.SCRIPT_180) rotateScript = rotate_180Script;
            else if (scriptType == (int)SCRIPTTYPE.SCRIPT_360) rotateScript = rotate_360Script;
            else if (scriptType == (int)SCRIPTTYPE.SCRIPT_CUSTOM) blendScript = rotateCustomScript;

            //Append model path and blend/scripts files.
            runBlenderFile = "blender " + blendModelsPath + blendFile + " --python " + blendModelsPath + rotateScript;
            runBlenderScript = "blender " + blendModelsPath + blendFile + ((blendScript == dataExtractorScript) ? " --background" : "") + " --python " + blendModelsPath + blendScript;

            if (blendType == (int)BLENDTYPE.BLEND) blendCmd = runBlender;
            else if (blendType == (int)BLENDTYPE.BLEND_FILE)
            {
                blendCmd = runBlenderFile;
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "file cmd : " + runBlenderFile);
            }
            else if (blendType == (int)BLENDTYPE.BLEND_SCRIPT)
            {
                blendCmd = runBlenderScript;
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "script cmd : " + runBlenderScript);
            }

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with blend type : " + (blendType == 1 ? "FILE" : "SCRIPT") + " and model  : " + blenderModel);

            if (!String.IsNullOrEmpty(blendCmd))
                QUtils.ShellExec(blendCmd);
        }

        internal static T ParseBlenderData<T>(bool positionExtract, bool rotationExtract)
        {
            Real32 rotation = null;
            Real64 position = null;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with data PositionExtract : " + positionExtract.ToString() + " and RotationExtract  : " + rotationExtract.ToString());

            var blenderData = QUtils.LoadFile(blendModelsPath + blenderDataFile);
            if (string.IsNullOrEmpty(blenderData)) QLog.ShowError("Blender model data is empty", "Blender - Error");
            var blenderLine = blenderData.Replace("=", String.Empty).Split('\n');

            foreach (var data in blenderLine)
            {
                if (data.Contains("Position") && positionExtract)
                {
                    var subStrPos = data.Slice(data.IndexOf('(') + 1, data.IndexOf(')'));
                    var posData = subStrPos.Split(',');
                    position = new Real64();

                    //Parse the positions.
                    position.x = double.Parse(posData[0].Trim());
                    position.y = double.Parse(posData[1].Trim());
                    position.z = double.Parse(posData[2].Trim());

                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "PositionExtract data : x : " + position.x.ToString() + ", y : " + position.y.ToString() + ", z : " + position.z.ToString());
                }

                else if (data.Contains("Rotation") && rotationExtract)
                {
                    var subStrRot = data.Slice(data.IndexOf('(') + 1, data.IndexOf(')'));
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "subStr : " + subStrRot);
                    var posData = subStrRot.Split(',');
                    rotation = new Real32();

                    //Parse the rotation.
                    rotation.beta = float.Parse(posData[0].Trim().Replace("x", String.Empty));
                    rotation.alpha = float.Parse(posData[1].Trim().Replace("y", String.Empty));
                    rotation.gamma = float.Parse(posData[2].Trim().Replace("z", String.Empty));

                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "RotationExtract data : x : " + rotation.alpha.ToString() + ", y : " + rotation.beta.ToString() + ", z : " + rotation.gamma.ToString());

                }
            }


            object value = null;
            if (positionExtract) value = position; else value = rotation;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        internal static void LoadModelRotation(string blenderModel, Real32 rotation)
        {
            string rotateCustomScript =
                "import bpy\n" +
                 "obj = bpy.context.object\n" +
                 "obj.rotationEuler = (" + rotation.beta + "," + rotation.alpha + "," + rotation.gamma + ")";

            QUtils.SaveFile(blendModelsPath + QBlender.rotateCustomScript, rotateCustomScript);
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with data model : " + blenderModel + " and rotation alpha : " + rotation.alpha + ", beta : " + rotation.beta + ", gamma : " + rotation.gamma);

            RunBlender((int)BLENDTYPE.BLEND_SCRIPT, blenderModel, (int)SCRIPTTYPE.SCRIPT_CUSTOM);
        }

    }
}
