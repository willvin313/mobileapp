using System;

namespace Toggl.PrimeRadiant.Settings
{
    public interface IPomodoroStorage
    {
        string GetPomodoroConfigurationXml();
        void Save(string xml);
    }
}
