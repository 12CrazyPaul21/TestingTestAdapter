using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Phantom.Testing.TestAdapter.Debugging
{
    [ServiceContract]
    public interface IDebuggerAttacherService
    {
        [OperationContract]
        [FaultContract(typeof(DebuggerAttacherServiceFault))]
        void AttachDebugger(int processId);
    }

    [DataContract]
    public class DebuggerAttacherServiceFault
    {
        [DataMember]
        public string Message { get; private set; }

        public DebuggerAttacherServiceFault(string message)
        {
            Message = message;
        }
    }

    public interface IDebuggerAttacherServiceWrapper : IDisposable
    {
        IDebuggerAttacherService Service { get; }
    }

    public class DebuggerAttacherServiceProxyWrapper : IDebuggerAttacherServiceWrapper
    {
        public DebuggerAttacherServiceProxyWrapper(IDebuggerAttacherService proxy)
        {
            Service = proxy;
        }

        public IDebuggerAttacherService Service { get; private set; }

        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        ((IClientChannel)Service).Close();
                    }
                    catch (CommunicationException)
                    {
                        ((IClientChannel)Service).Abort();
                    }
                    catch (Exception)
                    {
                        ((IClientChannel)Service).Abort();
                        throw;
                    }
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
