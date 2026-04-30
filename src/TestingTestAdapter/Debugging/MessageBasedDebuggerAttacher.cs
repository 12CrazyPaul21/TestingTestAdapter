using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.ServiceModel;

namespace Phantom.Testing.TestAdapter.Debugging
{
    public class MessageBasedDebuggerAttacher : IDebuggerAttacher
    {
        private static readonly TimeSpan AttachDebuggerTimeout = TimeSpan.FromSeconds(10);

        private readonly string _debuggingNamedPipeId;
        private readonly TimeSpan _timeout;
        private readonly IMessageLogger _logger;

        public MessageBasedDebuggerAttacher(string debuggingNamedPipeId, TimeSpan timeout, IMessageLogger logger)
        {
            _debuggingNamedPipeId = debuggingNamedPipeId;
            _timeout = timeout;
            _logger = logger;
        }

        public MessageBasedDebuggerAttacher(string debuggingNamedPipeId, IMessageLogger logger) :
            this(debuggingNamedPipeId, AttachDebuggerTimeout, logger)
        {
        }

        public bool AttachDebugger(int processId)
        {
            try
            {
                var proxy = DebuggerAttacherServiceConfiguration.CreateProxy(_debuggingNamedPipeId, _timeout);
                using (var client = new DebuggerAttacherServiceProxyWrapper(proxy))
                {
                    client.Service.AttachDebugger(processId);
                    _logger.SendMessage(TestMessageLevel.Informational, $"Debugger attached to process {processId}");
                    return true;
                }
            }
            catch (FaultException<DebuggerAttacherServiceFault> serviceFault)
            {
                var errorMessage = serviceFault.Detail.Message;
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    errorMessage = $"Could not attach debugger to process {processId}";
                }
                _logger.SendMessage(TestMessageLevel.Error, errorMessage);
            }
            catch (Exception e)
            {
                _logger.SendMessage(TestMessageLevel.Error, $"Could not attach debugger to process {processId}:{Environment.NewLine}{e}");
            }
            return false;
        }
    }
}
