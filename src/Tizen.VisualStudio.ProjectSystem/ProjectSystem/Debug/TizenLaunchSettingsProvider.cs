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
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Tizen.VisualStudio.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.ProjectSystem;
using System.Threading.Tasks.Dataflow;
using Tizen.VisualStudio.Tools.DebugBridge;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Tizen.VisualStudio.ProjectSystem.Debug
{
    [Export(typeof(ITizenLaunchSettingsProvider))]
    internal class TizenLaunchSettingsProvider : OnceInitializedOnceDisposed, ITizenLaunchSettingsProvider
    {
        private bool isProjectLoadingTime = true;

        [ImportingConstructor]
        public TizenLaunchSettingsProvider(UnconfiguredProject unconfiguredProject, IUnconfiguredProjectCommonServices commonProjectServices)
        {
            CommonProjectServices = commonProjectServices;
            SourceControlIntegrations = new OrderPrecedenceImportCollection<ISourceCodeControlIntegration>(projectCapabilityCheckProvider: unconfiguredProject);
            FileManager = new Win32FileSystem();

            ProjectSubscriptionService = CommonProjectServices.ActiveConfiguredProject.Services.ProjectSubscription;
        }

        private IProjectSubscriptionService ProjectSubscriptionService { get; }

        [ImportMany]
        private OrderPrecedenceImportCollection<ISourceCodeControlIntegration> SourceControlIntegrations { get; set; }

        private IUnconfiguredProjectCommonServices CommonProjectServices { get; }

        public const string TizenLaunchSettingsFilename = @"tizenLaunchSetttings.json";
        public const string TizenDefaultSettingsFileFoler = "Properties";

        protected IFileSystem FileManager { get; set; }

        protected IDisposable ProjectRuleSubscriptionLink { get; set; }

        // When we are saveing the file we set this to minimize noise from the file change
        protected bool IgnoreFileChanges { get; set; }

        // Returns the full path to the tizen launch settings file
        private string _tizenLaunchSettingsFile;
        public string TizenLaunchSettingsFile
        {
            get
            {
                if (_tizenLaunchSettingsFile == null)
                {
                    _tizenLaunchSettingsFile = Path.Combine(Path.GetDirectoryName(CommonProjectServices.Project.FullPath), TizenLaunchSettingsFileFolder, TizenLaunchSettingsFilename);
                }

                return _tizenLaunchSettingsFile;
            }
        }

        //
        private string _tizenLaunchSettingsFileFolder;
        private string TizenLaunchSettingsFileFolder
        {
            get
            {
                if (_tizenLaunchSettingsFileFolder == null)
                {
                    _tizenLaunchSettingsFileFolder = TizenDefaultSettingsFileFoler;
                }

                return _tizenLaunchSettingsFileFolder;
            }
        }

        private ITizenLaunchSettings _tizenLaunchSetting;
        public ITizenLaunchSettings TizenLaunchSetting
        {
            get
            {
                EnsureInitialized();
                return _tizenLaunchSetting;
            }

            protected set
            {
                _tizenLaunchSetting = value;
            }
        }

        public async Task UpdateAndSaveTizenSettingsAsync(ITizenLaunchSettings newSettings)
        {
            await CheckoutTizenSettingsFileAsync().ConfigureAwait(false);
            SaveTizenSettingsToDisk(newSettings);
        }
        
        protected async Task CheckoutTizenSettingsFileAsync()
        {
            var sourceControlIntegration = SourceControlIntegrations.FirstOrDefault();
            if (sourceControlIntegration != null && sourceControlIntegration.Value != null)
            {
                await sourceControlIntegration.Value.CanChangeProjectFilesAsync(new[] { TizenLaunchSettingsFile }).ConfigureAwait(false);
            }
        }

        protected void SaveTizenSettingsToDisk(ITizenLaunchSettings newSettings)
        {
            var serializeData = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);

            if (newSettings.Name != string.Empty)
            {
                serializeData.Add(newSettings.Name, TizenLaunchSettings.ToSerializableForm(newSettings));
            }

            try
            {
                EnsureSettingsFolder();

                JsonSerializerSettings settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
                string jsonString = JsonConvert.SerializeObject(serializeData, Formatting.Indented, settings);

                IgnoreFileChanges = true;
                FileManager.WriteAllText(TizenLaunchSettingsFile, jsonString);
            }
            catch (Exception ex)
            {
                string err = string.Format("The following error occurred when writing to the launch settings file '{0}'. {1}", TizenLaunchSettingsFile, ex.Message);
                throw;
            }
            finally
            {
                IgnoreFileChanges = false;
            }
        }

        protected void EnsureSettingsFolder()
        {
            var tizenLaunchSettingsFileFolderPath = Path.Combine(Path.GetDirectoryName(CommonProjectServices.Project.FullPath), _tizenLaunchSettingsFileFolder);
            if (!FileManager.DirectoryExists(tizenLaunchSettingsFileFolderPath))
            {
                FileManager.CreateDirectory(tizenLaunchSettingsFileFolderPath);
            }
        }

        protected override void Initialize()
        {
            if (ProjectSubscriptionService != null)
            {
                var projectChangesBlock = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                            DataflowUtilities.CaptureAndApplyExecutionContext<IProjectVersionedValue<IProjectSubscriptionUpdate>>(ProjectRuleBlock_ChangedAsync));

                ProjectRuleSubscriptionLink = ProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    projectChangesBlock,
                    ruleNames: ProjectDebugger.SchemaName,
                    linkOptions: new DataflowLinkOptions { PropagateCompletion = true });
            }

            DeviceManager.SelectDevice(DeviceManager.DeviceInfoList?.FindLast(_ => true));

            UpdateProfilesAsync();
        }

        protected async Task ProjectRuleBlock_ChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> projectSubscriptionUpdate)
        {
            if (projectSubscriptionUpdate.Value.CurrentState.TryGetValue(ProjectDebugger.SchemaName, out IProjectRuleSnapshot ruleSnapshot))
            {
                await Task.Run(() =>
                {
                    SDBDeviceInfo selectedDevice = null;

                    if (isProjectLoadingTime)
                    {
                        selectedDevice = DeviceManager.DeviceInfoList?.FindLast(_ => true);

                        isProjectLoadingTime = false;
                    }
                    else
                    {
                        ruleSnapshot.Properties.TryGetValue(ProjectDebugger.ActiveDebugProfileProperty, out string activeProfile);

                        selectedDevice = DeviceManager.DeviceInfoList.Find(device => activeProfile.Split('#')[0].Equals(device.Serial));
                    }

                    if (selectedDevice != null)
                    {
                        DeviceManager.SelectDevice(selectedDevice);

                        DeviceManager.UpdateDebugTargetList(false);
                    }
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
        }
        
        protected void UpdateProfilesAsync()
        {
            var tizenLaunchSettings = GetTizenLaunchSettings();

            if (tizenLaunchSettings.Name == null)
            {
                tizenLaunchSettings.Name = Path.GetFileNameWithoutExtension(CommonProjectServices.Project.FullPath);
                tizenLaunchSettings.ExtraArguments = string.Empty;
            }

            _tizenLaunchSetting = tizenLaunchSettings;
        }

        protected TizenLaunchSettings GetTizenLaunchSettings()
        {
            TizenLaunchSettings tizenLaunchSettingsData;
            if (FileManager.FileExists(TizenLaunchSettingsFile))
            {
                tizenLaunchSettingsData = ReadTizenSettingsFileFromDisk();
            }
            else
            {
                tizenLaunchSettingsData = new TizenLaunchSettings();
            }

            return tizenLaunchSettingsData;
        }

        private TizenLaunchSettings ReadTizenSettingsFileFromDisk()
        {
            try
            {
                string jsonString = FileManager.ReadAllText(TizenLaunchSettingsFile);
                var tizenLaunchSettingsData = new TizenLaunchSettings();
                JObject jsonObject = JObject.Parse(jsonString);
                foreach (var pair in jsonObject)
                {
                    tizenLaunchSettingsData = TizenLaunchSettings.DeserializeData((JObject)pair.Value);
                    tizenLaunchSettingsData.Name = Path.GetFileNameWithoutExtension(CommonProjectServices.Project.FullPath);
                }

                return tizenLaunchSettingsData;
            }
            catch (JsonReaderException readerEx)
            {
                string err = string.Format("JsonErrorReadingLaunchSettings", readerEx.Message);
                //LogError(err, TizenLaunchSettingsFile, readerEx.LineNumber, readerEx.LinePosition, false);
                throw;
            }
            catch (JsonException jsonEx)
            {
                string err = string.Format("JsonErrorReadingLaunchSettings", jsonEx.Message);
                //LogError(err, TizenLaunchSettingsFile, -1, -1, false);
                throw;
            }
            catch (Exception ex)
            {
                string err = string.Format("ErrorReadingLaunchSettings", Path.Combine(TizenLaunchSettingsFileFolder, TizenLaunchSettingsFilename), ex.Message);
                //LogError(err, false);
                throw;
            }
        }
    }
}
