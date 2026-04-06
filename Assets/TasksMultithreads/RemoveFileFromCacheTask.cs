using System.IO;

namespace Playcus.Assets
{
    /// <summary>
    /// multithreading task for delete file from cache
    /// </summary>
    public class RemoveFileFromCacheTask
    {
        private string fileAddress;

        public RemoveFileFromCacheTask(string fileAddress)
        {
            this.fileAddress = fileAddress;
        }

        public void ThreadProc()
        {
            if (File.Exists(fileAddress))
                File.Delete(fileAddress);
        }
    }


}
