namespace Phantom.Testing.TestAdapter.Settings
{
    internal interface IGlobalRunSettingsInternal : IGlobalRunSettings
    {
        new TestingRunSettings TestingRunSettings { get; set; }
    }
}
