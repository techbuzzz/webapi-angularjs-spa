﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ResourceMetadata.Web.Helpers;
using System.Web.Mvc.Filters;
using ResourceMetadata.Service;
using Microsoft.AspNet.Identity;
using ResourceMetadata.Model;
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using ResourceMetadata.Web.ViewModels;
using AutoMapper;

namespace ResourceMetadata.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }
        public HomeController(IUserService userService, UserManager<ApplicationUser> userManager)
        { 
            this.userManager = userManager;
            //Todo: This needs to be moved from here.
            this.userManager.UserValidator = new UserValidator<ApplicationUser>(userManager)
            {
                AllowOnlyAlphanumericUserNames = false
            };
        }

        [ResourceManagerAuthroize("Account/Login")]
        //[Authorize]
        public ActionResult Index()
        {            
            return View(GetUserProfile());
        }

        private UserProfileViewModel GetUserProfile()
        {
            var userName = User.Identity.GetUserName();
            var user = userManager.FindByName(userName);

            if (user != null)
            {
                string userRole = "Member";

                if (user.Roles != null && user.Roles.Count > 0)
                {
                    userRole = user.Roles.First().Role.Name;
                }

                var userProfile = new UserProfileViewModel
                {
                    Email = user.Email,
                    Role = userRole
                };

                return userProfile;
            }

            throw new UnauthorizedAccessException();
        }

        [HttpPost]
        [OverrideAuthentication]
        public async Task<ActionResult> Register(RegisterViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    ApplicationUser user = new ApplicationUser();
                    Mapper.Map(viewModel, user);

                    var identityResult =  await userManager.CreateAsync(user, viewModel.Password);
                    //var userRoleResult = userManager.AddToRole(user.Id, "Member");

                    if (identityResult.Succeeded)
                    {
                        var userRoleResult = userManager.AddToRole(user.Id, "Member");

                        if (userRoleResult.Succeeded)
                        {
                            await SignInAsync(user, isPersistent: false);
                            return RedirectToAction("Index", "Home");
                        }

                        return View();
                    }
                    else
                    {
                        foreach (var error in identityResult.Errors)
                        {
                            //ModelState.AddModelError(error)
                        }

                        return View();
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
            else
            {
                return View();
            }

        }

        #region SignInAsync
        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            try
            {
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                var identity = await userManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
                AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        #endregion SignInAsync
    }
}