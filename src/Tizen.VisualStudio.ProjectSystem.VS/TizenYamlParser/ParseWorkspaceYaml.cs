using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Tizen.VisualStudio.TizenYamlParser
{
    public partial class ParseWorkspaceYaml
    {
        [YamlMember(Alias = "auto_gen_build_files", ApplyNamingConventions = false, Description = " Enable auto build file generation")]
        public bool AutoGenBuildFiles { get; set; }

        [YamlMember(Alias = "type", ApplyNamingConventions = false, Description = " Workspace type, [native/web/dotnet]")]
        public string Type { get; set; }

        [YamlMember(Alias = "package_id", ApplyNamingConventions = false, Description = " Package ID for the Tizen package")]
        public string PackageId { get; set; }

        [YamlMember(Alias = "version", ApplyNamingConventions = false, Description = " version for the Tizen package")]
        public string Version { get; set; }

        [YamlMember(Alias = "profile", ApplyNamingConventions = false, Description = " Default profile")]
        public string Profile { get; set; }

        [YamlMember(Alias = "api_version", ApplyNamingConventions = false, Description = " Tizen API version")]
        public string ApiVersion { get; set; }

        [YamlMember(Alias = "profiles_xml_path", ApplyNamingConventions = false, Description = " Path of profiles.xml, containing the signing profiles \n # If value is empty, the profiles.xml in the data_path specified in tizen-studio/tools/tizen-core/config.yaml will be used")]
        public string ProfilesXmlPath { get; set; }

        [YamlMember(Alias = "signing_profile", ApplyNamingConventions = false, Description = " Signing profile to be used for Tizen package signing \n # If value is empty, active signing profile will be used")]
        public string SigningProfile { get; set; }

        [YamlMember(Alias = "build_type", ApplyNamingConventions = false, Description = " Build type [debug/ release/ test]")]
        public string BuildType { get; set; }

        [YamlMember(Alias = "rootstrap", ApplyNamingConventions = false, Description = " Rootstrap for compiling native app")]
        public string Rootstrap { get; set; }

        [YamlMember(Alias = "compiler", ApplyNamingConventions = false, Description = " Default compiler for native app compilation")]
        public string Compiler { get; set; }

        [YamlMember(Alias = "dotnet_cli_path", ApplyNamingConventions = false, Description = " Default path for dotnet-cli")]
        public string DotnetCliPath { get; set; }

        [YamlMember(Alias = "msbuild_path", ApplyNamingConventions = false, Description = " Default path for msbuild")]
        public string MsbuildPath { get; set; }

        [YamlMember(Alias = "dotnet_build_tool", ApplyNamingConventions = false, Description = " Default tool for dotnet build [dotnet-cli/ msbuild]")]
        public string DotnetBuildTool { get; set; }

        [YamlMember(Alias = "tizen_net_version", ApplyNamingConventions = false, Description = " Default nuget version for Tizen.Net")]
        public string TizenNetVersion { get; set; }

        [YamlMember(Alias = "tizen_net_sdk_verison", ApplyNamingConventions = false, Description = " Default nuget version for Xamarin.Forms")]
        public string TizenNetSdkVerison { get; set; }

        [YamlMember(Alias = "xamarin_forms_version", ApplyNamingConventions = false, Description = " Default nuget version for MSBuild.Tasks")]
        public string XamarinFormsVersion { get; set; }

        [YamlMember(Alias = "msbuild_tasks_version", ApplyNamingConventions = false, Description = " Default nuget version for Tizen.Wearable.CircleUI")]
        public string MsbuildTasksVersion { get; set; }

        [YamlMember(Alias = "tizen_wearable_circleui_version", ApplyNamingConventions = false, Description = " Default nuget version for Tizen.Wearable.CircleUI")]
        public string TizenWearableCircleuiVersion { get; set; }

        [YamlMember(Alias = "tizen_opentk_version", ApplyNamingConventions = false, Description = " Default nuget version for Tizen.OpenTK")]
        public string TizenOpentkVersion { get; set; }

        [YamlMember(Alias = "tizen_nuixaml_version", ApplyNamingConventions = false, Description = " Default nuget version for Tizen.NUI.Xaml")]
        public string TizenNuixamlVersion { get; set; }

        [YamlMember(Alias = "tizen_hotreload_version", ApplyNamingConventions = false, Description = " Default nuget version for Tizen.HotReload")]
        public string TizenHotreloadVersion { get; set; }

        [YamlMember(Alias = "working_folder", ApplyNamingConventions = false, Description = " Working folder for dotnet & web workspace, paths to csproj or sln or config.xml, if empty all projects will be build")]
        public string WorkingFolder { get; set; }

        [YamlMember(Alias = "chrome_path", ApplyNamingConventions = false, Description = " Default path for Google Chrome")]
        public string ChromePath { get; set; }

        [YamlMember(Alias = "chrome_simulator_options", ApplyNamingConventions = false, Description = " Default options to be passed to Chrome when running web simulator")]
        public List<string> ChromeSimulatorOptions { get; set; }

        [YamlMember(Alias = "chrome_simulator_data_path", ApplyNamingConventions = false, Description = " Default path for Web Simulator data")]
        public string ChromeSimulatorDataPath { get; set; }

        [YamlMember(Alias = "tv_simulator_path", ApplyNamingConventions = false, Description = " Default path for Samsung Tizen TV Simulator")]
        public string TvSimulatorPath { get; set; }

        [YamlMember(Alias = "chrome_inspector_options", ApplyNamingConventions = false, Description = " Default options to be passed to Chrome when running web inspector")]
        public List<string> ChromeInspectorOptions { get; set; }

        [YamlMember(Alias = "chrome_inspector_data_path", ApplyNamingConventions = false, Description = " Default path for Web Inspector data")]
        public string ChromeInspectorDataPath { get; set; }

        [YamlMember(Alias = "arch", ApplyNamingConventions = false, Description = " default arch for build, [x86/ x86_64/ arm/ aarch64]")]
        public string Arch { get; set; }

        [YamlMember(Alias = "opt", ApplyNamingConventions = false, Description = " Enable size optimization of wgt for web workspace")]
        public bool Opt { get; set; }

        [YamlMember(Alias = "src_file_patterns", ApplyNamingConventions = false, Description = " Source files matching these pattern will always be excluded from build")]
        public List<string> SrcFilePatterns { get; set; }

        [YamlMember(Alias = "test_file_patterns", ApplyNamingConventions = false, Description = " Source files matching these patterns will only be included while building in test mode")]
        public List<string> TestFilePatterns { get; set; }

        [YamlMember(Alias = "projects", ApplyNamingConventions = false, Description = " List of projects in the workspace and their dependencies")]
        public Dictionary<string, List<string>> Projects { get; set; }

        [YamlMember(Alias = "skip_vs_files", ApplyNamingConventions = false, Description = " Skip generating files needed for VS")]
        public bool SkipVSFiles { get; set; }
    }

    public partial class ParseWorkspaceYaml
    {
        public static ParseWorkspaceYaml FromYaml(string ymlContents)
        {
            var deserializer = new DeserializerBuilder()
     .WithNamingConvention(CamelCaseNamingConvention.Instance)
     .Build();

            return deserializer.Deserialize<ParseWorkspaceYaml>(ymlContents);
        }

        public static string ToYaml(ParseWorkspaceYaml self)
        {
            var serializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
            var yaml = serializer.Serialize(self);
            return yaml;
        }
    }
}
