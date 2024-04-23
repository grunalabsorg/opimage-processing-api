using Api.Common;
using Api.OS;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/analysis")]
    public class AnalysisController : ControllerBase
    {
        [DllImport("Libs/ProjetoContraste.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void analise(string path);

        private readonly SessionData _sessionData;

        public AnalysisController(SessionData sessionData)
        {
            _sessionData = sessionData;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Post(string studyId)
        {                      
            //var rootfolder = Directory.GetCurrentDirectory();
            //var assemblyLoc = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        
            FileInfo fileInfo = new FileInfo("Controllers/AnalysisController.cs");
            string drive = Path.GetPathRoot(fileInfo.FullName)!.Replace("\\","/");

            // studyId = 528d0ce9-e4b6d6b0-782e5f2f-c131cc1f-b621f554
            var requestid = _sessionData.RequestId;

            // TEMP VARS
            //var orthanc_server_url = "http://host.docker.internal:8042";
            var orthanc_server_url = "http://localhost:8042";
            var study_url = new Uri(orthanc_server_url + "/studies/" + studyId + "/archive");
            
            using (var client = new HttpClient())
            {
                // DOWNLOAD STUDY
                using (HttpResponseMessage response = await client.GetAsync(study_url))
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return NotFound();
                    }
                    
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return StatusCode((int)response.StatusCode);
                    }
                    
                    var imagesPath = drive + "/analysis/images";
                    Directory.CreateDirectory(imagesPath);

                    var studyFilename = Path.Join(imagesPath, $"/{requestid}_{studyId}.zip");

                    using (var responseFileStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var osFileStream = System.IO.File.OpenWrite(studyFilename))
                        {
                            responseFileStream.CopyTo(osFileStream);
                        }
                    }
                    
                    // EXTRACT STUDY
                    var zipFile = ZipFile.OpenRead(studyFilename);
                    FileManager.ExtractToDirectory(zipFile, imagesPath);                    

                    var entryFullName = Path.Join(imagesPath, zipFile.Entries.First().FullName);
                    var entryFullNameSplitted = entryFullName.Split('/');
                    
                    // aurea / rm mamas / vol1 / dicom

                    // verificando estudo dicom
                    if(!entryFullName.EndsWith(".dcm") && entryFullNameSplitted.Count() != 4)
                    {
                        return StatusCode((int)HttpStatusCode.UnprocessableEntity);
                    }

                    var volumesFolder = Path.GetDirectoryName(Path.GetDirectoryName(entryFullName));
                    var studyFolder = Path.GetDirectoryName(volumesFolder);

                    volumesFolder = volumesFolder.Replace("\\", "/");

                    // ANALYSIS
                    analise(volumesFolder!);
                    var analiseResultFolder = Path.Join(volumesFolder, "/RESULTADO");
                    var analiseResultZipFileName = volumesFolder + "/RESULTADO.zip";
                    ZipFile.CreateFromDirectory(analiseResultFolder, analiseResultZipFileName);

                    var newStudyUrl = new Uri(orthanc_server_url + "/instances");
                    // POST ANALYSIS
                    using (var fileStream = System.IO.File.Open(analiseResultZipFileName, FileMode.Open))
                    {
                        using(var sender = new HttpClient())
                        {  
                            HttpContent content = new StreamContent(fileStream);
                            var result = await sender.PostAsync(newStudyUrl, content);                                                                 
                            
                            if (!result.IsSuccessStatusCode)
                            {
                                return Problem();
                            }
                        }
                    }

                    System.IO.File.Delete(analiseResultZipFileName);
                    FileManager.DeleteDirectory(studyFolder!);
                    System.IO.File.Delete(studyFilename);
                }
            }            
            
            //var entries = Directory.GetFiles(Directory.GetCurrentDirectory());
            return Ok();
        }
    }
}
