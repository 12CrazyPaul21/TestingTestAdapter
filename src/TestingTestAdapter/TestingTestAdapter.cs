using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Phantom.Testing.Common;
using System;

namespace Phantom.Testing.TestAdapter
{
    internal static class Constants
    {
        public const string SettingsName = "TestingTestAdapterSettings";
        public const string ExecutorUriString = "executor://testing-adapter";

        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
    }

    internal static class Property
    {
        internal static readonly TestProperty TestArgument = TestProperty.Register(
            id: "TestingTestAdapter.TestArgument",
            label: "Test Argument",
            valueType: typeof(string),
            TestPropertyAttributes.Hidden,
            typeof(TestCase)
        );

        internal static readonly TestProperty TestEnvironment = TestProperty.Register(
            id: "TestingTestAdapter.TestEnvironment",
            label: "Test Environment",
            valueType: typeof(string[]),
            TestPropertyAttributes.Hidden,
            typeof(TestCase)
        );
    }

    internal class MessageLoggerAdapter : ILogger
    {
        private readonly IMessageLogger _messageLogger;

        public MessageLoggerAdapter(IMessageLogger messageLogger)
        {
            _messageLogger = messageLogger;
        }

        public void LogDebug(string message)
        {
            _messageLogger.SendMessage(TestMessageLevel.Informational, $"[DEBUG] {message}");
        }

        public void LogError(string message)
        {
            _messageLogger.SendMessage(TestMessageLevel.Error, message);
        }

        public void LogInfo(string message)
        {
            _messageLogger.SendMessage(TestMessageLevel.Informational, message);
        }

        public void LogWarning(string message)
        {
            _messageLogger.SendMessage(TestMessageLevel.Warning, message);
        }
    }
}
