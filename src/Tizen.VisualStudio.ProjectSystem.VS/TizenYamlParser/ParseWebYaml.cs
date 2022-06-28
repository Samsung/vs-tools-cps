using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Tizen.VisualStudio.TizenYamlParser
{
    public partial class ParseWebYaml
    {
        [YamlMember(Alias = "auto_gen_build_files", ApplyNamingConventions = false, Description = " Enable auto build file generation")]
        public bool AutoGenBuildFiles { get; set; }

        [YamlMember(Alias = "project_type", ApplyNamingConventions = false, Description = " Project type [web_app, test_runner]")]
        public string ProjectType { get; set; }

        [YamlMember(Alias = "output_name", ApplyNamingConventions = false, Description = " Output name for application")]
        public string OutputName { get; set; }

        [YamlMember(Alias = "trust_anchor", ApplyNamingConventions = false, Description = " list of certs in web project (.trust-anchor)")]
        public List<string> TrustAnchor { get; set; }

        [YamlMember(Alias = "files", ApplyNamingConventions = false, Description = " list of files in web project (.html,.css etc) and resources")]
        public List<string> Files { get; set; }

        [YamlMember(Alias = "excludes", ApplyNamingConventions = false, Description = " list of files to exclude based on the matched patterns")]
        public List<string> Excludes { get; set; }
    }

    public partial class ParseWebYaml
    {
        public static ParseWebYaml FromYaml(string ymlContents)
        {
            var deserializer = new DeserializerBuilder()
     .WithNamingConvention(CamelCaseNamingConvention.Instance)
     .Build();

            return deserializer.Deserialize<ParseWebYaml>(ymlContents);
        }

        public static string ToYaml(ParseWebYaml self)
        {
            var serializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
            var yaml = serializer.Serialize(self);
            return yaml;
        }
    }
}
