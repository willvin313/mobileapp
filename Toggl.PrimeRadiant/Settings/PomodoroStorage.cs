using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Onboarding;

namespace Toggl.PrimeRadiant.Settings
{
    public sealed class PomodoroStorage : IPomodoroStorage
    {
        private const string pomodoroConfiguration = "PomodoroConfiguration";

        private readonly IKeyValueStorage keyValueStorage;

        public PomodoroStorage(IKeyValueStorage keyValueStorage)
        {
            Ensure.Argument.IsNotNull(keyValueStorage, nameof(keyValueStorage));

            this.keyValueStorage = keyValueStorage;
        }

        public string GetPomodoroConfigurationXml()
            => keyValueStorage.GetString(pomodoroConfiguration);

        public void Save(string xml)
        {
            keyValueStorage.SetString(pomodoroConfiguration, xml);
        }
    }
}
