/*
 * Copyright 2021(c) Samsung Electronics Co., Ltd  All Rights Reserved.
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
using System.IO;
using Process = System.Diagnostics.Process;
using Microsoft.VisualStudio.Shell;
using System.Xml;
using Tizen.VisualStudio.Tools.Data;
using System.ComponentModel.Design;
using EnvDTE80;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Tizen.VisualStudio.Tidl
{
    internal sealed class TidlCommandOption
    {
        public static readonly Guid guidTidlCommand = 
            new Guid("587769ce-c5bb-42ea-b2f2-7df8af1d817e");

        public const int CmdIdMenuItemTidlCmdSet = 0x3001;

        private readonly VsPackage package;
        private static TidlCommandOption instance;

        private string apiversion;

        internal TidlData tidlData;

        private static void PrintLogs(string msg)
        {
            VsPackage.outputPaneTizen?.Activate();
            VsPackage.outputPaneTizen?.OutputStringThreadSafe(msg);
        }

        public static void Initialize(VsPackage package)
        {
            instance = new TidlCommandOption(package);
            instance.RegisterHandlers();
        }

        private IServiceProvider ServiceProvider => this.package as IServiceProvider;

        private void RegisterHandlers()
        {
            if (this.ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                CommandID cmdId;
                OleMenuCommand mItem;

                cmdId = new CommandID(guidTidlCommand, CmdIdMenuItemTidlCmdSet);
                mItem = new OleMenuCommand(HandleMenuItemTidlCompile, cmdId);

                // Add an event handler to BeforeQueryStatus if one was passed in
                mItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(mItem);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            DTE2 dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));

            UIHierarchy uih = dte2.ToolWindows.SolutionExplorer;
            Array selectedItems = (Array)uih.SelectedItems;

            if (selectedItems != null)
            {
                //Multiple selection not allowed.
                if (selectedItems.Length == 0 || selectedItems.Length > 1)
                {
                    command.Visible = false;
                    return;
                }

                UIHierarchyItem selItem = (UIHierarchyItem)selectedItems.GetValue(0);
                if (!(selItem.Object is ProjectItem prjItem))
                {
                    command.Visible = false;
                    return;
                }
                string fileName = prjItem.Name;
                if (fileName.EndsWith(".tidl"))
                {
                    //Activate command
                    command.Visible = true;

                    //Populate tidl data
                    tidlData.InputFilePath = prjItem.Properties.Item("FullPath").Value.ToString();
                    tidlData.OutputInterface = Path.GetFileNameWithoutExtension(fileName);
                    tidlData.ProjectPath = Path.GetDirectoryName(prjItem.ContainingProject.FullName);
                    tidlData.SolutionPath = Path.GetDirectoryName(prjItem.ContainingProject.DTE.Solution.FullName);
                }
                else
                {
                    command.Visible = false;
                }
            }
        }

        private void HandleMenuItemTidlCompile(object sender, EventArgs e)
        {
            //Activate Output Window
            DTE2 dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            OutputWindow outputWin = dte2.ToolWindows.OutputWindow;
            if (outputWin.Parent != dte2.ActiveWindow)
            {
                outputWin.Parent.Activate();
            }

            /* Launch Tidl Compilation method asynchronously, 
             * reading the optional arguments from the Tidl option
             * page and posting the messages to the output 
             * window. The completition message is finally posted to 
             * the Statusbar as well.
             */
            _ = Task.Run(() => LaunchTidlCompilation());
        }

        private bool ReadApiVersion()
        {
            apiversion = "";
            bool found = false;

            string projPath = tidlData.ProjectPath;
            string workspacePath = tidlData.SolutionPath;
            if (string.IsNullOrEmpty(projPath) || string.IsNullOrEmpty(workspacePath))
            {
                PrintLogs("path is null");
                return false;
            }

            string manifestPath = Path.Combine(projPath, "tizen-manifest.xml");
            if (File.Exists(manifestPath))
            {
                XmlDocument XDoc = new XmlDocument();
                XDoc.Load(manifestPath);
                XmlNodeList nodes = XDoc.GetElementsByTagName("manifest");
                if (nodes.Count > 0)
                {
                    XmlAttributeCollection attribute = nodes[0].Attributes;
                    for (int ii = 0; ii < attribute.Count; ++ii)
                    {
                        string name = attribute[ii].Name;
                        if (name == "api-version")
                        {
                            apiversion = attribute[ii].Value;
                            found = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                //check for tizen_workspace.yaml
                apiversion = VsProjectHelper.GetInstance.getTag(workspacePath, "api_version").Replace('"', ' ').Trim();

                if (string.IsNullOrEmpty(apiversion))
                {
                    PrintLogs("Couldn't read api_version from yaml");
                    return false;
                }

                found = true;
            }
            return found;
        }

        private async Task LaunchTidlCompilation()
        {
            PrintLogs($"{DateTime.Now} : Begin Tidl Compilation. \n");
            using (Process process = new Process())
            {
                try
                {
                    if (false == ReadApiVersion())
                    {
                        throw new ArgumentException("Unable to read the Tizen api-version.");
                    }

                    //api-version conversion and rounding
                    double val = double.Parse(apiversion);
                    string api_version = string.Format("{0:0.0}", val);

                    string inputPathFolder = Path.GetDirectoryName(tidlData.InputFilePath);
                    string fileName = Path.GetFileName(tidlData.InputFilePath);
                    string pltf = "\\platforms\\tizen-" + api_version;
                    string rem = "\\common\\tidl\\tidlc.exe";
                    string tidlcCommand = ToolsPathInfo.ToolsRootPath + pltf + rem;
                    string arguments = "/q /c pushd " + inputPathFolder + " && " + tidlcCommand;

                    //mandatory arguments
                    arguments += " -i " + fileName;
                    arguments += " -o " + tidlData.OutputInterface;
                    arguments += " -l " + TidlInfo.LanguageOption;
                    arguments += " ";

                    //optional arguments
                    if (TidlInfo.ProxyVal != false)
                    {
                        arguments += "-p ";
                    }

                    if (TidlInfo.StubVal != false)
                    {
                        arguments += "-s ";
                    }

                    if (TidlInfo.RpcVal != false)
                    {
                        arguments += "-r ";
                    }

                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    PrintLogs($"cmd.exe {process.StartInfo.Arguments} \n");

                    //Process Start
                    process.Start();

                    string outputStr = process.StandardOutput.ReadToEnd();
                    string errorStr = process.StandardError.ReadToEnd();

                    IVsStatusbar statusbar = (IVsStatusbar)Package.GetGlobalService(typeof(SVsStatusbar));

                    //awaiting process WaitForExit.
                    await Task.Run(() =>
                    {
                        process.WaitForExit();
                    });

                    if (string.IsNullOrEmpty(outputStr) == false)
                    {
                        PrintLogs($"{outputStr} \n");
                    }
                    if (string.IsNullOrEmpty(errorStr) == false)
                    {
                        PrintLogs($"{errorStr} \n");
                    }
                    statusbar.SetText("Tidl Compilation Done");
                }
                catch (Exception excp)
                {
                    PrintLogs($"{excp.Message} \n");
                }
                PrintLogs($"{DateTime.Now} : Tidl Compilation Done. \n");
            }
        }

        private TidlCommandOption(VsPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            tidlData = new TidlData();
        }
    }
}
