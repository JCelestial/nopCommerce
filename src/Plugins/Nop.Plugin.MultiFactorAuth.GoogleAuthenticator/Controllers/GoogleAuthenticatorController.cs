﻿using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Models;
using Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Services;
using Nop.Services.Authentication.MultiFactor;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Controllers
{
    [AutoValidateAntiforgeryToken]
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class GoogleAuthenticatorController : BasePluginController
    {
        #region Fields

        private readonly GoogleAuthenticatorService _googleAuthenticatorService;
        private readonly GoogleAuthenticatorSettings _googleAuthenticatorSettings;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IMultiFactorAuthenticationPluginManager _multiFactorAuthenticationPluginManager;        
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;


        #endregion

        #region Ctor 

        public GoogleAuthenticatorController(GoogleAuthenticatorService googleAuthenticatorService,
            GoogleAuthenticatorSettings googleAuthenticatorSettings,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IMultiFactorAuthenticationPluginManager multiFactorAuthenticationPluginManager,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            IWorkContext workContext
            )
        {
            _googleAuthenticatorService = googleAuthenticatorService;
            _googleAuthenticatorSettings = googleAuthenticatorSettings;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _multiFactorAuthenticationPluginManager = multiFactorAuthenticationPluginManager;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMultifactorAuthenticationMethods))
                return AccessDeniedView();

            //prepare model
            var model = new ConfigurationModel
            {
                QRPixelsPerModule = _googleAuthenticatorSettings.QRPixelsPerModule
                
            };
            model.GoogleAuthenticatorSearchModel.HideSearchBlock = _genericAttributeService
                .GetAttribute<bool>(_workContext.CurrentCustomer, GoogleAuthenticatorDefaults.HideSearchBlockAttribute);

            return View("~/Plugins/MultiFactorAuth.GoogleAuthenticator/Views/Configure.cshtml", model);
        }

        [HttpPost]        
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMultifactorAuthenticationMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var settings = _settingService.LoadSetting<GoogleAuthenticatorSettings>(storeScope);

            //set new settings values
            settings.QRPixelsPerModule = model.QRPixelsPerModule;

            //save settings
            _settingService.SaveSettingOverridablePerStore(settings, setting => setting.QRPixelsPerModule, true, storeScope, false);
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [HttpPost]
        public IActionResult GoogleAuthenticatorList(GoogleAuthenticatorSearchModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMultifactorAuthenticationMethods))
                return AccessDeniedView();

            //get GoogleAuthenticator configuration records
            var configurations = _googleAuthenticatorService.GetPagedConfigurations(searchModel.SearchEmail,
                searchModel.Page - 1, searchModel.PageSize);
            var model = new GoogleAuthenticatorListModel().PrepareToGrid(searchModel, configurations, () =>
            {
                //fill in model values from the configuration
                return configurations.Select(configuration => new GoogleAuthenticatorModel
                {
                    Id = configuration.Id,
                    Customer = configuration.Customer,
                    SecretKey = configuration.SecretKey
                });
            });

            return Json(model);
        }

        [HttpPost]
        public IActionResult GoogleAuthenticatorDelete (GoogleAuthenticatorModel model)
        {
            if (!ModelState.IsValid)
                return ErrorJson(ModelState.SerializeErrors());

            //delete configuration
            var configuration = _googleAuthenticatorService.GetConfigurationById(model.Id);
            if (configuration != null)
            {
                _googleAuthenticatorService.DeleteConfiguration(configuration);
            }

            return new NullJsonResult();
        }

        #endregion
    }
}
