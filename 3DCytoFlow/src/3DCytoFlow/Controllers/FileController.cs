﻿using _3DCytoFlow.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.OptionsModel;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace _3DCytoFlow.Controllers
{
    public class FileController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private ApplicationDbContext _context;
        private string storageConnectionString;

        public FileController( ApplicationDbContext context, IOptions<StorageSettings> options, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
            storageConnectionString = options.Value.StorageStringConnection;
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
        public async Task<ActionResult> DownloadResult(string path)
        {
            var jsonString = "";
            //prepare container name
            var index = path.IndexOf('/');

            var containerName = path.Substring(0, index);

            //prepare blobName
            var blobName = path.Substring(index + 1);

            // Retrieve storage account from web.config
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            //retrieve container
            var container = await GetContainer(storageAccount, containerName);

            List<IListBlobItem> blobs = new List<IListBlobItem>();

            //List blobs and directories in this container
            BlobContinuationToken token = null;
            do
            {
                var result = await container.ListBlobsSegmentedAsync("",true, BlobListingDetails.None, 500, token, null, null);
                token = result.ContinuationToken;
                blobs.AddRange(result.Results);
                //Now do something with the blobs
            } while (token != null);

            //prepare the location
            foreach (var blob in blobs.Where(i => i.Uri.ToString().Contains(".json")).Cast<CloudBlockBlob>().Where(i => i.Name.Equals(blobName)))
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
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            var patientCompleteName = patient.Split(' ');

            var firstName = patientCompleteName[0];
            var lastName = patientCompleteName[1];

            //container name will be lastname-name-id of the user. Everything in lowercase or Azure complains with a 400 error
            var user = GetUser();
            //var containerName = user.LastName + "-" + user.FirstName + "-" + user.Id;
            var containerName = user.LastName.ToLower() + "-" + user.FirstName.ToLower() + "-" + user.Id;
            var container = await GetContainer(storageAccount, containerName.ToLower());

            //get the patient
            var storedPatient = GetPatient(firstName, lastName);

            //blob exact name and location
            var blobName = lastName + "-" + firstName + "/" + DateTime.Now.ToString("MM-dd-yyyy") + ".fcs";

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
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
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
                    var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
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
                    "File uploaded successfully. {0} took {1} seconds to upload\nYou'll receive another SMS when the results are completed",
                    fileSizeMessage, duration.TotalSeconds);

                //Get the user
                var user = GetUser();
                //var fcsPath = user.LastName.ToLower() + "-" + user.FirstName.ToLower() + "-" + user.Id + "/" + model.FileName;
                var fcsPath = user.LastName.ToLower() + "-" + user.FirstName.ToLower() + "-" + user.Id + "/" + model.FileName;
                //if the analysis did not exist before, add a new record to the db
                if (ThereIsNoPreviousAnalysis(model, fcsPath))
                {

                    var storedPatient = _context.Patients.First(x => x.Id == model.Patient);

                    var analysis = new Analysis
                    {
                        Date = DateTime.Now.Date,
                        FcsFilePath = fcsPath,
                        FcsUploadDate = DateTime.Now.Date.ToString("yy-MM-dd-yyyy"),
                        ResultFilePath = "",
                        ResultDate = DateTime.Now.Date,
                        Delta = 0.00
                    };

                    storedPatient.Analyses.Add(analysis);
                    user.Analyses.Add(analysis);
                    _context.SaveChanges();
                }
                //otherwise, continue with the process and
                //notify the user about the success of the file upload
                //                SmsService.SendSms(new IdentityMessage
                //                {
                //                    Destination = user.Phone,
                //                    Body = Greeting + "\nStatus on: " + model.OriginalFileName + "\n" + message
                //                });

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

    public class StorageSettings
    {
        public string StorageStringConnection { get; set; }
    }
}