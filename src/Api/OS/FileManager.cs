using System.IO.Compression;

namespace Api.OS
{    
    public static class FileManager
    {
        public static string GetDrive()
        {
            FileInfo fileInfo = new FileInfo("Controllers/AnalysisController.cs");
            return Path.GetPathRoot(fileInfo.FullName)!.Replace("\\", "/");
        }
        
        public static void ExtractToDirectory(ZipArchive zipArchive, string destinationDirName)
        {
            zipArchive.ExtractToDirectory(destinationDirName, true);
        }

        public static void DeleteDirectory(string target_dir)
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

        public static void CreateZipFile(string[] files, string zipFilePath)
        {            
            using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {                
                foreach (var file in files)
                {
                    zip.CreateEntryFromFile(file, Path.GetFileName(file));
                }
            }
        }
    }
}
