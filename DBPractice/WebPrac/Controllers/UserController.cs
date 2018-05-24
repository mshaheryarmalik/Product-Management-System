using PMS.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mail;
using System.Web.Mvc;
using WebPrac.Security;

namespace WebPrac.Controllers
{
    public class UserController : Controller
    {
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Login")]
        public ActionResult MyLogin()
        {
            if (Request.Form["btnLogin"] != null)
            {
                String login=Request["login"];
                String password = Request["password"];
                var obj = PMS.BAL.UserBO.ValidateUser(login, password);
                if (obj != null)
                {
                    Session["user"] = obj;
                    if (obj.IsAdmin == true)
                        return Redirect("~/Home/Admin");
                    else
                        return Redirect("~/Home/NormalUser");
                }

                ViewBag.MSG = "Invalid Login/Password";
                ViewBag.Login = login;

                return View();
            }
            else
            {
                String email = Request["email"];
                int isValid = PMS.BAL.UserBO.ValidateEmail(email);
                if (isValid > 0)
                {
                    try
                    {

                        System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();

                        MailAddress to = new MailAddress(email);
                        mail.To.Add(to);

                        MailAddress from = new MailAddress("ead.csf15@gmail.com", "Malik Shaheryar");
                        mail.From = from;

                        mail.Subject = "Reset Code";
                        string resetCode = Guid.NewGuid().ToString();
                        Session["ResetCode"] = resetCode;
                        Session["Email"] = email;
                        mail.Body = "Reset Code: " + resetCode;

                        var sc = new SmtpClient("smtp.gmail.com", 587)
                        {
                            Credentials = new System.Net.NetworkCredential("ead.csf15", "EAD_csf15m"),
                            EnableSsl = true
                        };

                        sc.Send(mail);
                    }
                    catch (Exception ex)
                    {
                    }
                    return Redirect("~/user/resetpasswd");
                }
                else
                {
                    ViewBag.Message = "Email not found!";
                    return Redirect("~/home/index");
                }
            }
        }

        public ActionResult ResetPasswd(String email)
        {
            return View();
        }
        [HttpPost]
        public ActionResult ValidateCode()
        {
            String sentCode = Session["ResetCode"].ToString();
            String userCode = Request["code"];
            if (sentCode == userCode)
            {
                return View();
            }
            TempData["Message"] = "Invalid Code";
            return RedirectToAction("resetpasswd");
        }
        [HttpPost]
        public ActionResult NewPasswd()
        {
            String passwd = Request["passwd"];
            String email = Session["Email"].ToString();
            UserDTO dto = new UserDTO();
            dto.Password = passwd;
            dto.Email = email;
           int res = PMS.BAL.UserBO.MyUpdatePassword(email,passwd);
            return Redirect("~/home/index");
        }
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Save(UserDTO dto)
        {
            if(PMS.BAL.UserBO.ValidateEmail(dto.Email) > 0)
            {
                TempData["Message"] = "Email Already Exists";
                return Redirect("~/Home/NormalUser");
            }
            var uniqueName = "";
            if (Request.Files["image"] != null)
            {
                var file = Request.Files["image"];
                if (file.FileName != "")
                {
                    var ext = System.IO.Path.GetExtension(file.FileName);
                    String extension = ext.ToString();
                    if(extension != ".jpg")
                    {
                        TempData["Message"] = "Only jpg files are allowed";
                        return Redirect("~/Home/NormalUser");
                    }
                    uniqueName = Guid.NewGuid().ToString() + ext;
                    var rootPath = Server.MapPath("~/uploadedFiles");
                    var fileSavePath = System.IO.Path.Combine(rootPath, uniqueName);
                    file.SaveAs(fileSavePath);
                    dto.PictureName = uniqueName;
                }
            }
            //User Save Logic
            dto.IsAdmin = false;
            dto.IsActive = true;
            var obj = PMS.BAL.UserBO.Save(dto);
            int result = Convert.ToInt32(obj);
            if(result > 0)
            {
                if (dto.UserID == 0)
                    return View("Login");
                else
                {
                    SessionManager.User.Name = dto.Name;
                    SessionManager.User.PictureName = dto.PictureName;
                    return Redirect("~/Home/NormalUser");
                }
            }
            return Redirect("~/Views/Shared/Error");
        }

        [HttpGet]
        public ActionResult Logout()
        {
            SessionManager.ClearSession();
            return RedirectToAction("Login");
        }


        [HttpGet]
        public ActionResult Login2()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ValidateUser(String login, String password)
        {

            Object data = null;

            try
            {
                var url = "";
                var flag = false;

                var obj = PMS.BAL.UserBO.ValidateUser(login, password);
                if (obj != null)
                {
                    flag = true;
                    SessionManager.User = obj;

                    if (obj.IsAdmin == true)
                        url = Url.Content("~/Home/Admin");
                    else
                        url = Url.Content("~/Home/NormalUser");
                }

                data = new
                {
                    valid = flag,
                    urlToRedirect = url
                };
            }
            catch (Exception)
            {
                data = new
                {
                    valid = false,
                    urlToRedirect = ""
                };
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Edit()
        {
            UserDTO usr = PMS.BAL.UserBO.GetUserById(SessionManager.User.UserID);
            return RedirectToAction("SignUp","Home",usr);
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        public ActionResult Profile(int uid)
        {
            if(SessionManager.User.UserID == uid)
            {
                return Redirect("~/Home/NormalUser");
            }
            var usr = PMS.BAL.UserBO.GetUserById(uid);
            return View(usr);
        }

        [HttpPost]
        public ActionResult ValidatePasswd()
        {
            String old = Request["oldPasswd"];
            String newPass = Request["newPasswd"];
            String confirm = Request["confirmPasswd"];
            if(SessionManager.User.Email == old)
            {
                if(newPass != confirm)
                {
                    ViewBag.Message = "New and Confirm Password not match";
                    return View("ChangePassword");
                }
                else
                {
                    UserDTO dto = PMS.BAL.UserBO.GetUserById(SessionManager.User.UserID);
                    dto.Password = newPass;
                    var obj = PMS.BAL.UserBO.UpdatePassword(dto);
                    return RedirectToAction("NormalUser", "Home", dto);
                }
            }
            else
            {
                ViewBag.Message = "Invalid Old Password";
                return View("ChangePassword");
            }
        }

       
	}
}