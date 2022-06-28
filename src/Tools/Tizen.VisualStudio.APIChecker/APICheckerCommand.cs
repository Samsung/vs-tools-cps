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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Tizen.VisualStudio.Utilities;
using NetCore.Profiler.Extension.VSPackage;

namespace Tizen.VisualStudio.APIChecker
{
    /// <summary>
    /// Command handler
    /// </summary>
    public sealed class APICheckerCommand
    {

        private static IVsOutputWindowPane outpane;

        private Dictionary<ISymbol, string> symbolMap;

        private Analyzer analyzer;
        private static long startTime = 0, endTime = 0, usage = 0;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0111;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d9ed5c08-a5df-4e13-a326-dbc6f2f2d2eb");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;


        /// <summary>
        /// Initializes a new instance of the <see cref="APIChecker"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private APICheckerCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static APICheckerCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="outwindowPane">OutwindowPane</param>
        public static void Initialize(Package package, IVsOutputWindowPane outwindowPane)
        {
            Instance = new APICheckerCommand(package);
            outpane = outwindowPane;
            WebPrivilegeChecker.Initialize(package, outwindowPane);
        }

        public void RunBuildTime()
        {
            RunAPIChecker();
        }
        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        public void MenuItemCallback(object sender, EventArgs e)
        {
            VsProjectHelper projHelp = VsProjectHelper.Instance;
            bool isWebProj = projHelp.IsTizenWebProject();
            if (isWebProj)
            {
                WebPrivilegeChecker.Instance?.HandleMenuItemPrivilegeCheck(sender, e);
            }
            else
            {
                RunAPIChecker();
            }
        }

        private void RunAPIChecker()
        {
            startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000;
            RemoteLogger.logAccess("Tizen.VSWin.API Checker");

            outpane.Clear();
            outpane.OutputString("===================Running API and Privilege Checker================ \n");

            APICheckerWindowTaskProvider taskWindow = APICheckerWindowTaskProvider.CreateProvider(this.ServiceProvider);
            taskWindow.ClearError();

            symbolMap = new Dictionary<ISymbol, string>();

            var componentModel = (IComponentModel)this.ServiceProvider.GetService(typeof(SComponentModel));
            var workspace = componentModel?.GetService<Microsoft.VisualStudio.LanguageServices.VisualStudioWorkspace>();
            var soln = workspace.CurrentSolution;
            if (soln == null)
            {
                outpane.OutputString("Select a solution\n");
                return;
            }

            ProjectDependencyGraph projectGraph = soln.GetProjectDependencyGraph();

            foreach (ProjectId projectId in projectGraph.GetTopologicallySortedProjects())
            {
                Project proj = soln.GetProject(projectId);
                List<string> privilegeList = new List<string>();
                List<string> featureList = new List<string>();
                string apiversion = "";
                var projFile = proj.FilePath;
                if (projFile == null) // TODO create issue on VS and remove the check
                    continue;
                var projPath = projFile.Substring(0, projFile.LastIndexOf("\\") + 1);
                var manifestPath = projPath + "tizen-manifest.xml";

                if (File.Exists(manifestPath))
                {
                    XmlDocument XDoc = new XmlDocument();
                    XDoc.Load(manifestPath);
                    XmlNodeList nodes = XDoc.GetElementsByTagName("privilege");
                    foreach (XmlNode node in nodes)
                    {
                        privilegeList.Add(node.InnerText);
                    }

                    nodes = XDoc.GetElementsByTagName("feature");
                    foreach (XmlNode node in nodes)
                    {
                        if (node.InnerText == "true")
                        {
                            XmlAttributeCollection attr = node.Attributes;
                            for (int ii = 0; ii < attr.Count; ++ii)
                            {
                                string name = attr[ii].Name;
                                if (name == "name")
                                {
                                    featureList.Add(attr[ii].Value);
                                    break;
                                }
                            }
                        }
                    }

                    nodes = XDoc.GetElementsByTagName("manifest");
                    if (nodes.Count > 0)
                    {
                        XmlAttributeCollection attribute = nodes[0].Attributes;
                        for (int ii = 0; ii < attribute.Count; ++ii)
                        {
                            string name = attribute[ii].Name;
                            if (name == "api-version")
                            {
                                apiversion = attribute[ii].Value;
                                break;
                            }
                        }
                    }
                }

                if (apiversion == "")
                {
                    continue;
                }

                //Create a new Analyzer for this project.
                analyzer = new Analyzer(apiversion, privilegeList, manifestPath, this.ServiceProvider, featureList);

                //Get Compilation
                Compilation projectCompilation = proj.GetCompilationAsync().Result;
                if (null == projectCompilation || string.IsNullOrEmpty(projectCompilation.AssemblyName))
                {
                    continue;
                }

                //Run Analysis on each file in this project
                foreach (var syntaxTree in projectCompilation.SyntaxTrees)
                {
                    SemanticModel model = projectCompilation.GetSemanticModel(syntaxTree);
                    RunAnalysis(model, syntaxTree, apiversion, privilegeList);
                }

                analyzer.ReportUnusedPrivilegesAndFeature();
            }

            outpane.OutputString("===================API and Privilege Completed================ \n");
            endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000;
            usage = endTime - startTime;
            RemoteLogger.logUsage("Tizen.VSWin.API Checker", usage);
        }

        // Run Analysis on the called SyntaxTree with the SemanticModel
        private void RunAnalysis(SemanticModel semaModel, SyntaxTree syntaxTree, string apiversion, List<string> privilegeList)
        {
            string fileName = syntaxTree.FilePath;
            outpane.OutputString("............Analyzing File: " + fileName + "............\n");

            SymbolInfo symbInfo = semaModel.GetSymbolInfo(syntaxTree.GetRoot());
            var root = syntaxTree.GetRoot();
            char[] delim = { ':', '.' };
            List<ISymbol> invokationSymbols = new List<ISymbol>();
            List<SyntaxNode> nodes = new List<SyntaxNode>();

            //TODO: Handle calls to new
            foreach (SyntaxNode node in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var symbol = semaModel.GetSymbolInfo(node).Symbol;
                if (symbol == null)
                {
                    continue;
                }

                var id = symbol.GetDocumentationCommentId();
                if (id == null)
                {
                    continue;
                }

                var name = id.Split(delim);
                if (name.Length <= 1 || name[1].Equals("Tizen") == false)
                {
                    continue;
                }

                invokationSymbols.Add(symbol);
                nodes.Add(node);
            }

            char[] spandelim = { '(', ',', '-', ')', ' ' };

            for (var i = 0; i < invokationSymbols.Count; ++i)
            {
                ISymbol symbol = invokationSymbols[i];
                SyntaxNode node = nodes[i];
                string span = node.GetLocation().GetLineSpan().ToString();
                span = span.Substring(span.LastIndexOf(':') + 1);
                string[] lineInfo = span.Split(spandelim);
                lineInfo = lineInfo.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                var id = symbol.GetDocumentationCommentId();
                string comment;
                //Cache already read comment info
                if (symbolMap.ContainsKey(symbol))
                {
                    bool res = symbolMap.TryGetValue(symbol, out comment);
                    if (!res)
                    {
                        break;
                    }

                    analyzer.AnalyzeAPI(symbol.ToString(), comment, lineInfo, fileName);
                    continue;
                }

                //TODO: Optimize this.
                comment = symbol.GetDocumentationCommentXml();
                symbolMap.Add(symbol, comment);
                analyzer.AnalyzeAPI(symbol.ToString(), comment, lineInfo, fileName);
            }

            outpane.OutputString("............Analysis completed for file: " + fileName + "............\n");
        }

    }
}
