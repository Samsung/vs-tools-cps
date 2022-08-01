/*
 * Copyright 2017 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * 	http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Tizen.VisualStudio.Tools.DebugBridge;
using Process = System.Diagnostics.Process;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Tizen.VisualStudio.ProjectSystem.VS.Debug
{

    public static class HotReloadLaunchProvider
    {
        private static bool HotReloadStarted = false;
        private static readonly string[] _xamlFileExtensions = { ".xaml", "xaml.cs" };
        private static readonly string[] _dotnetFileExtensions = { ".cs" };
        private static readonly object _locker = new object();
        private static SDBDeviceInfo mDevice;
        private static string AppId;
        private static bool IsXamlProject;
        private static string tempFile;
        private static Solution solution;
        private static string deltaDir;
        private static WatchHotReloadService watchHotReloadService;

        private static Regex rx = new Regex(@"\[global::System.CodeDom.Compiler.GeneratedCodeAttribute.*\]", RegexOptions.Compiled);

        private static readonly HashSet<string> _addresses = new HashSet<string>();

        private static void PrintLogs(string msg)
        {
            VsPackage.outputPaneTizen?.Activate();
            VsPackage.outputPaneTizen?.OutputStringThreadSafe(msg);
        }

        public static void StopHotReloadObserver()
        {
            PrintLogs("Inside stop HotReload observer \n");
            if (HotReloadStarted)
            {
                HotReloadStarted = false;
                mDevice = null;
                AppId = null;

                if (!IsXamlProject)
                {
                    /* End the Dotnet HotReloadService Session */
                    watchHotReloadService.EndSession();

                    if (Directory.Exists(deltaDir))
                        Directory.Delete(deltaDir, true);
                }
            }
            PrintLogs($"flag observer value is : {HotReloadStarted} \n");
        }

        public static void SetIsXamlProject(bool value)
        {
            PrintLogs($"SetIsXamlProject : {value} \n");

            IsXamlProject = value;

        }

        public static bool GetIsXamlProject()
        {
            PrintLogs($"isXamlProject : {IsXamlProject} \n");
            return IsXamlProject;

        }

        public static bool IsHotreloadStarted()
        {
            return HotReloadStarted;
        }

        public static async Task StartHotReloadObserverAsync(SDBDeviceInfo device, string appId, string projPath)
        {
            PrintLogs("Inside StartHotReloadObserver \n");

            if (HotReloadStarted)
            {
                PrintLogs("Inside Observer already running, exit \n");
                return;
            }

            mDevice = device;
            AppId = appId;
            HotReloadStarted = true;

            tempFile = Path.Combine(Path.GetTempPath(), "hotreload");

            if (!IsXamlProject)
            {
                // Create a temp dir to store delta files
                var projDir = Path.GetDirectoryName(projPath);
                deltaDir = Path.Combine(projDir, "deltas");
                if (!Directory.Exists(deltaDir))
                {
                    Directory.CreateDirectory(deltaDir);
                }else
                {
                    Directory.Delete(deltaDir, true);
                    Directory.CreateDirectory(deltaDir);
                }

                var workspace = MSBuildWorkspace.Create();
                var prj = await workspace.OpenProjectAsync(projPath, cancellationToken: CancellationToken.None);
                solution = prj.Solution;

                /* Create the WatchHotReloadService Instance
                 * for supporting Dotnet Hotreload(EnC). */
                watchHotReloadService = new WatchHotReloadService(solution.Workspace.Services);

                /* Start the Dotnet HotReloadService Session
                 * for supporting Dotnet Hotreload(EnC). */
                await watchHotReloadService.StartSessionAsync(solution, CancellationToken.None);
            }
        }

        /// <summary>
        /// Calculate deltas use Roslyn
        /// </summary>
        /// <param name="solution">Current solution</param>
        /// <param name="sources"> Map of sources</param>
        /// <param name="filePath"> Path to current solution</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"> Incorrect source file</exception>
        private static List<Update> GetDeltas(Solution solution, Dictionary<string, bool> sources, string filePath, string filename)
        {
            var deltas = new List<Update>();

            for (int i = 0; i < sources.Count; i++)
            {
                DocumentId documentId = default;
                if (!sources.ElementAt(i).Value)
                {
                    foreach (var projectId in solution.ProjectIds)
                    {
                        var project = solution.GetProject(projectId);
                        foreach (var id in project.DocumentIds)
                        {
                            Document document = project.GetDocument(id);
                            if (document.Name != Path.GetFileName(filename))
                                continue;
                            documentId = id;
                        }
                    }
                }
                else
                {
                    documentId = DocumentId.CreateNewId(solution.Projects.First().Id);
                    var classPath = Path.GetFullPath(Path.Combine(filePath, "A.cs"));
                    solution = solution.AddDocument(DocumentInfo.Create(
                                                    id: documentId,
                                                    name: "A.cs",
                                                    loader: new FileTextLoader(classPath, Encoding.UTF8),
                                                    filePath: classPath));
                }

                solution = solution.WithDocumentText(documentId, SourceText.From(sources.ElementAt(i).Key, Encoding.UTF8));
                var (updates, hotReloadDiagnostics) = watchHotReloadService.EmitSolutionUpdate(solution, CancellationToken.None);
                if (!hotReloadDiagnostics.IsDefaultOrEmpty)
                {
                    foreach (var diagnostic in hotReloadDiagnostics)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }
                    throw new InvalidDataException($"Changes made in project will not be applied while the application is running,\n please change the source file #{i} in sources");
                }
                deltas.Add(updates);
            }
            return deltas;
        }

        /// <summary>
        /// Write deltas to files
        /// </summary>
        /// <param name="delta">Byte array of delta</param>
        /// <param name="filename">Name of file</param>
        private static void WriteDeltaToFile(byte[] delta, string filename)
        {
            using (FileStream file = new FileStream(filename, FileMode.Create, System.IO.FileAccess.Write))
            {
                file.Write(delta, 0, delta.Length);
            }
        }

        public static void OnFileChanged(string filePath, string rootDir)
        {
            lock (_locker)
            {
                if (IsXamlProject)
                {
                    if (_xamlFileExtensions.All(fileExt => !filePath.EndsWith(fileExt, StringComparison.OrdinalIgnoreCase)))
                    {
                        return;
                    }
                }
                else
                {
                    if (_dotnetFileExtensions.All(fileExt => !filePath.EndsWith(fileExt, StringComparison.OrdinalIgnoreCase)))
                    {
                        return;
                    }
                }
            }


            var hotreloadBlock = new ActionBlock<Tuple<string, string>>((info) =>
            {
                var fullName = info.Item1;
                var root = info.Item2;
                try
                {
                    if (fullName.EndsWith(".xaml", true, null))
                    {
                        var fileName = Path.GetFileName(fullName);
                        var xamlcs = Directory.GetFiles(root, $"{fileName}.cs", SearchOption.AllDirectories).FirstOrDefault();
                        var xamlgcs = Directory.GetFiles(root, $"{fileName}.g.cs", SearchOption.AllDirectories).OrderBy(p => File.GetLastWriteTime(p)).FirstOrDefault();
                        Hotreload(filePath, xamlcs, xamlgcs);
                    }
                    else if (fullName.EndsWith(".xaml.cs", true, null))
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullName);
                        var xaml = Directory.GetFiles(root, $"{fileNameWithoutExt}", SearchOption.AllDirectories).FirstOrDefault();
                        var xamlgcs = Directory.GetFiles(root, $"{fileNameWithoutExt}.g.cs", SearchOption.AllDirectories).OrderBy(p => File.GetLastWriteTime(p)).FirstOrDefault();
                        Hotreload(xaml, filePath, xamlgcs);
                    }
                    else if (fullName.EndsWith(".cs", true, null))
                    {
                        string source1 = File.ReadAllText(fullName);
                        //if you want to add a new document to the project set to true otherwise false
                        Dictionary<string, bool> sources = new Dictionary<string, bool>()
                        {
                            { source1, false  },
                        };

                        var deltas = GetDeltas(solution, sources, root, fullName);
                        DirectoryInfo di = new DirectoryInfo(deltaDir);
                        string deltaFiles = string.Empty;

                        // Delete previous delta files from deltaDir
                        foreach (FileInfo file in di.EnumerateFiles())
                        {
                            file.Delete();
                        }

                        for (int i = 0; i < deltas.Count; i++)
                        {
                            Update delta = deltas[i];
                            WriteDeltaToFile(delta.MetadataDelta.ToArray(), deltaDir + $"\\tmp_metadata{i}.metadata");
                            WriteDeltaToFile(delta.ILDelta.ToArray(), deltaDir + $"\\tmp_il{i}.il");
                            WriteDeltaToFile(delta.PdbDelta.ToArray(), deltaDir + $"\\tmp_pdb{i}.pdb");
                        }

                        // Push Delta files to Target device/emulator
                        const string hotreloadDirectory = "/home/owner/share/tmp/sdk_tools/.dotnethotreload";
                        SendFile(deltaDir, hotreloadDirectory);

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                CancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token,
                BoundedCapacity = 1000
            });
            hotreloadBlock.Post(new Tuple<string, string>(filePath, rootDir));
        }

        private static void Hotreload(string file, string xamlcs = null, string xamlgcs = null)
        {
            using (var sw = new StreamWriter(tempFile))
            {
                sw.WriteLine(File.ReadAllText(file));
                sw.WriteLine("// END XAML");

                if (xamlgcs != null)
                {
                    sw.WriteLine(rx.Replace(File.ReadAllText(xamlgcs), ""));
                    sw.WriteLine($"// SOURCE : {Path.GetFileName(xamlgcs)}");
                }

                if (xamlcs == null)
                {
                    xamlcs = $"{file}.cs";
                }
                sw.WriteLine(File.ReadAllText(xamlcs));
                sw.WriteLine($"// SOURCE : {Path.GetFileName(xamlcs)}");
            }
            SendFile(tempFile, "/home/owner/share/tmp/sdk_tools/.hotreload");
        }

        private static void SendFile(string filePath, string target)
        {
            VsPackage.outputPaneTizen?.OutputStringThreadSafe($"file changed :  {filePath} \n");
            string command = $"-s {mDevice.Serial} push {filePath} {target}/";

            PrintLogs($"file push command  :  {command} \n");

            var processInfo = new ProcessStartInfo
            {
                FileName = SDBLib.GetSdbFilePath(),
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = processInfo };
            process.Start();
            process.WaitForExit();
        }
    }
}