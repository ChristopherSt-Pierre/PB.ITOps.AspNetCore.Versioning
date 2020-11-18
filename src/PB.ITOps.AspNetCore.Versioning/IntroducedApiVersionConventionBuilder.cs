using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning.Conventions;
using PB.ITOps.AspNetCore.Versioning.Extensions;

namespace PB.ITOps.AspNetCore.Versioning
{
    public class IntroducedApiVersionConventionBuilder : ApiVersionConventionBuilder
    {
        private readonly Version _startVersion;
        private readonly Version _currentVersion;
        private readonly ApiVersions _allVersions;
        private readonly ApiVersionConventionBuilder _apiVersionConventionBuilder;

        internal ApiVersions AllVersions => _allVersions;

        /// <summary>
        /// Use this convention to allow use of the `IntroduceInApiVersionAttribute`
        /// and `RemovedAsOfApiVersionAttribute`.
        /// Configure the range of APIs that are available. Only the current API version
        /// will be supported. All other API versions will be marked as deprecated.
        /// </summary>
        /// <param name="startVersion">The lowest API version that is available.</param>
        /// <param name="currentVersion">The current supported API version.</param>
        public IntroducedApiVersionConventionBuilder(Version startVersion, Version currentVersion) 
            : this(startVersion, currentVersion, new ApiVersionConventionBuilder())
        {
        }
        
        internal IntroducedApiVersionConventionBuilder(Version startVersion, Version currentVersion, 
            ApiVersionConventionBuilder apiVersionConventionBuilder)
        {
            _startVersion = startVersion;
            _currentVersion = currentVersion;
            _allVersions = new ApiVersions(startVersion, currentVersion);
            _apiVersionConventionBuilder = apiVersionConventionBuilder;
        }

        public override bool ApplyTo(ControllerModel controllerModel)
        {
            var controllerIntroducedInVersion = controllerModel.GetIntroducedVersion();
            var controllerRemovedAsOfVersion = controllerModel.GetRemovedVersion();

            ValidateControllerVersions(controllerModel, controllerIntroducedInVersion, controllerRemovedAsOfVersion);
            
            if (UseApiConvention(controllerIntroducedInVersion, controllerRemovedAsOfVersion))
            {
                return _apiVersionConventionBuilder.ApplyTo(controllerModel);
            }
            
            var controller = _apiVersionConventionBuilder.Controller(controllerModel.ControllerType);
            SetControllerApiVersions(controller, controllerIntroducedInVersion, controllerRemovedAsOfVersion);
            SetActionApiVersions(controllerModel, controllerIntroducedInVersion, controllerRemovedAsOfVersion, controller);

            _apiVersionConventionBuilder.ApplyTo(controllerModel);
            
            return true;
        }

        private bool UseApiConvention(ApiVersion controllerIntroducedInVersion, ApiVersion controllerRemovedAsOfVersion)
        {
            return controllerIntroducedInVersion == null
                   || new Version(controllerIntroducedInVersion.MajorVersion.Value,controllerIntroducedInVersion.MinorVersion.Value) > _currentVersion
                   || (controllerRemovedAsOfVersion != null && controllerRemovedAsOfVersion.MajorVersion.HasValue && new Version(controllerRemovedAsOfVersion.MajorVersion.Value, controllerRemovedAsOfVersion .MinorVersion.Value) <= _startVersion);
        }

        private static void ValidateControllerVersions(ControllerModel controllerModel, ApiVersion controllerIntroducedInVersion,
            ApiVersion controllerRemovedAsOfVersion)
        {
            if (controllerIntroducedInVersion == null || controllerRemovedAsOfVersion == null)
            {
                return;
            }
                
            if (controllerIntroducedInVersion == controllerRemovedAsOfVersion)
            {
                throw new InvalidOperationException(
                    $"({controllerModel.ControllerType}) ApiVersion cannot be introduced and removed in the same version.");
            }
            
            if (controllerRemovedAsOfVersion < controllerIntroducedInVersion)
            {
                throw new InvalidOperationException(
                    $"({controllerModel.ControllerType}) ApiVersion cannot be removed before it is introduced.");
            }
        }
        
        private void SetActionApiVersions(ControllerModel controllerModel, ApiVersion controllerIntroducedInVersion,
            ApiVersion controllerRemovedAsOfVersion, IControllerConventionBuilder controller)
        {
            foreach (var actionModel in controllerModel.Actions)
            {
                var actionModelIntroduced = actionModel.GetIntroducedVersion();
                ValidateActionModel(controllerModel, controllerIntroducedInVersion, actionModelIntroduced, actionModel);
                
                var actionIntroducedVersion = actionModelIntroduced ?? controllerIntroducedInVersion;
                var actionRemovedVersion = actionModel.GetRemovedVersion() ?? controllerRemovedAsOfVersion;

                SetActionApiVersions(actionModel, controller, actionIntroducedVersion, actionRemovedVersion);
            }
        }

        private static void ValidateActionModel(ControllerModel controllerModel, ApiVersion controllerIntroducedInVersion,
            ApiVersion actionModelIntroduced, ActionModel actionModel)
        {
            if (actionModelIntroduced != null && actionModelIntroduced < controllerIntroducedInVersion)
            {
                throw new InvalidOperationException($"Action ({actionModel.ActionName}) version cannot be" +
                                                    $" introduced before controller ({controllerModel.ControllerName}) version.");
            }
        }

        private void SetActionApiVersions(ActionModel actionModel, IControllerConventionBuilder controller, ApiVersion introducedVersion, ApiVersion removedVersion)
        {
            var actionSupportedVersions = _allVersions.GetSupportedVersions(introducedVersion, removedVersion);
            var actionDeprecatedVersions = _allVersions.GetDeprecatedVersions(introducedVersion, removedVersion);

            var action = controller.Action(actionModel.ActionMethod);
            action.HasApiVersions(actionSupportedVersions);
            action.HasDeprecatedApiVersions(actionDeprecatedVersions);
        }

        private void SetControllerApiVersions(IControllerConventionBuilder controller,
            ApiVersion introducedVersion, ApiVersion removedVersion)
        {
            var supportedVersions =
                _allVersions.GetSupportedVersions(introducedVersion, removedVersion);

            var deprecatedVersions =
                _allVersions.GetDeprecatedVersions(introducedVersion, removedVersion);

            controller.HasApiVersions(supportedVersions);
            controller.HasDeprecatedApiVersions(deprecatedVersions);
        }
    }
}