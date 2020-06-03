﻿using Microsoft.Extensions.Configuration;
using System;

namespace Covid19Radar.Background
{
    public static class IConfigurationExtension
    {
        public static string CdnResourceGroupName(this IConfiguration config) => config["CdnResourceGroupName"];
        public static string CdnProfileName(this IConfiguration config) => config["CdnProfileName"];
        public static string CdnEndpointName(this IConfiguration config) => config["CdnEndpointName"];
        public static string TekExportBlobStorage(this IConfiguration config) => config["TekExportBlobStorage"];
        public static string TekExportBlobStorageContainerPrefix(this IConfiguration config) => config["TekExportBlobStorageContainerPrefix"];
        public static string TekExportKeyVaultKeyUrl(this IConfiguration config) => config["TekExportKeyVaultKeyUrl"];
        public static string iOSBundleId(this IConfiguration config) => config["iOSBundleId"];
        public static string AndroidPackageName(this IConfiguration config) => config["AndroidPackageName"];
        public static string[] SupportRegions(this IConfiguration config) => config["SupportRegions"]?.Split(',') ?? new string[] {};
        public static string VerificationKeyId(this IConfiguration config) => config["VerificationKeyId"];
        public static string VerificationKeyVersion(this IConfiguration config) => config["VerificationKeyVersion"];

    }
}
