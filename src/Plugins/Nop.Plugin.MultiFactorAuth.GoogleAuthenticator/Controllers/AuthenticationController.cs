﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Http.Extensions;
using Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Models;
using Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Services;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AuthenticationController : BasePluginController
    {
        #region Fields

        private readonly CustomerSettings _customerSettings;
        private readonly GoogleAuthenticatorService _googleAuthenticatorService;
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor
        public AuthenticationController(
            CustomerSettings customerSettings,
            GoogleAuthenticatorService googleAuthenticatorService,
            ICustomerRegistrationService customerRegistrationService,
            ICustomerService customerService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IWorkContext workContext)
        {
            _customerSettings = customerSettings;
            _googleAuthenticatorService = googleAuthenticatorService;
            _customerRegistrationService = customerRegistrationService;
            _customerService = customerService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        [HttpPost]
        public IActionResult RegisterGoogleAuthenticator(AuthModel model)
        {
            var currentCustomer = _workContext.CurrentCustomer;

            var isValidToken = _googleAuthenticatorService.ValidateTwoFactorToken(model.SecretKey, model.Code);
            if (isValidToken)
            {
                //try to find config with current customer and update
                if (_googleAuthenticatorService.IsRegisteredCustomer(currentCustomer.Email))
                {
                    _googleAuthenticatorService.UpdateGoogleAuthenticatorAccount(currentCustomer.Email, model.SecretKey);
                }
                else
                {
                    _googleAuthenticatorService.AddGoogleAuthenticatorAccount(currentCustomer.Email, model.SecretKey);
                }
                _notificationService.SuccessNotification(_localizationService.GetResource("Plugins.MultiFactorAuth.GoogleAuthenticator.Token.Successful"));
            }
            else
            {
                _notificationService.ErrorNotification(_localizationService.GetResource("Plugins.MultiFactorAuth.GoogleAuthenticator.Token.Unsuccessful"));
                return RedirectToRoute("CustomerMultiFactorAuthenticationProviderConfig", new { providerSysName = GoogleAuthenticatorDefaults.SystemName });
            }
            
            return RedirectToRoute("MultiFactorAuthenticationSettings");
        }

        [HttpPost]
        public IActionResult VerifyGoogleAuthenticator(TokenModel model)
        {
            var customerMultiFactorAuthenticationInfo = HttpContext.Session.Get<CustomerMultiFactorAuthenticationInfo>(NopCustomerDefaults.CustomerMultiFactorAuthenticationInfo);
            var username = customerMultiFactorAuthenticationInfo.UserName;
            var returnUrl = customerMultiFactorAuthenticationInfo.ReturnUrl;
            var isPersist = customerMultiFactorAuthenticationInfo.RememberMe;

            var customer = _customerSettings.UsernamesEnabled ? _customerService.GetCustomerByUsername(username) : _customerService.GetCustomerByEmail(username);
            if (customer == null)
                return RedirectToRoute("Login");

            var record = _googleAuthenticatorService.GetConfigurationByCustomerEmail(customer.Email);
            if (record != null)
            {
                var isValidToken = _googleAuthenticatorService.ValidateTwoFactorToken(record.SecretKey, model.Token);
                if (isValidToken)
                {
                    HttpContext.Session.Set<CustomerMultiFactorAuthenticationInfo>(NopCustomerDefaults.CustomerMultiFactorAuthenticationInfo, null);

                    return _customerRegistrationService.SignInCustomer(customer, returnUrl, isPersist);
                }
                else
                {
                    _notificationService.ErrorNotification(_localizationService.GetResource("Plugins.MultiFactorAuth.GoogleAuthenticator.Token.Unsuccessful"));
                }
            }

            return RedirectToRoute("MultiFactorVerification");
        }

        #endregion

    }
}
