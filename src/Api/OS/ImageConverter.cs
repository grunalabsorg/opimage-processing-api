using System.Diagnostics;

namespace Api.OS
{    
    public static class ImageConverter 
    {
        public static void MhaToDcm(string inputDir, string dicomReferenceFile)
        {
            var start = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"image_converter.py \"{inputDir}\" \"{dicomReferenceFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            Console.WriteLine("\narguments - "+start.Arguments);
            
            using (var process = Process.Start(start))
            {
                using (var reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);
                }

                using (var reader = process.StandardError)
                {
                    string error = reader.ReadToEnd();
                    Console.Write(error);
                }
            }            
        }
    }
}