using Api.Common;
using Api.OS;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Runtime.InteropServices;
using System;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/analysis")]
    public class AnalysisController : ControllerBase
    {
        [DllImport("ProjetoContraste.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void analise(string path);

        private readonly SessionData _sessionData;

        public AnalysisController(SessionData sessionData)
        {
            _sessionData = sessionData;
        }

        [HttpPost("{studyId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Post(string studyId)
        {
            Console.WriteLine("\nReceived request");
            Console.WriteLine("StudyId " + studyId);

            var requestid = _sessionData.RequestId;
            string? requestFolder = null;

            FileInfo fileInfo = new FileInfo("Controllers/AnalysisController.cs");
            string drive = Path.GetPathRoot(fileInfo.FullName)!.Replace("\\","/");

            // TEMP VARS
            //var orthanc_server_url = "http://host.docker.internal:8042";
            //var orthanc_server_url = "http://localhost:8042"; // local orthanc 
            var orthanc_server_url = "http://localhost:8000"; // local orthanc werton 
            
            //var orthanc_server_url = "https://api.comunicaresolutions.com/orthanc/"; // remote orthanc

            var study_url = new Uri(orthanc_server_url + "/studies/" + studyId + "/archive");
            
            using (var client = new HttpClient())
            {
                var username = "test";
                var password = "test";

                var authenticationString = $"{username}:{password}";
                var base64String = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);

                // DOWNLOAD STUDY
                Console.WriteLine("\nDownloading study");
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

                    //CREATE FOLDERS
                    Console.WriteLine("\nCreating folders");
                    var imagesPath = drive + "/analysis/images";
                    requestFolder = Path.Join(imagesPath, $"/{requestid}");
                    Directory.CreateDirectory(requestFolder);

                    var studyFilename = Path.Join(requestFolder, $"/{studyId}.zip");

                    using (var responseFileStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var osFileStream = System.IO.File.OpenWrite(studyFilename))
                        {
                            responseFileStream.CopyTo(osFileStream);                            
                        }
                    }

                    // EXTRACT STUDY
                    Console.WriteLine("\nExtracting study");
                    var zipFile = ZipFile.OpenRead(studyFilename);
                    FileManager.ExtractToDirectory(zipFile, requestFolder);                    

                    var entryFullName = Path.Join(requestFolder, zipFile.Entries.First().FullName);
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
                    Console.WriteLine("\nAnalyzing");
                    analise(volumesFolder!);
                    var analiseResultFolder = Path.Join(volumesFolder, "/RESULTADO");
                    var analiseResultZipFileName = volumesFolder + "/RESULTADO.zip";                    
                    string[] allfiles = Directory.GetFiles(analiseResultFolder, "*.dcm*", SearchOption.AllDirectories);
                    FileManager.CreateZipFile(allfiles, analiseResultZipFileName);

                    // POST ANALYSIS
                    Console.WriteLine("\nUpload result");
                    var newStudyUrl = new Uri(orthanc_server_url + "/instances");

                    using (var fileStream = System.IO.File.Open(analiseResultZipFileName, FileMode.Open))
                    {
                        using(var sender = new HttpClient())
                        {
                            sender.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);

                            HttpContent content = new StreamContent(fileStream);
                            var result = await sender.PostAsync(newStudyUrl, content);                                                                 
                            
                            if (!result.IsSuccessStatusCode)
                            {
                                Console.WriteLine(result);
                                return Problem(statusCode: 400);
                            }
                        }
                    }                    
                }
            }

            //if(requestFolder is not null)
                //FileManager.DeleteDirectory(requestFolder);

            return Ok();
        }
    }
}
