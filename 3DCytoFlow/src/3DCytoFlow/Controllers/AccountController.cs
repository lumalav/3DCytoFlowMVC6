using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using _3DCytoFlow.Models;
using _3DCytoFlow.Services;
using _3DCytoFlow.ViewModels.Account;
using Microsoft.WindowsAzure.Storage;
using System;
using System.IO;
using System.Globalization;
using System.Text;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;

namespace _3DCytoFlow.Controllers
{
    [Authorize]
    [Route("User/[action]")]
    public class AccountController : Controller
    {
        const string Greeting = "\nHi User! This is 3DCytoFlow giving you an update on your recent request";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;
        private readonly string storageConnectionString;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILoggerFactory loggerFactory,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = loggerFactory.CreateLogger<AccountController>();
            _context = context;
            storageConnectionString = "";
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation(1, "User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning(2, "User account locked out.");
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                    //await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                    //    "Please confirm your account by clicking this link: <a href=\"" + callbackUrl + "\">link</a>");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(3, "User created a new account with password.");
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        //
        // GET: /Account/ExternalLoginCallback
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.ExternalPrincipal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
        {
            if (User.IsSignedIn())
            {
                return RedirectToAction(nameof(ManageController.Index), "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                // Send an email with this link
                //var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                //var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                //await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                //   "Please reset your password by clicking here: <a href=\"" + callbackUrl + "\">link</a>");
                //return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            // Generate the token and send it
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            var message = "Your security code is: " + code;
            if (model.SelectedProvider == "Email")
            {
                await _emailSender.SendEmailAsync(await _userManager.GetEmailAsync(user), "Security Code", message);
            }
            else if (model.SelectedProvider == "Phone")
            {
                await _smsSender.SendSmsAsync(await _userManager.GetPhoneNumberAsync(user), message);
            }

            return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return RedirectToLocal(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(7, "User account locked out.");
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError("", "Invalid code.");
                return View(model);
            }
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
        /// TODO:Improve. this will be triggered with entity change notifier: http://www.codeproject.com/Articles/496484/SqlDependency-with-Entity-Framework
        /// Downloads all the json files from the storage and saves them in the Results folder
        /// </summary>
        /// <param name = "user" ></ param >
        //private void DownloadResults(ApplicationUser user)
        //{
        //    // Retrieve storage account from connection string.
        //    var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

        //    //get the container
        //    var containerName = user.LastName + "-" + user.FirstName + "-" + user.Id;

        //    var container = GetContainer(storageAccount, containerName.ToLower());

        //    //List blobs and directories in this container
        //    var blobs = container.ListBlobs(useFlatBlobListing: true);

        //    //prepare the location
        //    var directory = Server.MapPath("/Results/" + containerName);

        //    foreach (var blob in blobs.Where(i => i.Uri.ToString().Contains(".json")).Cast<CloudBlockBlob>())
        //    {
        //        var filePath = blob.Name.Split('/');
        //        var patientName = filePath[0];
        //        var fileName = filePath[1];

        //        //check if folder exists, if not create it
        //        if (!Directory.Exists(directory))
        //            Directory.CreateDirectory(directory);

        //        using (var fileStream = new FileStream(Server.MapPath("/Results/" + containerName + "/" + patientName + "-" + fileName), FileMode.Create))
        //        {
        //            blob.DownloadToStream(fileStream);
        //        }
        //    }
        //}
        /// <summary>
        /// Prepares the storage that will receive the .fcs file
        /// </summary>
        /// <param name="blocksCount"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SetMetadata(int blocksCount, string fileName, long fileSize, string patient)
        {
            string hello = "hello";

            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            var patientCompleteName = patient.Split(' ');

            var firstName = patientCompleteName[0];
            var lastName = patientCompleteName[1];

            //container name will be lastname-name-id of the user. Everything in lowercase or Azure complains with a 400 error
            var user = GetUser();
            var containerName = user.LastName + "-" + user.FirstName + "-" + user.Id;
            var container = GetContainer(storageAccount, containerName.ToLower());

            //get the patient
            var storedPatient = GetPatient(firstName, lastName);

            //blob exact name and location
            var blobName = lastName + "-" + firstName + "/" + DateTime.Now.ToString("MM-dd-yyyy") + ".fcs";

            //filename will be lastname-name-uploaddate.fcs of the patient
            var fileToUpload = new CloudFile()
            {
                OriginalFileName = fileName,
                Patient = storedPatient,
                BlockCount = blocksCount,
                FileName = blobName.ToLower(),
                Size = fileSize,
                BlockBlob = container.GetBlockBlobReference(blobName.ToLower()),
                StartTime = DateTime.Now,
                IsUploadCompleted = false,
                UploadStatusMessage = string.Empty
            };

            var fileByteArray = GetBytes(JsonConvert.SerializeObject(fileToUpload));

            using (var ms = new MemoryStream())
            {
                HttpContext.Session.Set("CurrentFile", fileByteArray);
            }

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
        public ActionResult UploadChunk(int id)
        {
            if (Request.ContentLength != null)
            {
                var chunk = new byte[Request.ContentLength.Value];

                var input = new StreamReader(Request.Body).ReadToEnd();

                JsonResult returnData;

                const string fileSession = "CurrentFile";

                var fileBytes = new byte[1024];                

                if (HttpContext.Session.TryGetValue(fileSession, out fileBytes))
                {
                    CloudFile model = (CloudFile)JsonConvert.DeserializeObject(GetString(fileBytes));

                    returnData = UploadCurrentChunk(model, chunk, id);

                    if (returnData != null)
                    {
                        return returnData;
                    }
                    if (id == model.BlockCount)
                    {
                        return CommitAllChunks(model);
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
        private ActionResult CommitAllChunks(CloudFile model)
        {
            model.IsUploadCompleted = true;

            var errorInOperation = false;

            try
            {
                var blockList = Enumerable.Range(1, (int)model.BlockCount).ToList().Select(rangeElement =>
                               Convert.ToBase64String(Encoding.UTF8.GetBytes(
                               string.Format(CultureInfo.InvariantCulture, "{0:D4}", rangeElement))));

                model.BlockBlob.PutBlockListAsync(blockList);

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
                var fcsPath = user.LastName.ToLower() + "-" + user.FirstName.ToLower() + "-" + user.Id + "/" + model.FileName;
                //if the analysis did not exist before, add a new record to the db
                if (ThereIsNoPreviousAnalysis(model, fcsPath))
                {
                    var analysis = new Analysis
                    {
                        Date = DateTime.Now.Date,
                        FcsFilePath = fcsPath,
                        FcsUploadDate = DateTime.Now.Date.ToString("yy-MM-dd-yyyy"),
                        ResultFilePath = "",
                        ResultDate = DateTime.Now.Date,
                        Delta = 0.00
                    };

                    var storedPatient = GetPatient(model.Patient.FirstName, model.Patient.LastName);

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
                        i.Patient.FirstName.Equals(model.Patient.FirstName) &&
                        i.Patient.LastName.Equals(model.Patient.LastName) && i.FcsFilePath.Equals(fcsPath));
        }

        /// <summary>
        /// Uploads the current chunk to the storage
        /// </summary>
        /// <param name="model"></param>
        /// <param name="chunk"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private JsonResult UploadCurrentChunk(CloudFile model, byte[] chunk, int id)
        {
            using (var chunkStream = new MemoryStream(chunk))
            {
                var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        string.Format(CultureInfo.InvariantCulture, "{0:D4}", id)));
                try
                {
                    model.BlockBlob.PutBlockAsync(
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
        public CloudBlobContainer GetContainer(CloudStorageAccount account, string name)
        {
            //blob client now
            var blobClient = account.CreateCloudBlobClient();

            //the container for this is companystyles
            var container = blobClient.GetContainerReference(name);

            //Create a new container, if it does not exist
            container.CreateIfNotExistsAsync();

            return container;
        }
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.FindByIdAsync(HttpContext.User.GetUserId());
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
