using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

using static PB.ITOps.AspNetCore.Versioning.Constants;

namespace PB.ITOps.AspNetCore.Versioning
{
    internal class ApiVersions
    {
        private readonly ApiVersion _currentVersion;
        
        internal IReadOnlyList<ApiVersion> AllVersions { get; }

        internal ApiVersions(Version startApiVersion, Version currentApiVersion)
        {
            if (currentApiVersion < startApiVersion)
                throw new ArgumentException($"{nameof(currentApiVersion)} must be >= {nameof(startApiVersion)}");

            _currentVersion = new ApiVersion(currentApiVersion.Major, currentApiVersion.Minor);
            var allVersions = new List<ApiVersion>();

            if (startApiVersion.Major == currentApiVersion.Major)
            {
                for (var minor = startApiVersion.Minor; minor <= currentApiVersion.Minor; minor++)
                {
                    allVersions.Add(new ApiVersion(startApiVersion.Major, minor));
                }
            }
            else
            {
                for (var major = startApiVersion.Major; major <= currentApiVersion.Major; major++)
                {
                    //Start
                    if (major != currentApiVersion.Major && major == startApiVersion.Major)
                    {
                        for (var minor = startApiVersion.Minor; minor <= MAX_MINOR_VERSION_SUPPORTED; minor++)
                        {
                            allVersions.Add(new ApiVersion(major, minor));
                        }
                    }
                    //Middle
                    else if (major != currentApiVersion.Major && major != startApiVersion.Major)
                    {
                        for (var minor = 0; minor <= MAX_MINOR_VERSION_SUPPORTED; minor++)
                        {
                            allVersions.Add(new ApiVersion(major, minor));
                        }
                    }
                    //End
                    else if (major == currentApiVersion.Major)
                    {
                        for (var minor = 0; minor <= currentApiVersion.Minor; minor++)
                        {
                            allVersions.Add(new ApiVersion(major, minor));
                        }
                    }
                }
            }

            AllVersions = allVersions;
        }
        
        internal ApiVersion[] GetSupportedVersions(ApiVersion introducedIn, ApiVersion removedAsOf = null)
        {
            if (introducedIn > _currentVersion)
                return new ApiVersion[0];
            
            if (removedAsOf is null)
                return new[] {_currentVersion};
            
            if (introducedIn > removedAsOf)
                throw new InvalidOperationException($"Cannot remove an API version ({removedAsOf}) before it has been introduced ({introducedIn}).");
            
            if (introducedIn == removedAsOf)
                throw new InvalidOperationException($"Cannot remove an API version ({removedAsOf}) in the same version it has been introduced ({introducedIn}).");
                        
            if (removedAsOf > _currentVersion)
                return new[] {_currentVersion};
            
            return new ApiVersion[0];
        }
        
        internal ApiVersion[] GetDeprecatedVersions(ApiVersion introducedIn, ApiVersion removedAsOf = null)
        {
            if (introducedIn == null)
                throw new ArgumentException($"{nameof(introducedIn)} cannot be null.");
            
            if (removedAsOf == null)
            {
                return AllVersions.Where(v => 
                    v >= introducedIn
                    && v < _currentVersion).ToArray();
            }

            return AllVersions.Where(v =>
                v >= introducedIn
                && v < removedAsOf).ToArray();
        }
    }
}