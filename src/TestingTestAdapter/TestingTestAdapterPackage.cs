using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Phantom.Testing.TestAdapter.Debugging;
using Phantom.Testing.TestAdapter.Settings;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Phantom.Testing.TestAdapter
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(TestingTestAdapterPackage.PackageGuidString)]
    public sealed class TestingTestAdapterPackage : AsyncPackage
    {
        /// <summary>
        /// TestingTestAdapterPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "d7f61b9e-56c2-45e7-8ff6-80b76d37fe7b";

        private readonly string _debuggingNamedPipeId = Guid.NewGuid().ToString();

        private IGlobalRunSettingsInternal _globalRunSettings;
        private DebuggerAttacherServiceHost _debuggerAttacherServiceHost;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var componentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            _globalRunSettings = componentModel?.GetService<IGlobalRunSettingsInternal>();

            DoInitialize();
        }

        private void DoInitialize()
        {
            InitializeSettings();
            InitializeDebuggerAttacherService();
        }

        private void InitializeSettings()
        {
            _globalRunSettings.TestingRunSettings = GetTestingRunSettings();
        }

        private void InitializeDebuggerAttacherService()
        {
            try
            {
                var debuggerAttacher = new VsDebuggerAttacher(this);
                _debuggerAttacherServiceHost = new DebuggerAttacherServiceHost(_debuggingNamedPipeId, debuggerAttacher);
                _debuggerAttacherServiceHost.Open();
            }
            catch (Exception e)
            {
                _debuggerAttacherServiceHost?.Abort();
                _debuggerAttacherServiceHost = null;
                Debug.WriteLine($"failed to start debugger attacher service:{Environment.NewLine}{e}");
            }
        }

        private TestingRunSettings GetTestingRunSettings()
        {
            return new TestingRunSettings
            {
                DebuggingNamedPipeId = _debuggingNamedPipeId
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _debuggerAttacherServiceHost?.Close();
                }
                catch (CommunicationException)
                {
                    _debuggerAttacherServiceHost?.Abort();
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
