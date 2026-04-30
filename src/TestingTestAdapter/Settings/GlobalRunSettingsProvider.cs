using System.Composition;

namespace Phantom.Testing.TestAdapter.Settings
{
    [Shared]
    [Export(typeof(IGlobalRunSettings))]
    [Export(typeof(IGlobalRunSettingsInternal))]
    internal class GlobalRunSettingsProvider : IGlobalRunSettingsInternal
    {
        public TestingRunSettings TestingRunSettings { get; set; }
    }
}
