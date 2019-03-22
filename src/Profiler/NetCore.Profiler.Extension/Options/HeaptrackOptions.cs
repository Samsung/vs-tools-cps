using System;
using NetCore.Profiler.Extension.Common;

namespace NetCore.Profiler.Extension.Options
{
    public class HeaptrackOptions : NotifyPropertyChanged
    {
        private readonly SettingsStore _settingsStore;

        public HeaptrackOptions(SettingsStore settingsStore)
        {
            if (settingsStore == null)
            {
                throw new ArgumentNullException(nameof(settingsStore));
            }

            _settingsStore = settingsStore;
            LoadSettings();
        }

        public void LoadSettings()
        {
        }
    }
}
