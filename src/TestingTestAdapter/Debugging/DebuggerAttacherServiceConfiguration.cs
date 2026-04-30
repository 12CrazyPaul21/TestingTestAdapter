using System;
using System.Globalization;
using System.ServiceModel;

namespace Phantom.Testing.TestAdapter.Debugging
{
    public static class DebuggerAttacherServiceConfiguration
    {
        public static readonly Uri InterfaceAddress = new Uri(nameof(IDebuggerAttacherService), UriKind.Relative);

        public static Uri CreateAddress(string id)
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, "net.pipe://localhost/TTA_{0}/", id));
        }

        public static IDebuggerAttacherService CreateProxy(string id, TimeSpan timeout)
        {
            var binding = new NetNamedPipeBinding()
            {
                OpenTimeout = timeout,
                CloseTimeout = timeout,
                SendTimeout = timeout,
                ReceiveTimeout = timeout
            };
            var endpointUri = new Uri(CreateAddress(id), InterfaceAddress);
            var endpointAddress = new EndpointAddress(endpointUri);
            return ChannelFactory<IDebuggerAttacherService>.CreateChannel(binding, endpointAddress);
        }
    }
}
