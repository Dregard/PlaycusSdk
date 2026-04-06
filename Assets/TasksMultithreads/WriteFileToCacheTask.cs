using System.IO;


namespace Playcus.Assets
{
    /// <summary>
    /// multithreading task for file writing in cache
    /// </summary>
    public class WriteFileToCacheTask
    {
        private string assetAdress;
        private byte[] assetBytes;

        public WriteFileToCacheTask(string assetAdress, byte[] assetBytes)
        {
            this.assetAdress = assetAdress;
            this.assetBytes = assetBytes;
        }

        public void ThreadProc()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetAdress));
            File.WriteAllBytes(assetAdress, assetBytes);
        }
    }


}
