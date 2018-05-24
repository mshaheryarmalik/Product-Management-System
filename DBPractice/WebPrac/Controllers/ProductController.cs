using PMS.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebPrac.Security;

namespace WebPrac.Controllers
{
    public class ProductController : Controller
    {
        private ActionResult GetUrlToRedirect()
        {
            if (SessionManager.IsValidUser)
            {
                if (SessionManager.User.IsAdmin == false || SessionManager.User.UserID == 0)
                {
                    TempData["Message"] = "Unauthorized Access";
                    return Redirect("~/Home/NormalUser");
                }
            }
            else
            {
                TempData["Message"] = "Unauthorized Access";
                return Redirect("~/User/Login");
            }

            return null;
        }
        public ActionResult ShowAll()
        {
            if (SessionManager.IsValidUser == false)
            {
                return Redirect("~/User/Login");
            }

            var products = PMS.BAL.ProductBO.GetAllProducts(true);

            return View(products);
        }

        public ActionResult New()
        {

            var dto = new ProductDTO();
            return View(dto);
            //var redVal = GetUrlToRedirect();
            //if (redVal == null)
            //{
            //    var dto = new ProductDTO();
            //    redVal =  View(dto);
            //}
         }

        public ActionResult Edit(int id)
        {

            var redVal = GetUrlToRedirect();
            if (redVal == null)
            {
                var prod = PMS.BAL.ProductBO.GetProductById(id);
                redVal= View("New", prod);
            }

            return redVal;
            
        }
        public ActionResult Edit2(int pid)
        {
            var prod = PMS.BAL.ProductBO.GetProductById(pid);
            return View("New", prod);
        }
        public ActionResult Delete(int id)
        {

            if (SessionManager.IsValidUser)
            {

                if (SessionManager.User.IsAdmin == false)
                {
                    TempData["Message"] = "Unauthorized Access";
                    return Redirect("~/Home/NormalUser");
                }
            }
            else
            {
                return Redirect("~/User/Login");
            }

            PMS.BAL.ProductBO.DeleteProduct(id);
            TempData["Msg"] = "Record is deleted!";
            return RedirectToAction("ShowAll");
        }
        [HttpPost]
        public ActionResult Save(ProductDTO dto)
        {
            /*
            if (SessionManager.IsValidUser)
            {

                if (SessionManager.User.IsAdmin == false)
                {
                    TempData["Message"] = "Unauthorized Access";
                    return Redirect("~/Home/NormalUser");
                }
            }
            else
            {
                return Redirect("~/User/Login");
            }
            */

            var uniqueName = "";

            if (Request.Files["Image"] != null)
            {
                var file = Request.Files["Image"];
                if (file.FileName != "")
                {
                    var ext = System.IO.Path.GetExtension(file.FileName);
                    String extension = ext.ToString();
                    if (extension != ".jpg")
                    {
                        TempData["Message"] = "Only jpg files are allowed";
                        return Redirect("~/Home/NormalUser");
                    }
                    //Generate a unique name using Guid
                    uniqueName = Guid.NewGuid().ToString() + ext;

                    //Get physical path of our folder where we want to save images
                    var rootPath = Server.MapPath("~/UploadedFiles");

                    var fileSavePath = System.IO.Path.Combine(rootPath, uniqueName);

                    // Save the uploaded file to "UploadedFiles" folder
                    file.SaveAs(fileSavePath);

                    dto.PictureName = uniqueName;
                }
            }



            if (dto.ProductID > 0)
            {
                dto.ModifiedOn = DateTime.Now;
                dto.ModifiedBy = SessionManager.User.UserID;
            }
            else
            {
                dto.CreatedOn = DateTime.Now;
                dto.CreatedBy = SessionManager.User.UserID;
            }

            PMS.BAL.ProductBO.Save(dto);

            TempData["Msg"] = "Record is saved!";

            return RedirectToAction("ShowAll");
        }

        [HttpPost]
        public ActionResult SaveComment(CommentDTO dto)
        {
           
            if (SessionManager.IsValidUser == false)
            {
                return Redirect("~/User/Login");
            }

            var products = PMS.BAL.ProductBO.GetAllProducts(true);
            dto.CommentOn = DateTime.Now;
            dto.UserID = SessionManager.User.UserID;
            PMS.BAL.CommentBO.Save(dto);

            return View("ShowAll", products);
        }

    }
}