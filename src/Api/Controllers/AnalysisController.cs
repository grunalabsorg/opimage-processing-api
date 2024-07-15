﻿using Api.Common;
using Api.Models;
using Api.OS;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Collections.Immutable;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/analysis")]
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
            var orthanc_server_url = "http://localhost:8000"; // local orthanc            
            //var orthanc_server_url = "https://api.comunicaresolutions.com/orthanc/"; // remote orthanc
            
            var orthancUsername = "test";
            var orthancPassword = "test";
            var orthancAuthString = $"{orthancUsername}:{orthancPassword}";
            var orthancAuthBase64String = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(orthancAuthString));

            //var referenceSeriesId = seriesIds[0];
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
            //
            var imagesPath = "/analysis/images";            
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
                    
                    using (var responseFileStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var osFileStream = System.IO.File.OpenWrite(seriesFilename))
                        {
                            responseFileStream.CopyTo(osFileStream);
                        }
                    }     

                    // Extract series
                    //cannot read
                    var zipFile = ZipFile.OpenRead(seriesFilename);
                    zipFile.ExtractToDirectory(studyFolder);               
                }    

                // Delete all zip folders
                //
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
                var analiseFolder = Path.GetDirectoryName(Path.GetDirectoryName(fullnameReference));
                
                // Copy series to study folder
                analise(analiseFolder);
                var analiseResultFolder = Path.Join(analiseFolder, "/RESULTADO");
                var analiseResultZipFileName = analiseFolder + ".zip";
                ZipFile.CreateFromDirectory(analiseResultFolder, analiseResultZipFileName);
            
                // Post analysis
                using (var fileStream = System.IO.File.Open(analiseResultZipFileName, FileMode.Open))
                {
                    var study_url_upload = orthanc_server_url + "/study/" + referenceParentStudyId + "/archive";

                    using(var sender = new HttpClient())
                    {
                        HttpContent content = new StreamContent(fileStream);
                        await sender.PostAsync(study_url_upload, content);
                    }
                }
                
                Directory.Delete(studyFolder);                                        
            }
            
            return Ok();
        }            
    
        private static void CopyFilesAndFolders(string sourcePath, string targetPath)
        {
            try
            {
                // Get all directories in the source root
                string[] directories = Directory.GetDirectories(sourcePath);

                foreach (var dir in directories)
                {
                    // Get the name of the current directory
                    string directoryName = new DirectoryInfo(dir).Name;

                    // Define the target directory path in the destination root
                    string targetDir = Path.Combine(targetPath, directoryName);

                    // Create the target directory if it does not exist
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // Copy all files from the current source directory to the target directory
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        string targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
                        System.IO.File.Copy(file, targetFilePath, overwrite: true);
                    }
                }

                Console.WriteLine("All folders copied successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}