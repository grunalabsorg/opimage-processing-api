using System.IO.Compression;

namespace Api.OS
{
    public static class FileManager
    {
        public static void DeleteImagesFolders()
        {
            var imagesFolder = Path.Join(Directory.GetCurrentDirectory(), "/images");

            if (!Directory.Exists(imagesFolder))
                return;

            DeleteDirectory(imagesFolder);
        }

        private static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }
    }
}
