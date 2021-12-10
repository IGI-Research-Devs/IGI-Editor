﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IGIEditor
{
    internal class QCompiler
    {
        enum QTYPE
        {
            COMPILE,
            DECOMPILE,
        };
        private static string qpath;

        private string compileStart = "compile.bat";
        private string decompileStart = "decompile.bat";
        internal static string compilePath = QUtils.igiEditorQEdPath + "\\" + QUtils.qCompiler + @"\Compile";
        private string compileInputPath = QUtils.igiEditorQEdPath + "\\" + QUtils.qCompiler + @"\Compile\input";
        internal static string decompilePath = QUtils.igiEditorQEdPath + "\\" + QUtils.qCompiler + @"\Decompile";
        private string decompileInputPath = QUtils.igiEditorQEdPath + "\\" + QUtils.qCompiler + @"\Decompile\input";
        private string copyNoneErr = "0 File(s) copied";
        private string moveNoneErr = "0 File(s) moved";
        private string qappPath;

        internal QCompiler()
        {
            qappPath = Directory.GetCurrentDirectory();
            qpath = QUtils.appdataPath;

        }

        private static QCompiler GetQCompiler()
        {
            bool compilerExist = false;
            if (QUtils.externalCompiler) compilerExist = CheckQCompilerExist();
            return compilerExist ? new QCompiler() : null;
        }

        internal static bool CheckQCompilerExist()
        {
            var qCompilerPath = QUtils.igiEditorQEdPath + "\\" + QUtils.qCompiler;
            bool exist = Directory.Exists(qCompilerPath);
            if (!exist)
            {
                QUtils.ShowError("QCompiler external tool not found in system\nSwitching to Internal Compiler.", "Compiler Error.");
                return false;
            }
            return true;
        }


        private void QSetPath(string path)
        {
            //path = qpath + path;
            Directory.SetCurrentDirectory(path);
        }

        private string QGetAbsPath(string dirName)
        {
            return qpath + dirName;
        }

        private bool QCopy(List<string> files, QTYPE type)
        {
            bool status = true;
            string copyPath = (type == (int)QTYPE.COMPILE) ? (compileInputPath) : (decompileInputPath);
            foreach (var file in files)
            {
                string copyFile = "copy \"" + file + "\" \"" + copyPath + "\"";
                var shellOut = QUtils.ShellExec(copyFile);

                //Check for error in copy.
                if (shellOut.Contains(copyNoneErr))
                {
                    status = false;
                    break;
                }
            }
            return status;
        }

        private bool XCopy(string src, string dest)
        {
            bool status = true;
            string xcopyCmd = "xcopy " + src + dest + " /s /e /h /D";

            var shellOut = QUtils.ShellExec(xcopyCmd);

            //Check for error in copy.
            if (shellOut.Contains(copyNoneErr))
                status = false;
            return status;
        }

        private bool XMove(string src, string dest, QTYPE qtype)
        {
            bool status = true;
            string filter = "*.";

            if (qtype == QTYPE.COMPILE) filter = "*qvm";
            else if (qtype == QTYPE.DECOMPILE) filter = "*qsc";

            string xmoveCmd = "for /r \"" + src + "\" %x in (" + filter + ") do move /y \"%x\" \"" + dest + "\"";
            var shellOut = QUtils.ShellExec(xmoveCmd, true);

            //Check for error in move.
            if (shellOut.Contains(moveNoneErr))
                status = false;
            return status;
        }


        //Private Compilers -Internal (DLL Only.).

        private static bool CompileInternalFile(string qscFile, string gamePath)
        {
            bool status = false;
            QUtils.currGameLevel = QMemory.GetRunningLevel();
            string currLevelPath = "level" + QUtils.currGameLevel;
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QFile: '" + qscFile + "' Game Path: '" + gamePath + "'" + " CurrLevel path: " + currLevelPath);

            if (File.Exists(qscFile))
            {
                string scriptFile = "";
                string outScriptPath = gamePath + "\\" + qscFile;
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QFile : Output path '" + outScriptPath + "'");

                var qscData = QUtils.LoadFile(qscFile);

                if (!String.IsNullOrEmpty(qscData))
                {
                    //Compile for Humanplayer.
                    if (gamePath.Contains(QUtils.humanplayerPath))
                        scriptFile = "LOCAL:humanplayer/" + qscFile;

                    else if (gamePath.Contains("ai"))
                        scriptFile = "MISSION:AI/" + qscFile;

                    if (File.Exists(outScriptPath))
                    {
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QFile : File exist '" + outScriptPath + "' deleting file.");
                        QUtils.FileIODelete(outScriptPath);
                    }

                    //Copy file to OutPath and Compile with Internal Compiler.
                    QUtils.FileCopy(qscFile, outScriptPath);

                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QFile : Starting Compiling of file '" + scriptFile + "'");
                    QInternals.ScriptCompile(scriptFile);
                    status = true;
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QFile : Compiling of file '" + scriptFile + "' done\tOutput Script Path: '" + outScriptPath + "'");


                    if (status) IGIEditorUI.editorRef.SetStatusText("Compile success");
                }
                else QUtils.ShowError("Compile error data is empty.", "COMPILE ERROR");
            }
            else QUtils.ShowError("Compile error file not found.", "COMPILE ERROR");
            return status;
        }

        internal static bool CompileInternalData(string qscData, string gamePath, bool appendData = false, bool restartLevel = false, bool savePos = true)
        {
            bool status = false;
            try
            {
                QUtils.currGameLevel = QMemory.GetRunningLevel();
                string currLevelPath = "level" + QUtils.currGameLevel;
                if (!String.IsNullOrEmpty(qscData))
                {
                    string scriptFile = "MISSION:objects.qsc";
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QData : Script File :'" + scriptFile + "' Game Path: '" + gamePath + ",CurrLevel path: " + currLevelPath + "',Append Data: " + appendData + ",Restart Level: " + restartLevel + ",Save Position: " + savePos);

                    if (!gamePath.Contains(currLevelPath))
                    {
                        QUtils.ShowLogError(MethodBase.GetCurrentMethod().Name, "Compile error in game path for level #" + QUtils.currGameLevel);
                        return false;
                    }

                    //Compile for Objets.
                    QUtils.SaveFile(qscData, appendData);
                    QUtils.gamePath = QUtils.cfgGamePath + QMemory.GetRunningLevel();
                    string outScriptPath = QUtils.gamePath + "\\" + QUtils.objectsQsc;
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QData : Output Path: '" + outScriptPath + "'");

                    if (File.Exists(outScriptPath))
                    {
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QData : File exist '" + outScriptPath + "' deleting file.");
                        QUtils.FileIODelete(outScriptPath);
                    }

                    //Copy file to OutPath and Compile with Internal Compiler.
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QData : Starting Compiling of file '" + QUtils.objectsQsc + "'");
                    QUtils.FileCopy(QUtils.objectsQsc, outScriptPath);
                    QInternals.ScriptCompile(scriptFile);
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QData : Compiling of file '" + scriptFile + "' done\tOutput Path: '" + outScriptPath + "'");

                    QUtils.Sleep(1.5f);
                    //Delete script file after compiling.
                    QUtils.FileIODelete(outScriptPath);

                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "QData : Output Path: '" + outScriptPath + "' removed");

                    if (restartLevel) QMemory.RestartLevel(savePos);
                    status = true;
                }
            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
                status = false;
            }
            return status;
        }


        //Compiler External - QConv Tools.
        internal static bool CompileExternalFile(string qscFile, string qscPath)
        {
            bool status = false;
            try
            {
                if (!String.IsNullOrEmpty(qscFile))
                {
                    var qcompiler = GetQCompiler();
                    status = qcompiler.QCompile(new List<string>() { qscFile }, qscPath);
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return status;
        }

        internal static bool CompileExternalData(string qscData, string gamePath, bool appendData = false, bool restartLevel = false, bool savePos = true)
        {
            bool status = false;
            try
            {
                if (!String.IsNullOrEmpty(qscData))
                {
                    QUtils.SaveFile(qscData, appendData);
                    var qcompiler = GetQCompiler();
                    status = qcompiler.QCompile(new List<string>() { QUtils.objectsQsc }, gamePath);

                    if (status)
                        if (restartLevel) QMemory.RestartLevel(savePos);
                }
            }

            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return status;
        }


        public bool QCompile(List<string> qscFiles, string outputPath)
        {
            bool status = true;
            try
            {
                status = QCopy(qscFiles, QTYPE.COMPILE);

                if (!status)
                    QUtils.ShowError("Error occurred while copying files");

                //Change directory to compile directory.
                QSetPath(compilePath);

                //Start compile command.
                string shellOut = QUtils.ShellExec(compileStart);
                if (shellOut.Contains("Error") || shellOut.Contains("importModule") || shellOut.Contains("ModuleNotFoundError") || shellOut.Contains("Converted: 0"))
                {
                    QUtils.ShowError("Error in compiling input files");
                    QSetPath(QUtils.editorCurrPath);
                    return false;
                }

                var currDir = Directory.GetCurrentDirectory();
                if (Directory.Exists(currDir))
                {
                    bool moveStatus = XMove("output", outputPath, QTYPE.COMPILE);
                    if (!moveStatus)
                        QUtils.ShowError("Error while moving data to Output path");
                }
                else
                {
                    QUtils.ShowError("Path '" + currDir + "' does not exist!");
                }

            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            Directory.SetCurrentDirectory(qappPath);
            return status;
        }

        internal static bool Compile(string qscFile, string gamePath, int _ignore)
        {
            bool status = false;
            if (QUtils.internalCompiler)
                status = CompileInternalFile(qscFile, gamePath);
            else if (QUtils.externalCompiler)
                status = CompileExternalFile(qscFile, gamePath);
            return status;
        }

        internal static bool Compile(string qscData, string gamePath, bool appendData = false, bool restartLevel = false, bool savePos = true)
        {
            bool status = false;
            if (QUtils.internalCompiler)
                status = CompileInternalData(qscData, gamePath, appendData, restartLevel, savePos);
            else if (QUtils.externalCompiler)
                status = CompileExternalData(qscData, gamePath, appendData, restartLevel, savePos);
            return status;
        }

        internal static bool CompileEx(string qscData)
        {
            return Compile(qscData, QUtils.gamePath, false, true, true);
        }
    }
}
