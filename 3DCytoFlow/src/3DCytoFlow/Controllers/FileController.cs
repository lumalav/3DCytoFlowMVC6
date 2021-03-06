﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Extensions.OptionsModel;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using _3DCytoFlow.Models;
using _3DCytoFlow.Services;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace _3DCytoFlow.Controllers
{
    public class FileController : Controller
    {
        public UserManager<ApplicationUser> Manager { get; }
        private readonly ApplicationDbContext _context;
        private readonly string _storageConnectionString;
        private readonly ISmsSender _smsSender;
        private readonly string _sid;
        private readonly string _authToken;
        private readonly string _number;

        public FileController(ApplicationDbContext context, IOptions<StorageSettings> options, UserManager<ApplicationUser> userManager, ISmsSender smsSender, IOptions<SMSSettings> smsOptions)
        {
            Manager = userManager;
            _context = context;
            _smsSender = smsSender;
            _storageConnectionString = options.Value.StorageStringConnection;
            _sid = smsOptions.Value.Sid;
            _authToken = smsOptions.Value.Token;
            _number = smsOptions.Value.Number;
        }

        /// <summary>
        /// Returns login information for the vm from the web server
        /// </summary>
        /// <param></param>
        /// <param name="username"></param>
        /// <param name="psw"></param>
        /// <returns>LoginID?</returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetToken([FromQuery]string username, string psw)
        {
            var vm = _context.VirtualMachines.First(x => x.MachineName == username);

            if (vm != null)
            {
                if (PasswordHash.ValidatePassword(psw, vm.HashedPassword))
                {
                    return Json(vm.Id);
                }
                return HttpBadRequest();
            }

            return HttpNotFound();
        }
        
        /// <summary>
        // gets vm id
        // assigns analysis that has no VM to that VM
        // gives the filepath of the .fcs file linked to that analysis
        /// </summary>
        /// <param name="vmId"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult RequestAnalysis(string vmId)
        {
            var r = new RequestResponse();
            VirtualMachine vm;
            if (_context.VirtualMachines.Any(x => x.Id == vmId))
            {
                vm = _context.VirtualMachines.First(x => x.Id == vmId);
            }
            else
            {
                return HttpBadRequest();
            }

            // go through each analysis in the database and find the one that has a null virtual machine
            // return the filepath of the .fcs linked to that analysis

            // json object with found/notfound and the fcsFilePath
            // if found vm looks into it


            var analyses = _context.Analyses.Include(i => i.VirtualMachine);
            Analysis analysis = null;
            foreach (var a in analyses)
            {
                if (a.VirtualMachine == null && string.IsNullOrEmpty(a.ResultFilePath))
                {
                    analysis = a;
                    r.FileLocation = analysis.FcsFilePath;
                    r.Found = true;
                }
            }

            if (analysis != null)
            {
                vm.Analysis = analysis;
                _context.SaveChanges();
                r.Jobs = vm.Jobs;
                r.Points = vm.PointsToCalculate;

                return Json(r);
            }

            return Json(r);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult UpdateETC(string vmId, long? totalSeconds)
        {
            // Check if an analysis is found that matches vmID
            if (_context.VirtualMachines.Select(i => i.Analysis).Any(i => i.VirtualMachine.Id == vmId))
            {
                // if found then change the location to the given string and update the db
                var a = _context.VirtualMachines.Select(i => i.Analysis).First(i => i.VirtualMachine.Id == vmId);
                var analysis = _context.Analyses.Include(i => i.User).First(i => i.Id == a.Id);

                if( analysis != null )
                {
                    VirtualMachine vm = _context.VirtualMachines.First(i => i.Id == vmId);

                    if( totalSeconds != null )
                    {
                        int days = (int)totalSeconds / 60 / 60 / 24;
                        int hours = (int)(totalSeconds / 60 / 60) % 24;
                        int minutes = (int)(totalSeconds / 60) % 60;
                        int seconds = (int)(totalSeconds % 60);

                        vm.CompletionDate = DateTime.Now.Add(new TimeSpan(days, hours, minutes, seconds, 0));
                        _context.VirtualMachines.Update(vm);
                        _context.SaveChanges();

                        return Ok();
                    }
                    return HttpBadRequest();
                }
                return HttpBadRequest();
            }
            return HttpNotFound();
        }


        /// <summary>
        /// receive analysisID and loation
        /// check if the VM is linked that analyss
        /// if so, put the location into the analysis
        /// if not, then cry
        /// </summary>
        /// 
        [HttpGet]
        [AllowAnonymous]   
        public ActionResult AnalysisFinished(string vmId, string location)
        {

            Analysis analysis;
            // Check if an analysis is found that matches vmID
            if (_context.VirtualMachines.Select(i => i.Analysis).Any(i => i.VirtualMachine.Id == vmId))
            {
                // if found then change the location to the given string and update the db
                analysis = _context.VirtualMachines.Select(i => i.Analysis).FirstOrDefault(i => i.VirtualMachine.Id == vmId);

                if (analysis != null)
                {
                    var vm = _context.VirtualMachines.FirstOrDefault(i => i.Analysis.Id == analysis.Id);
                    vm.Analysis = null;

                    //TODO: Fix
                    vm.CompletionDate = null;

                    analysis.ResultFilePath = location;
                    _context.Analyses.Update(analysis);
                    _context.VirtualMachines.Update(vm);
                    _context.SaveChanges();

                    //send message to the user
                    var phone = "";
                    foreach (var user in _context.Users)
                    {
                        if (user.Analyses.FirstOrDefault(i => i.Id == analysis.Id) != null)
                        {
                            phone = user.Phone;
                        }
                    }

                    _smsSender.SendSms(new AuthMessageSender.Message{
                        Body = "Greetings" + "\nAn analysis that you recently requested has been completed"+ "\n" +
                               "Please, login to 3DCytoFlow to see the results\n"
                    }, _sid, _authToken, _number, phone);
                }

                // return something else here? Not sure what to return for working result
                return Ok();
            }
            // if the analysis does not exist
            // this should not be triggered
            return HttpBadRequest();
        }


        //
        // GET: /Account/UploadFile
        public ActionResult UploadFile()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = GetUser();

                var model = new UploadFileModel { Patients = _context.Patients.Where(x => x.User.UserName == user.UserName).ToArray() };

                return View(model);
            }

            return RedirectToAction("LogIn", "Account");
        }

        /// <summary>
        /// returns a patient with the provided name
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns></returns>
        private Patient GetPatient(string firstName, string lastName)
        {
            return _context.Patients.First(i => i.FirstName.Equals(firstName) && i.LastName.Equals(lastName));
        }

        /// <summary>
        /// Downloads all the json files from the storage and saves them in the Results folder
        /// </summary> 
        [HttpPost]
        public async Task<ActionResult> DownloadResult(string analysisId)
        {
            var analysis = _context.Analyses.FirstOrDefault(i => i.Id == int.Parse(analysisId));
            var vm = _context.VirtualMachines.Include(i => i.Analysis).FirstOrDefault(i => i.Analysis.Id == analysis.Id);

            if (analysis == null) return Json(false);

            var path = analysis.ResultFilePath;

            if (string.IsNullOrWhiteSpace(path) && vm != null) return Json(new {ETC = vm.CompletionDate});
            if (string.IsNullOrWhiteSpace(path) && vm == null) return Json(new {message = "Waiting to be analyzed"});

            var jsonString = "";
            //prepare container name
            var index = path.IndexOf('/');

            var containerName = path.Substring(0, index);

            //prepare blobName
            var blobName = path.Substring(index + 1);

            // Retrieve storage account from web.config
            var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);

            //retrieve container
            var container = await GetContainer(storageAccount, containerName);

            var blobs = new List<IListBlobItem>();

            //List blobs and directories in this container
            BlobContinuationToken token = null;
            do
            {
                var result = await container.ListBlobsSegmentedAsync("", true, BlobListingDetails.None, 500, token, null, null);
                token = result.ContinuationToken;
                blobs.AddRange(result.Results);
                //Now do something with the blobs
            } while (token != null);

            //prepare the location
            foreach (var blob in blobs.Where(i => i.Uri.ToString().Contains(blobName)).Cast<CloudBlockBlob>())
            {
                using (var stream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(stream);

                    stream.Position = 0;

                    var serializer = new JsonSerializer();

                    using (var sr = new StreamReader(stream))
                    {
                        using (var jsonTextReader = new JsonTextReader(sr))
                        {
                            var result = serializer.Deserialize(jsonTextReader);
                            jsonString = JsonConvert.SerializeObject(result);
                        }
                    }
                }
            }

            return Content(jsonString);
        }

        /// <summary>
        /// Prepares the storage that will receive the .fcs file
        /// </summary>
        /// <param name="blocksCount"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> SetMetadata(int blocksCount, string fileName, long fileSize, string patient)
        {
   //         var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);

            var patientCompleteName = patient.Split(' ');

            var firstName = patientCompleteName[0];
            var lastName = patientCompleteName[1];

            //container name will be lastname-name-id of the user. Everything in lowercase or Azure complains with a 400 error
            var user = GetUser();
            //var containerName = user.LastName + "-" + user.FirstName + "-" + user.Id;
            var containerName = user.LastName.ToLower() + "-" + user.FirstName.ToLower();
            //    var container = await GetContainer(storageAccount, containerName.ToLower());

            //get the patient
            var storedPatient = GetPatient(firstName, lastName);

            //blob exact name and location
            var blobName = lastName + "-" + firstName + "/" + DateTime.Now.ToString("MM-dd-yyyy-hh-mm") + ".fcs";

            //filename will be lastname-name-uploaddate.fcs of the patient
            var fileToUpload = new CloudFile()
            {
                OriginalFileName = fileName,
                Patient = storedPatient.Id,
                BlockCount = blocksCount,
                FileName = blobName.ToLower(),
                Size = fileSize,
                ContainerName = containerName,
                BlobName = blobName.ToLower(),
                StartTime = DateTime.Now,
                IsUploadCompleted = false,
                UploadStatusMessage = string.Empty
            };

            var fileByteArray = GetBytes(JsonConvert.SerializeObject(fileToUpload));

            HttpContext.Session.Set("CurrentFile", fileByteArray);

            return Json(true);
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        /// <summary>
        /// Uploads a chunk
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        // [ValidateInput(false)]
        public async Task<ActionResult> UploadChunk(int id)
        {
            if (Request.ContentLength != null)
            {
                var request = Request.Form.Files["Slice"];

                var chunk = new byte[request.Length];

                request.OpenReadStream().Read(chunk, 0, Convert.ToInt32(request.Length));

                JsonResult returnData;

                const string fileSession = "CurrentFile";

                var fileBytes = new byte[1024];

                if (HttpContext.Session.TryGetValue(fileSession, out fileBytes))
                {
                    var fileString = GetString(fileBytes);

                    CloudFile model = JsonConvert.DeserializeObject<CloudFile>(fileString);

                    // Get the blob
                    var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
                    var container = await GetContainer(storageAccount, model.ContainerName);
                    CloudBlockBlob bBlob = container.GetBlockBlobReference(model.BlobName);

                    returnData = await UploadCurrentChunk(model, chunk, id, bBlob);

                    if (returnData != null)
                    {
                        return returnData;
                    }
                    if (id == model.BlockCount)
                    {
                        return await CommitAllChunks(model, bBlob);
                    }
                }
                else
                {
                    returnData = Json(new
                    {
                        error = true,
                        isLastBlock = false,
                        message = string.Format(CultureInfo.CurrentCulture,
                            "Failed to Upload file.", "Session Timed out")
                    });
                    return returnData;
                }
            }

            return Json(new { error = false, isLastBlock = false, message = string.Empty });
        }

        /// <summary>
        /// Sends every chunk of the .fcs file and sends an sms to the user
        /// </summary>
        /// <param name="model"></param>
        /// <param name="blockBlob"></param>
        /// <returns></returns>
        private async Task<ActionResult> CommitAllChunks(CloudFile model, CloudBlockBlob blockBlob)
        {
            model.IsUploadCompleted = true;

            var errorInOperation = false;

            try
            {
                var blockList = Enumerable.Range(1, (int)model.BlockCount).ToList().Select(rangeElement =>
                               Convert.ToBase64String(Encoding.UTF8.GetBytes(
                               string.Format(CultureInfo.InvariantCulture, "{0:D4}", rangeElement))));

                //model.BlockBlob.PutBlockListAsync(blockList);
                await blockBlob.PutBlockListAsync(blockList);

                var duration = DateTime.Now - model.StartTime;

                float fileSizeInKb = model.Size / 1024;

                var fileSizeMessage = fileSizeInKb > 1024 ?
                    string.Concat((fileSizeInKb / 1024).ToString(CultureInfo.CurrentCulture), " MB") :
                    string.Concat(fileSizeInKb.ToString(CultureInfo.CurrentCulture), " KB");

                var message = string.Format(CultureInfo.CurrentCulture,
                    "File uploaded successfully. {0} took {1} seconds to upload\n",
                    fileSizeMessage, duration.TotalSeconds);

                //Get the user
                var user = GetUser();
                //var fcsPath = user.LastName.ToLower() + "-" + user.FirstName.ToLower() + "-" + user.Id + "/" + model.FileName;
                var fcsPath = user.LastName.ToLower() + "-" + user.FirstName.ToLower() + "/" + model.FileName;

                var storedPatient = _context.Patients.First(x => x.Id == model.Patient);

                var analysis = new Analysis
                {
                    Date = DateTime.Now.Date,
                    FcsFilePath = fcsPath,
                    FcsUploadDate = DateTime.Now.Date.ToString("MM-dd-yyyy-hh-mm"),
                    ResultFilePath = "",
                    ResultDate = DateTime.Now.Date,
                    Delta = 0.00
                };

                storedPatient.Analyses.Add(analysis);
                user.Analyses.Add(analysis);
                _context.SaveChanges();

                if (!string.IsNullOrWhiteSpace(user.Phone))
                {
                    //send message to the user
                    _smsSender.SendSms(new AuthMessageSender.Message
                    {
                        Body = "Greetings" + "\nStatus on: " + model.OriginalFileName + "\n" + message
                    }, _sid, _authToken, _number, user.Phone);
                }

                model.UploadStatusMessage = message;
            }
            catch (StorageException e)
            {
                model.UploadStatusMessage = "Failed to Upload file. Exception - " + e.Message;
                errorInOperation = true;
            }
            finally
            {
                HttpContext.Session.Remove("CurrentFile");
            }
            return Json(new
            {
                error = errorInOperation,
                isLastBlock = model.IsUploadCompleted,
                message = model.UploadStatusMessage
            });
        }
        /// <summary>
        /// returns if there is no previous analysis for this particular user and a particular date
        /// </summary>
        /// <param name="model"></param>
        /// <param name="fcsPath"></param>
        /// <returns></returns>
        private bool ThereIsNoPreviousAnalysis(CloudFile model, string fcsPath)
        {
            return
                !_context.Analyses.Any(
                    i =>
                        i.Patient.Id.Equals(model.Patient));
        }

        /// <summary>
        /// Uploads the current chunk to the storage
        /// </summary>
        /// <param name="model"></param>
        /// <param name="chunk"></param>
        /// <param name="id"></param>
        /// <param name="blobBlock"></param>
        /// <returns></returns>
        private async Task<JsonResult> UploadCurrentChunk(CloudFile model, byte[] chunk, int id, CloudBlockBlob blobBlock)
        {
            using (var chunkStream = new MemoryStream(chunk))
            {
                var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        string.Format(CultureInfo.InvariantCulture, "{0:D4}", id)));
                try
                {
                    //model.BlockBlob.PutBlockAsync(
                    //    blockId,
                    //    chunkStream, null, null,
                    //    new BlobRequestOptions()
                    //    {
                    //        RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3)
                    //    },
                    //    null);

                    await blobBlock.PutBlockAsync(
                        blockId,
                        chunkStream, null, null,
                        new BlobRequestOptions()
                        {
                            RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3)
                        },
                        null);

                    return null;
                }
                catch (StorageException e)
                {
                    HttpContext.Session.Remove("CurrentFile");
                    model.IsUploadCompleted = true;
                    model.UploadStatusMessage = "Failed to Upload file. Exception - " + e.Message;
                    return Json(new { error = true, isLastBlock = false, message = model.UploadStatusMessage });
                }
            }
        }

        #region Helpers
        /// <summary>
        /// returns the current user
        /// </summary>
        /// <returns></returns>
        private ApplicationUser GetUser()
        {
            return _context.Users.First(i => i.UserName.Equals(User.Identity.Name));
        }

        /// <summary>
        /// returns the container, if not it will create a new one
        /// </summary>
        /// <param name="account"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<CloudBlobContainer> GetContainer(CloudStorageAccount account, string name)
        {
            //blob client now
            var blobClient = account.CreateCloudBlobClient();

            //the container for this is companystyles
            var container = blobClient.GetContainerReference(name);

            //Create a new container, if it does not exist
            await container.CreateIfNotExistsAsync();

            return container;
        }

        #endregion
    }
}