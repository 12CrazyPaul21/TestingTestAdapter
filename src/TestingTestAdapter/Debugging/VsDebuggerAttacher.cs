using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Phantom.Testing.TestAdapter.Debugging
{
    // https://github.com/csoltenborn/GoogleTestAdapter/blob/master/GoogleTestAdapter/VsPackage.Shared/Debugging/VsDebuggerAttacher.cs
    public class VsDebuggerAttacher : IDebuggerAttacher
    {
        private const int AttachRetryWaitingTimeInMs = 100;
        private const int MaxAttachTries = 10;

        private readonly IServiceProvider _serviceProvider;

        public VsDebuggerAttacher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool AttachDebugger(int processId)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            IntPtr pDebugEngine = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Guid)));
            try
            {
                Marshal.StructureToPtr(VSConstants.DebugEnginesGuids.NativeOnly_guid, pDebugEngine, false);

                var debugTarget = new VsDebugTargetInfo4
                {
                    dlo = (uint)DEBUG_LAUNCH_OPERATION.DLO_AlreadyRunning | (uint)_DEBUG_LAUNCH_OPERATION4.DLO_AttachToSuspendedLaunchProcess,
                    dwProcessId = (uint)processId,
                    dwDebugEngineCount = 1,
                    pDebugEngines = pDebugEngine
                };
                var debugger = (IVsDebugger4)_serviceProvider.GetService(typeof(SVsShellDebugger));
                AttachDebuggerRetrying(debugger, debugTarget);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pDebugEngine);
            }

            return true;
        }

        public static void AttachDebuggerRetrying(IVsDebugger4 debugger, VsDebugTargetInfo4 debugTarget)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            int tries = 0;
            while (true)
            {
                try
                {
                    debugger.LaunchDebugTargets4(1, new[] { debugTarget }, new VsDebugTargetProcessInfo[1]);
                    break;
                }
                catch (Exception e)
                {
                    tries++;
                    if (tries == MaxAttachTries)
                    {
                        Debug.WriteLine(e);
                        throw;
                    }

                    Thread.Sleep(AttachRetryWaitingTimeInMs);
                }
            }
        }
    }
}
