using Microsoft.VisualStudio.Editor.OptionDescriptions;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.Composition;
using System.Xml.XPath;

namespace Phantom.Testing.TestAdapter.Settings
{
    [Export(typeof(IRunSettingsService))]
    [SettingName(Constants.SettingsName)]
    public class RunSettingsService : IRunSettingsService
    {
        public string Name => Constants.SettingsName;

        private readonly IGlobalRunSettings _globalRunSettings;

        [ImportingConstructor]
        public RunSettingsService(IGlobalRunSettings globalRunSettings)
        {
            _globalRunSettings = globalRunSettings;
        }

        public IXPathNavigable AddRunSettings(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo, ILogger logger)
        {
            XPathNavigator navigator = inputRunSettingDocument.CreateNavigator();
            if (!navigator.MoveToChild(Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants.RunSettingsName, ""))
            {
                logger.Log(MessageLevel.Warning, "RunSettingsDocument does not contain a RunSettings node");
                return navigator;
            }

            var settings = new TestingRunSettings();
            if (navigator.MoveToChild(Constants.SettingsName, ""))
            {
                settings.FillMissingFrom(TestingRunSettings.LoadFromXml(navigator));
                navigator.DeleteSelf();
            }

            var internalSettings = _globalRunSettings.TestingRunSettings;
            if (internalSettings != null)
            {
                settings.FillMissingFrom(internalSettings);
            }

            navigator.MoveToChild(Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants.RunSettingsName, "");
            navigator.AppendChild(settings.ToXml().CreateNavigator());

            navigator.MoveToRoot();
            return navigator;
        }
    }
}
