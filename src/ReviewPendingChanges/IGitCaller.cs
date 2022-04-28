namespace ReviewPendingChanges
{
    public interface IGitCaller
    {
        string[] GetStatus();
        void DiffTool(string file);
        void Add(string file);
        void Discard(string file);
        void NewFileDiff(string fileStatusFile);
        void Delete(string file);
    }
}
