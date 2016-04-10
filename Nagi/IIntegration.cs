namespace Nagi
{
    public interface IIntegration
    {
        string ActionMessage { get; }
        void Execute(string filePath);
    }
}