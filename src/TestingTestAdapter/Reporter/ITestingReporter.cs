using System;

namespace Phantom.Testing.TestAdapter.Reporter
{
    internal interface ITestingReporter
    {
        void RecordStart();
        void RecordEnd();

        void RecordResult(int exitcode, string stdout, string sterr);
        void RecordException(Exception exception);
    }
}
