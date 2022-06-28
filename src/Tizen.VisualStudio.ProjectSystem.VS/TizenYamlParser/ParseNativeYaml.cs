using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Tizen.VisualStudio.TizenYamlParser
{
    public partial class ParseNativeYaml
    {
        [YamlMember(Alias = "auto_gen_build_files", ApplyNamingConventions = false, Description = " Enable auto build file generation")]
        public bool AutoGenBuildFiles { get; set; }

        [YamlMember(Alias = "output_name", ApplyNamingConventions = false, Description = " Output name for compiled binary/ lib")]
        public string OutputName { get; set; }

        [YamlMember(Alias = "project_type", ApplyNamingConventions = false, Description = " Project type [native_app, shared_lib, static_lib, test_runner]")]
        public string ProjectType { get; set; }

        [YamlMember(Alias = "sources", ApplyNamingConventions = false, Description = " list of source files (.c, .cpp, .asm, .S etc) and headers")]
        public List<string> Sources { get; set; }

        [YamlMember(Alias = "defines", ApplyNamingConventions = false, Description = " preprocessor defines, passed to the C/C++ compiler with -D options")]
        public List<string> Defines { get; set; }

        [YamlMember(Alias = "include_dirs", ApplyNamingConventions = false, Description = " header include directories, passed to the C/C++ compiler with -I option")]
        public List<string> IncludeDirs { get; set; }

        [YamlMember(Alias = "cflags", ApplyNamingConventions = false, Description = " common compilation flags, passed to both C/C++ compiler")]
        public List<string> Cflags { get; set; }

        [YamlMember(Alias = "cflags_c", ApplyNamingConventions = false, Description = " c compilation flags, passed to only C compiler")]
        public List<string> CflagsC { get; set; }

        [YamlMember(Alias = "cflags_cc", ApplyNamingConventions = false, Description = " cpp compilation flags, passed to only C++ compiler")]
        public List<string> CflagsCc { get; set; }

        [YamlMember(Alias = "asmflags", ApplyNamingConventions = false, Description = " assembler flags, passed to any invocation of a tool that takes an .asm or .S file as input")]
        public List<string> Asmflags { get; set; }

        [YamlMember(Alias = "lib_files", ApplyNamingConventions = false, Description = " list of library names or library paths to link with target, values containing '/' will be passed directly other wise passed with -l option")]
        public List<string> LibFiles { get; set; }

        [YamlMember(Alias = "libs", ApplyNamingConventions = false, Description = " list of library names or library paths to link with target.values containing '/' will be passed directly other wise passed with -l option")]
        public List<string> Libs { get; set; }

        [YamlMember(Alias = "lib_dirs", ApplyNamingConventions = false, Description = " list of library dirs, to be passed to linker with -L option")]
        public List<string> LibDirs { get; set; }

        [YamlMember(Alias = "ldflags", ApplyNamingConventions = false, Description = " additional linker flags for the target")]
        public List<string> Ldflags { get; set; }

        [YamlMember(Alias = "edc_files", ApplyNamingConventions = false, Description = " list of edc files")]
        public List<string> EdcFiles { get; set; }

        [YamlMember(Alias = "edc_images_dirs", ApplyNamingConventions = false, Description = " list of directories containing images, passed to edje_cc with -id option")]
        public List<string> EdcImagesDirs { get; set; }

        [YamlMember(Alias = "edc_sound_dirs", ApplyNamingConventions = false, Description = " list of directories containing sound files, passed to edje_cc with -sd option")]
        public List<string> EdcSoundDirs { get; set; }

        [YamlMember(Alias = "edc_font_dirs", ApplyNamingConventions = false, Description = " list of directories containing font files, passed to edje_cc with -fd option")]
        public List<string> EdcFontDirs { get; set; }

        [YamlMember(Alias = "po_files", ApplyNamingConventions = false, Description = " list of po files")]
        public List<string> PoFiles { get; set; }

        [YamlMember(Alias = "resources", ApplyNamingConventions = false, Description = " list of resource files, to be packed in tpk")]
        public List<string> Resources { get; set; }
    }

    public partial class ParseNativeYaml
    {
        public static ParseNativeYaml FromYaml(string ymlContents)
        {
            var deserializer = new DeserializerBuilder()
     .WithNamingConvention(CamelCaseNamingConvention.Instance)
     .Build();

            return deserializer.Deserialize<ParseNativeYaml>(ymlContents);
        }

        public static string ToYaml(ParseNativeYaml self)
        {
            var serializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
            var yaml = serializer.Serialize(self);
            return yaml;
        }
    }
}
