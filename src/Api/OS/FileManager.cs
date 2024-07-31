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
        
        public static void ExtractToDirectory(string zipFileName, string destinationDirName)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFileName))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destinationPath = Path.Combine(destinationDirName, entry.FullName);

                    // Check if the file or directory already exists
                    if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                    {
                        destinationPath = GetUniquePath(destinationPath);
                    }

                    // Ensure the directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? string.Empty);

                    // Extract the file
                    entry.ExtractToFile(destinationPath);
                }
            }
        }

        private static string GetUniquePath(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(path);            

            int counter = 1;
            string uniquePath;
            
            do
            {
                uniquePath = Path.Combine(directory+$" ({counter})", fileName);
                counter++;
            }
            while (File.Exists(uniquePath) || Directory.Exists(uniquePath));

            return uniquePath;
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
