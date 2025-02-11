using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace IGIEditor
{

    class FileIntegrity
    {
        private static readonly string qChecksFile = Path.Combine(QUtils.igiEditorQEdPath, "QChecks.dat");
        private static string qChecksFileData = null;
        private static bool checksumEnabled = false;
        private static readonly Dictionary<string, string> hashCache = new Dictionary<string, string>();
        private static readonly object hashLock = new object();

        public static void EnableChecksum(bool enable)
        {
            checksumEnabled = enable;
            QUtils.AddLog(MethodBase.GetCurrentMethod().Name, 
                $"Checksum validation {(enable ? "enabled" : "disabled")}, QChecks file: {qChecksFile}");
        }

        public static void RunFileIntegrityCheck(string processName = null, List<string> gameDirs = null)
        {
            try {
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, 
                    $"Running integrity check. Checksum enabled: {checksumEnabled}, Process: {processName ?? "null"}");
                
                if (!checksumEnabled) {
                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Checksum validation is disabled, skipping integrity check");
                    return;
                }

                if (!File.Exists(qChecksFile))
                {
                    GenerateFileHash(qChecksFile);
                }

                if (String.IsNullOrEmpty(qChecksFileData))
                {
                    qChecksFileData = QCryptor.Decrypt(qChecksFile);
                }

                var filesToCheck = GetFilesToCheck(processName, gameDirs);
                foreach (var file in filesToCheck)
                {
                    if (!CheckFileIntegrity(file, false))
                    {
                        QUtils.ShowError($"File integrity check failed for: {file}");
                    }
                }
            }
            catch (Exception ex) {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, 
                    new Exception("Failed integrity check", ex));
            }
        }

        private static List<string> GetFilesToCheck(string processName, List<string> gameDirs)
        {
            var files = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(processName))
                {
                    var processPath = Process.GetProcessesByName(processName)
                        .FirstOrDefault()?.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(processPath))
                    {
                        files.Add(processPath);
                    }
                }

                if (gameDirs != null && gameDirs.Any())
                {
                    foreach (var dir in gameDirs)
                    {
                        if (Directory.Exists(dir))
                        {
                            files.AddRange(Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, 
                    new Exception("Failed to get files for integrity check", ex));
            }
            return files;
        }

        public static bool CheckFileIntegrity(string qfilePath, bool showError = true)
        {
            try
            {
                if (!checksumEnabled)
                {
                    return true;
                }

                if (!File.Exists(qfilePath))
                {
                    if (showError) QUtils.ShowError($"File not found: {qfilePath}");
                    return false;
                }

                var parentDir = Path.GetDirectoryName(qfilePath);
                var filePath = Path.Combine(
                    parentDir.Substring(parentDir.LastIndexOf(Path.DirectorySeparatorChar) + 1), 
                    Path.GetFileName(qfilePath));

                if (!File.Exists(qChecksFile))
                {
                    GenerateFileHash(qfilePath);
                    return true;
                }

                if (String.IsNullOrEmpty(qChecksFileData))
                {
                    qChecksFileData = QCryptor.Decrypt(qChecksFile);
                }

                string md5Hash = GenerateMD5(qfilePath);
                if (string.IsNullOrEmpty(md5Hash) || qChecksFileData.Length < 5)
                {
                    if (showError)
                    {
                        QUtils.ShowError("File integrity hashes generation error");
                    }
                    return false;
                }

                var fileHashesData = qChecksFileData.Split('\n');
                bool fileMatch = false;

                foreach (var hashLine in fileHashesData)
                {
                    if (hashLine.Contains(filePath) && hashLine.Contains(md5Hash))
                    {
                        fileMatch = true;
                        break;
                    }
                }

                if (!fileMatch && showError)
                {
                    QUtils.ShowError($"File integrity check failed for: {filePath}");
                }

                return fileMatch;
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, 
                    new Exception($"Failed to check integrity for file: {qfilePath}", ex));
                return false;
            }
        }

        public static bool CheckDirIntegrity(List<string> dirNames, HashSet<string> excludeList, bool showError = true)
        {
            try
            {
                if (!checksumEnabled)
                {
                    return true;
                }

                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, 
                    $"Checking directory integrity. Directories: {string.Join(", ", dirNames)}");

                bool allValid = true;
                foreach (var dir in dirNames)
                {
                    if (!Directory.Exists(dir))
                    {
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Directory not found: {dir}");
                        continue;
                    }

                    var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                        .Where(f => !excludeList.Contains(Path.GetFileName(f)));

                    foreach (var file in files)
                    {
                        if (!CheckFileIntegrity(file, showError))
                        {
                            allValid = false;
                        }
                    }
                }

                return allValid;
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, 
                    new Exception("Failed to check directory integrity", ex));
                return false;
            }
        }

        private static void GenerateFileHash(string fileName, bool append = false)
        {
            try
            {
                var hash = GenerateMD5(fileName);
                if (!string.IsNullOrEmpty(hash))
                {
                    string hashLine = $"{fileName}={hash}\n";
                    File.AppendAllText(qChecksFile, hashLine);
                    lock (hashLock)
                    {
                        hashCache[fileName] = hash;
                    }
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public static async Task GenerateDirHashes(List<string> dirNames)
        {
            try
            {
                QUtils.AddLog(MethodBase.GetCurrentMethod().Name, 
                    $"Generating hashes for directories: {string.Join(", ", dirNames)}");

                foreach (var dir in dirNames)
                {
                    if (!Directory.Exists(dir))
                    {
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, $"Directory not found: {dir}");
                        continue;
                    }

                    var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        await Task.Run(() => GenerateFileHash(file, true));
                    }
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, 
                    new Exception("Failed to generate directory hashes", ex));
            }
        }

        private static string GenerateMD5(string fileName)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    return string.Empty;
                }

                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(fileName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
                return string.Empty;
            }
        }
    }
}
