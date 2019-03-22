using NetCore.Profiler.Extension.VSPackage;
using System;

namespace NetCore.Profiler.Extension.Commands
{
    internal sealed class HeaptrackWindowCommand : Command
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0116;

        public static Command Instance { get; private set; }

        private HeaptrackWindowCommand(IServiceProvider serviceProvider) : base(serviceProvider, CommandId, (sender, args) => ProfilerPlugin.Instance.ExplorerWindow.Show(), true)
        {
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            Instance = new HeaptrackWindowCommand(serviceProvider);
        }
    }
}
