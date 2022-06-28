/*
 * Copyright 2021 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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

using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.ProjectWizard
{
    [ComVisible(true)]
    public sealed class TizenPageContentLoader
    {
        public string ProjectType { get; set; }
        public string ProfileName { get; set; }
        public string TemplateName { get; set; }
        public string ProjectName { get; set; }
        public string WorkspacePath { get; set; }

        private static readonly string[] _projectsArr = { "native", "web" };
        private readonly WebBrowser _webBrowserRef;
        private readonly Window _projectWizardCreateWizard;
        private readonly Dictionary<string, Dictionary<string, List<string>>> _templateListDictionary;
        private readonly Dictionary<string, string> _navigationHistory;
        public TizenPageContentLoader(Window win, WebBrowser obj)
        {
            _projectWizardCreateWizard = win;
            _webBrowserRef = obj;
            _webBrowserRef.LoadCompleted += PageLoadCompleted;
            _webBrowserRef.ObjectForScripting = this;
            _navigationHistory = new Dictionary<string, string>();
            _templateListDictionary = VsProjectHelper.GetInstance.TemplateListDictionary;
            Transit("Command:Select,Back:,Text:");
        }

        private string GetWebviewContentFinish(string backCommand, string text)
        { 
            //type, profile and selected template app
            (string type, string profile, string template) = (text.Split(';')[0], text.Split(';')[1], text.Split(';')[2]);
            string profileClass = profile.Split('-')[0];

            TemplateName = template;

            var dirPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string pathstr = Path.Combine(ToolsPathInfo.TizenCorePath, @"resources", type, profileClass, template, "screenshot.png");
            string dir = Path.GetDirectoryName(pathstr);
            Uri imageNormal;
            if (!Directory.Exists(dir) || !File.Exists(pathstr))
            {
                imageNormal = new Uri(Path.Combine(dirPath, @"images", "tizen_app_icon.png"));
            }
            else
            {
                imageNormal = new Uri(pathstr);
            }

            string imageStr = "<img src=\"" + imageNormal + "\"/>" + '\n';
            return string.Format(@"<!DOCTYPE HTML>
            <html>
            <head>
            <meta charset=""UTF-8"" http-equiv=""X-UA-Compatible"" content=""IE=edge;"">
            <title> Create Tizen Project Finish </title>
            <style type=""text/css"">
            body {{
                background-color: #EAEAEA;
                display: flex;
                justify-content: flex-start;
                align-items: center;
                height: 95vh;
                width: 95vw;
            }}
            button {{
                background:#1F87FF;
                color:#FFF;
                border: none;
                position: relative;
                height: 60px;
                font-size:1.2em;
                padding: 0 2em;
                cursor: pointer;
                transition: 800ms ease all;
                outline: none;
                width: 150px;
                margin: 0 30px;
            }}

            button:hover {{
                background:#EAEAEA;
                color:#1F87FF;
            }}

            button: before,button: after {{
                content: '';
                position: absolute;
                top: 0;
                right: 0;
                height: 3px;
                width: 0;
                background: #1F87FF;
                transition: 400ms ease all;
            }}

            button: after {{
                right: inherit;
                top: inherit;
                left: 0;
                bottom: 0;
            }}

            button:hover:before, button:hover:after {{
                width: 100%;
                transition: 800ms ease all;
            }}

            img {{
                width: fit-content;
                height: fit-content;
                padding: 15px 50px;
                border: 2px solid #1F87FF;
                background-color: #D3E5FB;
                margin-right: 50px;
            }}
            h1 {{
                font-size: 25px;
                color: #000000;
                font-weight: 700;
                margin: 150px auto;
            }}

            span {{
                font-size: 24px;
                line-height:32px;
                color: #5a5a5a;
                margin: 20px 0;
            }}
            input {{
                width: 360px;
                height: 34px;
            }}
            #projectContainer {{
                display: flex;
                width: 50vw;
                justify-content: space-around;
                align-items: center;
            }}
            #buttonContainer {{
                display: flex;
                justify-content: center;
                align-items: center;
                margin: 150px auto;
            }}
            @media(max-width: 1050px), (max-height: 900px) {{
                #projectContainer {{
                    flex-wrap: column;
                }}
                h1 {{
                    font-size: 18px;
                    margin: 60px auto;
                }}
                img {{
                    max-height: 352px;
                    max-width: 256px;
                    margin-bottom: 60px;
                    margin-right: 0;
                }}
                button {{
                    height: 40px;
                    width: 115px;
                    font-size: 18px;
                    margin-right: 10px;
                    margin-left: 10px;
                }}
                input {{
                    width: 300px;
                }}
                #buttonContainer {{
                    margin: 60px auto;
                }}
            }}
            </style>
            </head>
            <body>
            <div id=""FinishScreen"" style=""display: flex; flex-direction: column; justify-content: center; align-items: center; height: 100vh; width: 100vw;"">
                <h1>Define the project properties</h1>
                 <div id=""projectContainer"" style=""display: flex; flex-direction: row; justify-content: center; align-items: center;"">
                    <div id=""inputContainer"" style=""width: inherit; display:flex; flex-direction: column; justify-content: center; align-items: center; margin-right: 100px;"">
                        <div id=""projectNameContainer"" style=""width: inherit; height: 128px; display: flex; flex-direction: column; justify-content: center;"">
                            <span>Project name</span>
                            <div style=""width: inherit; display: flex; flex-direction: row;"">
                               <input type=""text"" id=""project_path"" placeholder=""Please enter a Project Name"">
                            </div>
                        </div>
                        <div id=""workspacePathContainer"" style=""width:inherit; height: 128px; display: flex; flex-direction: column; justify-content: center;"">
                            <span>Workspace Path</span>
                            <div style=""width: inherit; display: flex; flex-direction: row;"">
                                <input type=""text"" id=""workspace_path"" placeholder=""Select Workspace Path"">
                                <button id=""browse"" style=""width: 130px; text-align: center;"">Browse</button>                               
                            </div>
                        </div>
                    </div>
                    <div id=""screenshotContainer"" style=""width: inherit; display: flex; flex-direction: column; justify-content: center; align-items: center; padding-top: 30px; padding-bottom: 30px; margin-left: 100px;"">
                        {0}
                    </div>
                </div>
                <div id=""buttonContainer"">
                    <button id=""back"">Back</button>
                    <button id=""finish"">Finish</button>
                </div>
            </div>  
            <script>
            function ScriptWorkspaceUpdate(arg) {{
                document.getElementById('workspace_path').value = arg;
            }}
            (function() {{
                var btn = document.getElementById('finish');
                var backCommand = '{1}';
                
                browse.addEventListener('click', function() {{
                    window.external.Browse()
                }})
                btn.addEventListener('click', function() {{
                    var value = document.getElementById('project_path').value;
                    var workspace = document.getElementById('workspace_path').value;
                    window.external.CreateProject(value, workspace)
                }})
                back.addEventListener('click', function() {{
                    window.external.Transit(backCommand)
                }})
            }}())
            </script>
            </body>
            </html>", imageStr, backCommand);
        }

        private string GetWebviewContentTemplate(string backCommand, string text)
        {
            //deconstruct the text to type and profile
            (string type, string profile) = (text.Split(';')[0], text.Split(';')[1]);
            string profileClass = profile.Split('-')[0];
            ProfileName = profile;
            List<string> templateList = _templateListDictionary[type][profile];
            Dictionary<string, (Uri normal, Uri select)> imageMap = new Dictionary<string, (Uri, Uri)>();
            var dirPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);        

            foreach (string val in templateList)
            {
                string strNormal = Path.Combine(ToolsPathInfo.TizenCorePath, @"resources", type, profileClass, val, "icon_n.png");
                string strSelect = Path.Combine(ToolsPathInfo.TizenCorePath, @"resources", type, profileClass, val, "icon_s.png");
                string dir = Path.GetDirectoryName(strNormal);
                if (!Directory.Exists(dir) || !File.Exists(strNormal) || !File.Exists(strSelect))
                {
                    strNormal = Path.Combine(dirPath, @"images", "device_template_nor.png");
                    strSelect = Path.Combine(dirPath, @"images", "device_template_sel.png");
                }

                Uri imageNormal = new Uri(strNormal);
                Uri imageSel = new Uri(strSelect);
                imageMap[val] = (imageNormal, imageSel);
            }

            string str = null; //div elements
            string str2 = null; //select listener
            foreach (string prof in templateList)
            {
                str += "<div class=\"projectSelect\" id=\"" + prof + "\"><span style=\"font-size: 18px;  color: black;  font-weight: 400;\">" + prof + "</span><img src=\"" + imageMap[prof].normal + "\" id=\"" + prof + "-image\"></div>";
            }
            foreach (string prof in templateList)
            {
                str2 += "var " + prof + "= document.getElementById(\"" + prof + "\")," + '\n';
                str2 += prof + "image = document.getElementById(\"" + prof + "-image\");" + '\n';
            }
            foreach (string prof in templateList)
            {
                str2 += prof + ".addEventListener('click', function(){" + '\n';
                str2 += prof + ".style.backgroundColor = \"#1F87FF\";" + '\n';
                str2 += prof + "image.src=\"" + imageMap[prof].select + "\";" + '\n';
                foreach (string prof2 in templateList)
                {
                    if (prof2 != prof)
                    {
                        str2 += prof2 + ".style.backgroundColor=\"#FFFFFF\";" + '\n';
                        str2 += prof2 + "image.src=\"" + imageMap[prof2].normal + "\";" + '\n';
                    }
                }

                str2 += "type=\"" + type + "\";" + '\n';
                str2 += "profile=\"" + profile + "\";" + '\n';
                str2 += "next_btn.disabled=false;" + '\n';
                str2 += "select=\"" + prof + "\";" + '\n';
                str2 += "})" + '\n';
            }

            return string.Format(@"<!DOCTYPE HTML>
            <html>
            <head>
            <meta charset=""UTF-8"" http-equiv=""X-UA-Compatible"" content=""IE=edge;"">
            <title> Template </title>
            <style type=""text/css"">
            body {{
                background-color: #EAEAEA;
                display: flex;
                flex-wrap: wrap;
                flex-direction: column;
                justify-content: flex-start;
                align-items: center;
                height: 95vh;
                width: 95vw;
                padding: 0;
            }}
            .projectSelect {{
                display: flex;
                flex-direction: column;
                justify-content: center;
                align-items: center;
                height: 240px;
                width: 240px;
                margin: 20px;
                background-color: white;
                cursor: pointer;
                border: 3px solid transparent;
            }}
            .projectSelect:hover {{
                border-color: #1F87FF;
                z-index: 10;
            }}

            button {{
                background: #1F87FF;
                color: #FFF;
                border: none;
                position: relative;
                height: 60px;
                font-size:1.2em;
                padding: 0 2em;
                cursor: pointer;
                transition: 800ms ease all;
                outline: none;
                width: 150px;
                margin: 0 30px;
            }}
            button:hover {{
                background: #EAEAEA;
                color: #1F87FF;
            }}

            button: before,button: after {{
                content: '';
                position: absolute;
                top: 0;
                right: 0;
                height: 3px;
                width: 0;
                background: #1F87FF;
                transition: 400ms ease all;
            }}

            button: after {{
                right: inherit;
                top: inherit;
                left: 0;
                bottom: 0;
            }}

            button:hover:before,button:hover:after {{
                width: 100%;
                transition: 800ms ease all;
            }}
            #next:disabled {{
                background: #DDDDDD;
            }}
            span {{
                font-size: 24px;
                color: black;
                font-weight: 700; 
            }}
            img {{
                height: 180px;
                width: 180px;
            }}
            @media(max-width: 1050px), (max-height: 900px) {{
                .projectSelect {{
                    height: 240px;
                    width: 240px;
                    margin: 10px 10px;
                }}
                span {{
                    font-size: 20px;
                }}
                img {{
                    height: 180px;
                    width: 180px;
                }}
                button {{
                    height: 40px;
                    width: 115px;
                    font-size: 18px;
                }}
                div {{
                    margin: 10px 10px;
                }}
            }}
            @media(max-width: 546px), (max-height: 396px) {{
                .projectSelect {{
                    height: 120px;
                    width: 120px;
                    margin: 10px 10px;
                }}
                span {{
                    font-size: 16px;
                }}
                img {{
                    height: 80px;
                    width: 80px;
                }}
                button {{
                    height: 32px;
                    width: 100px;
                    font-size: 15px;
                    margin-right: 7px;
                    margin-left: 1px;
                }}
            }}
            </style>
            </head>
            <body>
            <div style=""width: inherit; height: fit-content; display: flex; flex-direction: row; justify-content: center; align-items: center; flex-wrap: wrap;"">
                <span style=""margin-bottom: 40px;"">Select the application template</span>
                <div style=""width: inherit; height: fit-content; display: flex; justify-content: center; align-items: center; margin-bottom: 40px; flex-wrap: wrap;"" id=""itemsContainer"">
                </div>
                <div>
                    <button id=""back"">Back</button>
                    <button id=""next"" disabled>Next</button>
                </div>
            </div>
            <script>
            container = document.getElementById(""itemsContainer"");
            container.innerHTML+='{0}';
            (function() {{
                var next_btn = document.getElementById('next'),value;
                var type, profile, select;
                var backCommand = '{1}';
                {2};
                next.addEventListener('click', function() {{
                    window.external.Transit('Command:Finish,Back:Template,Text:' + type + ';' + profile + ';' + select)
                }});
                back.addEventListener('click', function() {{
                    window.external.Transit(backCommand)
                }});
            }} ())
            </script>
            </body>
            </html>", str, backCommand, str2);
        }
        private string GetWebviewContentProfile(string backCommand, string type)
        {
            ProjectType = type;
            List<string> profileList = new List<string>(_templateListDictionary[type].Keys);
            HashSet<string> profileHash = new HashSet<string>();
            Dictionary<string, List<string>> profileMap = new Dictionary<string, List<string>>();
            Dictionary<string, List<Uri>> imageMap = new Dictionary<string, List<Uri>>();
            foreach (string key in profileList)
            {
                //profile class must be unique, thereby storing in a hash set
                string profile = key.Split('-')[0];
                if (!!profileHash.Add(profile))
                {
                    profileMap[profile] = new List<string>();
                }
                profileMap[profile].Add(key);
            }

            foreach (string prof in profileHash)
            {
                string imageNormalStr = "device_" + prof + "_nor.png";
                string imageSelStr = "device_" + prof + "_sel.png";
                var dirPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                Uri imageNormal = new Uri(Path.Combine(dirPath, @"images", imageNormalStr));
                Uri imageSel = new Uri(Path.Combine(dirPath, @"images", imageSelStr));
                List<Uri> entryList = new List<Uri>
                {
                    imageNormal,
                    imageSel
                };
                imageMap[prof] = entryList;
            }
            string str = null;
            string str2 = null;
            foreach (string prof in profileHash)
            {
                List<Uri> imageList = imageMap[prof];
                List<string> profileNameList = profileMap[prof];
                str += "<div class=\"appSelect\" id=\"" + prof + "\">" + '\n';
                str += "<span>" + prof + "</span>" + '\n';
                str += "<img src=\"" + imageList[0] + "\" id=\"" + prof + "-image\">" + '\n';
                str += "<select id=\"" + prof + "select-item\" style=\"display:flex; flex-direction: column; justify-content: center; align-items: center; width: 200px; height: 30px; font-size: 15pt;\">" + '\n';
                str += "<option value=\"ready\">select version</option>" + '\n';
                foreach (string profileName in profileNameList)
                {
                    str += "<option value=\"" + profileName + "\">" + profileName + "</option>" + '\n';
                }
                str += "</select>" + '\n';
                str += "</div>" + '\n';
            }
            foreach (string prof in profileHash)
            {
                List<Uri> imageList = imageMap[prof];
                str2 += "var " + prof + "= document.getElementById(\"" + prof + "\")," + '\n';
                str2 += prof + "image = document.getElementById(\"" + prof + "-image\")," + '\n';
                str2 += prof + "select = document.getElementById(\"" + prof + "select-item\");" + '\n';
            }
            foreach (string prof in profileHash)
            {
                List<Uri> imageList = imageMap[prof];
                str2 += prof + "select.addEventListener(\"change\", function(){" + '\n';
                str2 += "version=" + prof + "select.value;" + '\n';
                str2 += "})" + '\n';
                str2 += prof + ".addEventListener(\"click\", function(){" + '\n';
                str2 += prof + ".style.backgroundColor = \"#1F87FF\";" + '\n';
                str2 += prof + "image.src=\"" + imageList[1] + "\";" + '\n';
                foreach (string prof2 in profileHash)
                {
                    List<Uri> imageList2 = imageMap[prof2];
                    if (prof2 != prof)
                    {
                        str2 += prof2 + ".style.backgroundColor=\"#FFFFFF\";" + '\n';
                        str2 += prof2 + "image.src=\"" + imageList2[0] + "\";" + '\n';
                        str2 += prof2 + "select.value = \"ready\";";
                    }
                }

                str2 += "type=\"" + type + "\";" + '\n';
                str2 += "profile=\"" + prof + "\";" + '\n';
                str2 += "if(" + prof + "select.value != \"ready\") { next_btn.disabled=false; } else { next_btn.disabled=true; }" + '\n';
                str2 += "version=" + prof + "select.value;" + '\n';
                str2 += "})" + '\n';
            }
            return string.Format(@"<!DOCTYPE HTML> 
            <html> 
            <head> 
            <meta charset=""UTF-8"" http-equiv=""X-UA-Compatible"" content=""IE=edge;"">
            <title> Template </title>
            <style type = ""text/css"">
            body {{
                background-color: #EAEAEA;
                display: flex;
                flex-direction: column;
                justify-content: center;
                align-items: center;
                height: inherit;
                width: inherit;
                padding: 0;
            }}
            .appSelect {{
                display: flex;
                flex-direction: column;
                justify-content: center;
                align-items:center;
                height: 415px;
                width: 474px;
                margin: 38px 20px;
                background-color: white;
                cursor: pointer;
                border: 3px solid transparent;
            }}
            .appSelect:hover {{
                border-color: #1F87FF;
            }}
            button {{
                background:#1F87FF;
                color:#FFF;
                border: none;
                position: relative;
                height: 60px;
                font-size:1.2em;
                padding: 0 2em;
                cursor: pointer;
                transition: 800ms ease all;
                outline: none;
                width: 150px;
                margin: 0 30px;
            }}
            button:hover{{
                background:#EAEAEA;
                color:#1F87FF;
            }}
            button: before,button: after {{
                content: '';
                position: absolute;
                top: 0;
                right: 0;
                height: 3px;
                width: 0;
                background: #1F87FF;
                transition: 400ms ease all;
            }}
            button: after{{
                right: inherit;
                top: inherit;
                left: 0;
                bottom: 0;
            }}
            button:hover:before,button:hover:after {{
                width: 100%;
                transition: 800ms ease all;
            }}
            #next:disabled {{
                background: #DDDDDD;
            }}
            span {{ 
                font-size: 24px;
                color: black;
                font-weight: 700; 
            }}
            img {{
                height: 150px;
                width: 150px;
                padding: 30px;
            }}
            @media(max-width: 1050px), (max-height: 900px) {{
                .appSelect {{
                    height: 250px;
                    width: 250px;
                    margin: 0 10px;
                }}
                span {{
                    font-size: 20px;
                }}
                img {{
                    height: 120px;
                    width: 120px;
                    padding-top: 25px;
                    padding-bottom: 20px;
                }}
                button {{
                    height: 40px;
                    width: 115px;
                    font-size: 18px;
                }}
                div {{
                    margin: 10px 0;
                }}
            }}
            @media(max-width: 546px), (max-height: 396px) {{
                .appSelect {{
                    height: 180px;
                    width: 205px;
                    margin: 10px 10px;
                }}
                span {{
                    font-size: 15px;
                }}
                img {{
                    height: 110px;
                    width: 110px;
                    padding: 20px;
                }}
                button {{
                    height: 32px;
                    width: 100px;
                    font-size: 15px;
                    margin-right: 7px;
                    margin-left: 1px;
                }}
            }}
            </style>
            </head>
            <body>
            <div style=""height: 95vh; display:flex; flex-direction: column; justify-content: center; align-items: center;"">
                <span style=""margin-bottom: 40px;"">Select the application profile</span>
                <div style=""display: flex; justify-content: center; align-items: center; margin-bottom: 40px; flex-wrap: wrap;"">
                {0}
                </div>
                <div>
                    <button id=""back"">Back</button>
                    <button id= ""next"" disabled>Next</button>
                </div>
            </div>
            <script>
            (function() {{
                var next_btn = document.getElementById('next'), type, profile, version;
                var backCommand = '{1}';
                {2};
                next.addEventListener('click', function() {{
                    window.external.Transit('Command:Template,Back:Profile,Text:' + type + ';' + version)
                }});
                back.addEventListener('click', function() {{
                    window.external.Transit(backCommand)
                }});
            }} ())
            </script>
            </body>
            </html>", str, backCommand, str2);
        }
        private string GetWebviewContentSelect()
        {
            var dirPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Uri tImageNor = new Uri(Path.Combine(dirPath, @"images\", "entry_template_icon_nor.png"));  // TODO: fetch images from tizen studio eg:C:\tizen-studio\tools\tizen-core\templates\web\mobile\BasicUI
            Uri tImageSel = new Uri(Path.Combine(dirPath, @"images\", "entry_template_icon_sel.png"));   // TODO: fetch images from tizen studio eg:C:\tizen-studio\tools\tizen-core\templates\web\mobile\BasicUI

            string str = null;
            string str2 = null;

            for (int i = 0; i < _projectsArr.Length; i++)
            {
                str += "<div class=\"projectSelect\" id=\"" + _projectsArr[i] + "\"><span>" + _projectsArr[i] + "</span><img src=\"" + tImageNor + "\" id=\"" + _projectsArr[i] + "img\"></div>";
            }

            for (int i = 0; i < _projectsArr.Length; i++)
            {
                str2 += "var " + _projectsArr[i] + "=document.getElementById(\"" + _projectsArr[i] + "\")," + _projectsArr[i] + "img=document.getElementById(\"" + _projectsArr[i] + "img\");";
            }

            for (int i = 0; i < _projectsArr.Length; i++)
            {
                str2 += _projectsArr[i] + ".addEventListener(\"click\", function() {" + _projectsArr[i] + ".style.backgroundColor = \"#1F87FF\";" + _projectsArr[i] + "img.src = \"" + tImageSel + "\";next_btn.disabled = false;";
                string projecct = _projectsArr[i];
                for (int j = 0; j < _projectsArr.Length; j++)
                {
                    if (_projectsArr[j] != projecct)
                    {
                        str2 += _projectsArr[j] + ".style.backgroundColor = \"#FFFFFF\";";
                        str2 += _projectsArr[j] + "img.src = \"" + tImageNor + "\";";
                    }
                }
                str2 += "value = \"" + _projectsArr[i] + "\";})";
                str2 += Environment.NewLine;
            }

            return string.Format(@"<!DOCTYPE HTML>
            <html>
            <head>
            <meta charset = ""UTF-8"" http-equiv=""X-UA-Compatible"" content=""IE=edge;"">
            <title> Template </title>
            <style type = ""text/css"">
            body {{
                background-color: #EAEAEA;
                display: flex;
                flex-direction: column;
                justify-content: center;
                align-items: center;
                height: inherit;
                width: inherit;
                padding: 0;
            }}
            .projectSelect {{
                display: flex;
                flex-direction: column;
                justify-content: center;
                align-items: center;
                border-radius: 50%;
                height: 350px;
                width: 350px;
                margin: 20px;
                background-color: white;
                cursor: pointer;
                border: 3px solid transparent;
            }}
            .projectSelect:hover {{
                border-color: #1F87FF;
            }}
            button {{
                background:#1F87FF;
                color:#FFF;
                border: none;
                position: relative;
                height: 60px;
                font-size:1.2em;
                padding: 0 2em;
                cursor: pointer;
                transition: 800ms ease all;
                outline: none;
                width: 150px;
                margin: 0 30px;
            }}
            button:hover {{
                background:#EAEAEA;
                color:#1F87FF;
            }}
            button: before,button: after {{
                content: '';
                position: absolute;
                top: 0;
                right: 0;
                height: 3px;
                width: 0;
                background: #1F87FF;
                transition: 400ms ease all;
            }}
            button: after {{
                right: inherit;
                top: inherit;
                left: 0;
                bottom: 0;
            }}
            button:hover:before,button:hover:after {{
                width: 100%;
                transition: 800ms ease all;
            }}
            #next:disabled {{
                background: #DDDDDD;
            }}
            span {{
                font-size: 24px;
                color: black;
                font-weight: 700; 
            }}
            img {{
                height: 300px;
                width: 300px;
            }}
            @media(max-width: 1050px), (max-height: 900px) {{
                .projectSelect {{
                    height: 250px;
                    width: 250px;
                    margin: 10px 10px;
                }}
                span {{
                    font-size: 20px;
                }}
                img {{
                    height: 180px;
                    width: 180px;
                }}
                button {{
                    height: 40px;
                    width: 115px;
                    font-size: 18px;
                }}
                div {{
                    margin: 10px 10px;
                }}
            }}
            @media(max-width: 546px), (max-height: 396px) {{
                .projectSelect {{
                    height: 180px;
                    width: 180px;
                    margin: 10px 10px;
                }}
                span {{
                    font-size: 16px;
                }}
                img {{
                    height: 120px;
                    width: 120px;
                }}
                button {{
                    height: 32px;
                    width: 100px;
                    font-size: 15px;
                    margin-right: 7px;
                    margin-left: 1px;
                }}
            }}
            </style>
            </head>
            <body>
            <div style = ""height: 95vh; display:flex; flex-direction: column; justify-content: center; align-items: center; text-align: center;""> 
                <span style = ""margin-bottom: 40px;""> Select the type of project</span>
                <div style=""display: flex; justify-content: center; align-items: center; margin-bottom: 40px; flex-wrap: wrap;"" id=""itemsContainer""></div>
                <div>
                    <button id=""next"" disabled=true>Next</button>
                </div>
            </div>
            <script>
                (function() {{
                    container = document.getElementById(""itemsContainer"");
                    container.innerHTML +='{0}';
                    var next_btn = document.getElementById('next'),value;
                    var select;
                    {1};
                    next.addEventListener('click', function() {{ 
                        window.external.Transit('Command:Profile,Back:Select,Text:' + value) 
                    }});
                }}())
            </script>
            </body> 
            </html>", str, str2);
        }
        private string GetWebviewContentHome()
        {
            var dirPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Uri homeImg = new Uri(Path.Combine(dirPath, @"images\", "wappl_icon.png")); // 'wappl_icon.png';  // TODO: fetch proper image
            return string.Format(@"<!DOCTYPE HTML>
            <html>
            <head>
            <meta charset=""UTF-8"" http-equiv=""X-UA-Compatible"" content=""IE=edge;"">
            <title>Home</title> <style type = ""text/css"">
            body {{
              background-color: #EAEAEA;
              display: flex;
              flex-direction: column;
              justify-content: center;
              align-items: center;
              height: 100vh;
              width: 100vw;
              padding: 0;
              margin: 0 auto;
            }}
            button {{
              background:#1F87FF;
              color:#FFF;
              border: none;
              position: relative;
              height: 60px;
              font-size:1.2em;
              padding: 0 2em;
              cursor: pointer;
              transition: 800ms ease all;
              outline: none;
              margin-top: 50px;
              width: 500px;
            }}
            button:hover {{
              background:#EAEAEA;
              color:#1F87FF;
            }}
            button:before,button:after {{
              content: '';
              position: absolute;
              top: 0;
              right: 0;
              height: 3px;
              width: 0;
              background: #1F87FF;
              transition: 400ms ease all;
            }}
            button: after {{
              right: inherit;
              top: inherit;
              left: 0;
              bottom: 0;
            }}
            button:hover:before,button:hover:after {{
              width: 100%;
              transition: 800ms ease all;
            }}
            img {{
              height: 500px;
              width: 500px;
              margin-top: 50px;
            }}
            img:hover {{
              opacity: 0.6;
              transition: 800ms ease all;
            }}
            span {{
              font-size: 40px;
              color: black;
            }}
            @media(max-width: 570px), (max-height: 710px) {{
              span {{
                font-size: 28px;
              }}
              img {{
                height: 300px;
                width: 300px;
                margin-top: 20px;
              }}
              button {{
                height: 45px;
                width: 300px;
                font-size: 18px;
                margin-top: 20px;
              }}
            }}
            </style>
            </head>
            <body>
            <div style=""display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center"">
                <span>Tizen Application Studio</span>
                <img src=""{0}"" id =""template""/>
                <button id=""next"">New Project</button>
            </div>
            <script>
                next.addEventListener('click', function() {{
                    window.external.Transit('Command:Select,Back:Home,Text:')
                }});
            </script>
            </body>
            </html>", homeImg);
        }

        private void PageLoadCompleted(object sender, NavigationEventArgs e)
        {
            WebBrowser wb = sender as WebBrowser;
            if (_navigationHistory["Current"] == "Finish")
            {
                WorkspacePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                                            + "\\source\\repos";
                _ = wb.InvokeScript("ScriptWorkspaceUpdate", WorkspacePath);
            }
        }
        public void Transit(string message)
        {
            System.Diagnostics.Debug.Write(message);
            Dictionary<string, string> messageDictionary = message.Split(',').ToDictionary(
                k => k.Split(':')[0],
                v => v.Split(':')[1]);

            string command = "";
            string text = "";
            string backCmdMessage = "";
            if (!!messageDictionary.ContainsKey("Command"))
            {
                command = messageDictionary["Command"];
            }

            if (!!messageDictionary.ContainsKey("Text"))
            {
                text = messageDictionary["Text"];
            }

            //recording the navigation history for Back navigation
            _navigationHistory[command] = message;
            _navigationHistory["Current"] = command;

            if (!!messageDictionary.ContainsKey("Back"))
            {
                string backCmd = messageDictionary["Back"];
                if (!string.IsNullOrEmpty(backCmd) && _navigationHistory.ContainsKey(backCmd))
                {
                    backCmdMessage = _navigationHistory[backCmd];
                }
            }

            switch (command)
            {
                case "Home":
                    _webBrowserRef.NavigateToString(GetWebviewContentHome());
                    break;
                case "Select":
                    _webBrowserRef.NavigateToString(GetWebviewContentSelect());
                    break;
                case "Profile":
                    _webBrowserRef.NavigateToString(GetWebviewContentProfile(backCmdMessage, text));
                    break;
                case "Template":
                    _webBrowserRef.NavigateToString(GetWebviewContentTemplate(backCmdMessage, text));
                    break;
                case "Finish":
                    _webBrowserRef.NavigateToString(GetWebviewContentFinish(backCmdMessage, text));
                    break;
                default:
                    System.Diagnostics.Debug.Write("Unknown command {0}. Cannot process.", command);
                    _ = _navigationHistory.Remove(command);
                    break;
            }
        }

        private string GetToolsFolderDialog()
        {
            using (System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = (!string.IsNullOrEmpty(WorkspacePath)) ? WorkspacePath : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) 
                                + "\\source\\repos",
                Description = "Tizen Workspace Path",
                ShowNewFolderButton = true
            })
            {
                return (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) ?
                    folderBrowserDialog.SelectedPath : string.Empty;
            }
        }

        public void Browse()
        {
            string folderPath = GetToolsFolderDialog();
            if (!string.IsNullOrEmpty(folderPath))
            {
                WorkspacePath = folderPath;
                _ = _webBrowserRef.InvokeScript("ScriptWorkspaceUpdate", folderPath);
            }
        }

        private bool ValidateProjectName(string wkspce, string prjName)
        {
            if (string.IsNullOrWhiteSpace(prjName))
            {
                _ = MessageBox.Show("Project name cannot be empty.");
                return false;
            }

            // Check if Project starts with something other than alphabet
            if (!char.IsLetter(prjName[0]))
            {
                _ = MessageBox.Show("Project name must start with alphabet.");
                return false;
            }

            // Check if Project name contains a-zA-Z0-9_ only
            // Handles unicode control char and surrogate char check
            Regex objAlphaPattern = new Regex(@"^[a-zA-Z0-9_]*$");
            bool sts = objAlphaPattern.IsMatch(prjName);
            if (!sts)
            {
                _ = MessageBox.Show("Project name can only have [a-zA-Z0-9_]");
                return false;
            }

            // Check if Project name contain less than 3 chars
            if (prjName.Length < 3 || prjName.Length > 50)
            {
                _ = MessageBox.Show("Project name length must be 3-50 chars.");
                return false;
            }

            //Check if project already exists in the path
            if (!string.IsNullOrEmpty(wkspce) && Directory.Exists(Path.Combine(wkspce, prjName)))
            {
                _ = MessageBox.Show($"Project {prjName} already exists at {wkspce}!");
                return false;
            }

            return true;
        }
        public void CreateProject(string prjName, string wkspce)
        {
            if (!ValidateProjectName(wkspce, prjName))
            {
                System.Diagnostics.Debug.Write("Project name validation failed.");
                return;
            }

            //Save the Finish states
            ProjectName = prjName;

            //Append the prjName folder which does get created below.
            WorkspacePath = !string.IsNullOrEmpty(wkspce) ? Path.Combine(wkspce, prjName) : Path.Combine(ToolsPathInfo.TizenCorePath, prjName);

            //close Create Wizard window
            _projectWizardCreateWizard.Close();

            WaitDialogUtil waitPopup = new WaitDialogUtil();
            waitPopup.ShowPopup("Creating Tizen Project",
                    "Please wait while the new project is being loaded...",
                    "Preparing...", "Tizen project load in progress...");

            if (!Directory.Exists(WorkspacePath))
            {
                _ = Directory.CreateDirectory(WorkspacePath);
            }

            TzCmdExec executor = new TzCmdExec();
            // Added \" escape sequence to include Workspace Path in quotes ("") to avoid error if Path has Spaces.
            string message = executor.RunTzCmnd(string.Format("/c tz init -t {0} -p {1} -w \"{2}\"", ProjectType, ProfileName, WorkspacePath));

            if (!string.IsNullOrWhiteSpace(message))
            {
                _ = MessageBox.Show(message);
                waitPopup.ClosePopup();
                return;
            }

            // Added \" escape sequence to include Workspace Path in quotes ("") to avoid error if Path has Spaces.
            message = executor.RunTzCmnd(string.Format("/c tz new -t \"{0}\" -w \"{1}\" -p \"{2}\"", TemplateName, WorkspacePath, ProjectName));

            if (!string.IsNullOrWhiteSpace(message))
            {
                _ = MessageBox.Show(message);
                waitPopup.ClosePopup();
                return;
            }

            EnvDTE80.DTE2 dte = Package.GetGlobalService(typeof(EnvDTE._DTE)) as EnvDTE80.DTE2;
            if (ProjectType == "dotnet")
            {
                if (!File.Exists(string.Format("{0}\\{1}\\{2}.sln", WorkspacePath, ProjectName, ProjectName.ToLower())))
                {
                    _ = MessageBox.Show("Unable to find solution file");
                    waitPopup.ClosePopup();
                    return;
                }
                dte.Solution.Open(string.Format("{0}\\{1}\\{2}.sln", WorkspacePath, ProjectName, ProjectName.ToLower()));

            }
            else
            {
                if (!File.Exists(string.Format("{0}\\{1}.sln", WorkspacePath, ProjectName)))
                {
                    _ = MessageBox.Show("Unable to find solution file");
                    waitPopup.ClosePopup();
                    return;
                }
                dte.Solution.Open(string.Format("{0}\\{1}.sln", WorkspacePath, ProjectName));

                if (File.Exists(string.Format("{0}\\{1}\\config.xml", WorkspacePath, ProjectName)))
                {
                    _ = dte.ItemOperations.OpenFile(WorkspacePath + "\\" + ProjectName + "\\config.xml");
                }

                string solutionName = Path.GetFileNameWithoutExtension(dte.Solution.FullName);
                dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();
                EnvDTE.UIHierarchyItem root = dte.ToolWindows.SolutionExplorer.GetItem(solutionName + @"\" + ProjectName);
                if (root != null)
                {
                    root.Select(EnvDTE.vsUISelectionType.vsUISelectionTypeSelect);
                    root.UIHierarchyItems.Expanded = true;
                }
            }
            waitPopup.ClosePopup();
        }
    }
}
