using System;
using System.ServiceModel;

namespace Phantom.Testing.TestAdapter.Debugging
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class DebuggerAttacherService : IDebuggerAttacherService
    {
        private readonly IDebuggerAttacher _debuggerAttacher;

        public DebuggerAttacherService(IDebuggerAttacher debuggerAttacher)
        {
            _debuggerAttacher = debuggerAttacher;
        }

        public void AttachDebugger(int processId)
        {
            bool success;
            try
            {
                success = _debuggerAttacher.AttachDebugger(processId);
            }
            catch (Exception e)
            {
                throw new FaultException<DebuggerAttacherServiceFault>(new DebuggerAttacherServiceFault(
                    $"Could not attach debugger to process {processId} because of exception on the server side:{Environment.NewLine}{e}"
                ));
            }
            if (!success)
            {
                throw new FaultException<DebuggerAttacherServiceFault>(new DebuggerAttacherServiceFault(
                    $"Could not attach debugger to process {processId} for unknown reasons"
                ));
            }
        }
    }

    public class DebuggerAttacherServiceHost : ServiceHost
    {
        public DebuggerAttacherServiceHost(string id, IDebuggerAttacher debuggerAttacher) :
            base(new DebuggerAttacherService(debuggerAttacher), new Uri[] {
                DebuggerAttacherServiceConfiguration.CreateAddress(id)
            })
        {
            AddServiceEndpoint(typeof(IDebuggerAttacherService), new NetNamedPipeBinding(), DebuggerAttacherServiceConfiguration.InterfaceAddress);
        }
    }
}
