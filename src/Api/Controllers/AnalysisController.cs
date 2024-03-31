using Api.Common;
using Api.OS;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;

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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Post(string studyId)
        {
            // studyId = 528d0ce9-e4b6d6b0-782e5f2f-c131cc1f-b621f554
            var requestid = _sessionData.RequestId;

            // TEMP VARS
            // var orthanc_server_url = "http://host.docker.internal:8042";
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
                    
                    var imagesPath = Path.Join(Directory.GetCurrentDirectory(), "/images");
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
                    zipFile.ExtractToDirectory(imagesPath);

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

                    // ANALYSIS
                    analise(volumesFolder);
                    var analiseResultFolder = Path.Join(volumesFolder, "/RESULTADO");
                    var analiseResultZipFileName = entryFullNameSplitted + ".zip";
                    ZipFile.CreateFromDirectory(analiseResultFolder, analiseResultZipFileName);

                    // POST ANALYSIS
                    using (var fileStream = System.IO.File.Open(analiseResultZipFileName, FileMode.Open))
                    {
                        using(var sender = new HttpClient())
                        {
                            HttpContent content = new StreamContent(fileStream);
                            await sender.PostAsync(study_url, content);
                        }
                    }

                    System.IO.File.Delete(analiseResultZipFileName);
                    Directory.Delete(studyFolder);
                    System.IO.File.Delete(studyFilename);
                }
            }            

            return Ok(Directory.GetCurrentDirectory());
        }
    }
}
