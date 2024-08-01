﻿using Api.Common;
using Api.Models;
using Api.OS;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Api.Controllers
{
    [ApiController]
    [Route("/analysis")]
    public class AnalysisController : ControllerBase
    {
        [DllImport("libProjetoContraste.so", CallingConvention = CallingConvention.Cdecl)]
        static extern void analise(string path);

        private readonly SessionData _sessionData;

        public AnalysisController(SessionData sessionData)
        {
            _sessionData = sessionData;
        }
        
        [HttpPost("test")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Post() 
        {
            Console.WriteLine("Received test request");
            return Ok();
        }

        /// <summary>
        /// Analyze series
        /// </summary>
        /// <param name="request">Request body with series id's</param>
        /// <response code="204">Series analyzed</response>          
        /// <response code="422">Minimum of 4 series | Series instances count mismatch | Series parent study mismatch</response>        
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Post([FromBody] string[] seriesIds)
        {       
            var requestid = _sessionData.RequestId;

            Console.WriteLine("\nReceived request");
            Console.WriteLine("Series " + seriesIds);

            // Temp vars
            var orthanc_server_url = "http://orthanc:8042"; // local orthanc  
            //var orthanc_server_url = "https://api.op-image.com/orthanc/"; // remote orthanc                                           
            
            var orthancUsername = "test";
            var orthancPassword = "test";
            var orthancAuthString = $"{orthancUsername}:{orthancPassword}";
            var orthancAuthBase64String = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(orthancAuthString));
  
            int? referenceSeriesInstancesCount = null;
            string? referenceParentStudyId = null;

            // Get and check info from series

            if (seriesIds.Length < 4)            
                return BadRequest("Minimum of 4 series");
            
            using(var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", orthancAuthBase64String);

                foreach(var seriesId in seriesIds)
                {
                    var response = await client.GetAsync(orthanc_server_url + "/series/" + seriesId);                    
                    var seriesInfo = JsonSerializer.Deserialize<SeriesInfoRoot>(await response.Content.ReadAsStringAsync());
                    
                    if(referenceSeriesInstancesCount is null && referenceParentStudyId is null)
                    {
                        referenceSeriesInstancesCount = seriesInfo.Instances.Count;
                        referenceParentStudyId = seriesInfo.ParentStudy;
                    }                    
                    else
                    {
                        if(referenceSeriesInstancesCount != seriesInfo.Instances.Count)
                        {
                            //return BadRequest("Series instances count mismatch");
                            Console.WriteLine("Series instances count mismatch");
                        }
                        
                        if(referenceParentStudyId != seriesInfo.ParentStudy)
                        {
                            return BadRequest("Series parent study mismatch");
                        }                        
                    }                             
                } 

                Console.WriteLine("\nValidated series");
            }
    
            // Download series            
            Console.WriteLine("\nDonwload series");

            var imagesPath = "/analysis/images";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                imagesPath = "C:/analysis/images";
            }           

            var studyFolder = Path.Join(imagesPath, $"/{requestid}_{referenceParentStudyId}");
            Directory.CreateDirectory(studyFolder);

            using(var client = new HttpClient())
            {                
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", orthancAuthBase64String);
                
                foreach(var seriesId in seriesIds) 
                {                    
                    var series_url_download = orthanc_server_url + "/series/" + seriesId + "/archive";

                    var response = await client.GetAsync(series_url_download);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                        return NotFound();

                    if (response.StatusCode != HttpStatusCode.OK)                
                        return StatusCode((int)response.StatusCode);
                    
                    var seriesFilename = Path.Join(studyFolder, $"/{seriesId}.zip");
                    Console.WriteLine(seriesFilename);
                    
                    using (var responseFileStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var osFileStream = System.IO.File.OpenWrite(seriesFilename))
                        {
                            responseFileStream.CopyTo(osFileStream);
                        }
                    }     

                    // Extract series                    
                    Console.WriteLine("\nExtract series");
                    //var zipFile = ZipFile.OpenRead(seriesFilename);
                    //zipFile.ExtractToDirectory(studyFolder);   
                    FileManager.ExtractToDirectory(seriesFilename, studyFolder);            
                }    

                // Delete all zip folders            
                var zipFiles = Directory.GetFiles(studyFolder, "*.zip");

                foreach(var file in zipFiles) 
                {
                    System.IO.File.Delete(file);
                }
                              
                // Check files in the current directory
                var files = Directory.GetFiles(studyFolder, "*.*", SearchOption.AllDirectories);
                
                if(files.Count() == 0)
                    return StatusCode((int)HttpStatusCode.UnprocessableEntity);
                
                var fullnameReference = files.First();
                var fullnameReferenceSplitted = fullnameReference.Split('/');
                
                // verificando estudo dicom
                if(!fullnameReference.EndsWith(".dcm") && fullnameReferenceSplitted.Count() != 8)
                    return StatusCode((int)HttpStatusCode.UnprocessableEntity);
                
                // TODO! Refactor to exclude patient and study folder
                var analiseDir = Path.GetDirectoryName(Path.GetDirectoryName(fullnameReference));
                
                // Copy series to study folder
                Console.WriteLine("\nStart analysis");
                analise(analiseDir);
                var analiseResultFolder = Path.Join(analiseDir, "/RESULTADO");

                // Convert mha to dcm
                var referenceDcmFilename = Directory.GetFiles(Directory.GetDirectories(analiseDir).First()).First();                                                                                                                                          
                ImageConverter.MhaToDcm(analiseResultFolder, referenceDcmFilename);
                var analiseResultZipFileName = analiseResultFolder + ".zip";                
                ZipFile.CreateFromDirectory(analiseResultFolder, analiseResultZipFileName);
                                
                // Post analysis
                Console.WriteLine("\nUpload analysis");
                Console.WriteLine("\n zip filename"+analiseResultZipFileName);                
                    using (var fileStream = System.IO.File.Open(analiseResultZipFileName, FileMode.Open))
                    {
                        var series_url_upload = orthanc_server_url + "/instances";
                        Console.WriteLine("POST "+series_url_upload);

                        using(var sender = new HttpClient())
                        {
                            sender.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", orthancAuthBase64String);

                            HttpContent content = new StreamContent(fileStream);
                            var response = await sender.PostAsync(series_url_upload, content);
                            Console.WriteLine(response);
                        }
                    }
                
                FileManager.DeleteDirectory(studyFolder);                                        
            }
            
            return Ok();
        }                    
    }
}