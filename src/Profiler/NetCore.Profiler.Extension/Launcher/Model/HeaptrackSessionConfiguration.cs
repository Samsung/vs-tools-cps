using System.IO;
using EnvDTE;
using NetCore.Profiler.Extension.Options;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    public class HeaptrackSessionConfiguration : AbstractSessionConfiguration
    {       

        public HeaptrackSessionConfiguration(Project project, HeaptrackOptions hto) : base(project)
        {
            ReadProjectConfiguration();

            ProjectHostBinPath = Path.Combine(ProjectHostPath, ProjectOutputPath);
        }

        private void ReadProjectConfiguration()
        {
            if (Project.ConfigurationManager != null)
            {
                Configuration config = Project.ConfigurationManager.ActiveConfiguration;
                SetOutputPath(config);
            }
        }
    }
}
