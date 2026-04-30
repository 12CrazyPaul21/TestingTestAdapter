namespace Phantom.Testing.TestAdapter.Debugging
{
    public interface IDebuggerAttacher
    {
        bool AttachDebugger(int processId);
    }
}
