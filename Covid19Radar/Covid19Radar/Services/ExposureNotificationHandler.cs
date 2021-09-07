﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Acr.UserDialogs;
using CommonServiceLocator;
using Covid19Radar.Model;
using Covid19Radar.Resources;
using Covid19Radar.Services.Logs;
using Covid19Radar.Services.Migration;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Xamarin.ExposureNotifications;

namespace Covid19Radar.Services
{
    [Xamarin.Forms.Internals.Preserve] // Ensure this isn't linked out
    public class ExposureNotificationHandler : IExposureNotificationHandler
    {
        private ILoggerService LoggerService => ServiceLocator.Current.GetInstance<ILoggerService>();
        private IHttpDataService HttpDataService => ServiceLocator.Current.GetInstance<IHttpDataService>();
        private IExposureNotificationService ExposureNotificationService => ServiceLocator.Current.GetInstance<IExposureNotificationService>();
        private IUserDataService UserDataService => ServiceLocator.Current.GetInstance<IUserDataService>();
        private IMigrationService MigrationService => ServiceLocator.Current.GetInstance<IMigrationService>();
        private ILocalNotificationService LocalNotificationService => ServiceLocator.Current.GetInstance<ILocalNotificationService>();

        public ExposureNotificationHandler()
        {
            // Do not initialize the field variables here.
            LoggerService.Info("Initialized exposuer notification handler");
        }

        // this string should be localized
        public string UserExplanation
            => AppResources.LocalNotificationDescription;

        // this configuration should be obtained from a server and it should be cached locally/in memory as it may be called multiple times
        public Task<Configuration> GetConfigurationAsync()
        {
            var loggerService = LoggerService;
            loggerService.StartMethod();

            var configuration = ExposureNotificationService.GetConfiguration();
            if (configuration != null)
            {
                loggerService.Info("Get configuration from cached");
                loggerService.EndMethod();
                return Task.FromResult(configuration);
            }

            configuration = new Configuration
            {
                MinimumRiskScore = 21,
                AttenuationWeight = 50,
                TransmissionWeight = 50,
                DurationWeight = 50,
                DaysSinceLastExposureWeight = 50,
                TransmissionRiskScores = new int[] { 7, 7, 7, 7, 7, 7, 7, 7 },
                AttenuationScores = new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                DurationScores = new[] { 0, 0, 0, 0, 1, 1, 1, 1 },
                DaysSinceLastExposureScores = new[] { 1, 1, 1, 1, 1, 1, 1, 1 },
                DurationAtAttenuationThresholds = new[] { 50, 70 }
            };

            loggerService.Info("Get default configuration");

            var defaultConfiguration = Task.FromResult(configuration);
            loggerService.Info($"configuration: {JsonConvert.SerializeObject(configuration)}");

            loggerService.EndMethod();
            return defaultConfiguration;
        }

        // this will be called when a potential exposure has been detected
        public async Task ExposureDetectedAsync(ExposureDetectionSummary summary, Func<Task<IEnumerable<ExposureInfo>>> getExposureInfo)
        {
            var loggerService = LoggerService;
            loggerService.StartMethod();

            var exposureNotificationService = ExposureNotificationService;
            var exposureInformationList = exposureNotificationService.GetExposureInformationList() ?? new List<UserExposureInfo>();

            UserExposureSummary userExposureSummary = new UserExposureSummary(summary.DaysSinceLastExposure, summary.MatchedKeyCount, summary.HighestRiskScore, summary.AttenuationDurations, summary.SummationRiskScore);

            loggerService.Info($"ExposureSummary.MatchedKeyCount: {userExposureSummary.MatchedKeyCount}");
            loggerService.Info($"ExposureSummary.DaysSinceLastExposure: {userExposureSummary.DaysSinceLastExposure}");
            loggerService.Info($"ExposureSummary.HighestRiskScore: {userExposureSummary.HighestRiskScore}");
            loggerService.Info($"ExposureSummary.AttenuationDurations: {string.Join(",", userExposureSummary.AttenuationDurations)}");
            loggerService.Info($"ExposureSummary.SummationRiskScore: {userExposureSummary.SummationRiskScore}");

            var config = await GetConfigurationAsync();

            var isNewExposureDetected = false;

            if (userExposureSummary.HighestRiskScore >= config.MinimumRiskScore)
            {
                var exposureInfo = await getExposureInfo();
                loggerService.Info($"ExposureInfo: {exposureInfo.Count()}");

                foreach (var exposure in exposureInfo)
                {
                    loggerService.Info($"Exposure.Timestamp: {exposure.Timestamp}");
                    loggerService.Info($"Exposure.Duration: {exposure.Duration}");
                    loggerService.Info($"Exposure.AttenuationValue: {exposure.AttenuationValue}");
                    loggerService.Info($"Exposure.TotalRiskScore: {exposure.TotalRiskScore}");
                    loggerService.Info($"Exposure.TransmissionRiskLevel: {exposure.TransmissionRiskLevel}");

                    if (exposure.TotalRiskScore >= config.MinimumRiskScore)
                    {
                        //UserExposureInfo userExposureInfo = new UserExposureInfo(exposure.Timestamp, exposure.Duration, exposure.AttenuationValue, exposure.TotalRiskScore, (RiskLevel)exposure.TransmissionRiskLevel);
                        //exposureInformationList.Add(userExposureInfo);
                        //isNewExposureDetected = true;
                    }
                }
            }

            if (isNewExposureDetected)
            {
                loggerService.Info($"Save ExposureSummary. MatchedKeyCount: {userExposureSummary.MatchedKeyCount}");
                loggerService.Info($"Save ExposureInformation. Count: {exposureInformationList.Count}");

                exposureInformationList.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
                exposureNotificationService.SetExposureInformation(userExposureSummary, exposureInformationList);

                await LocalNotificationService.ShowExposureNotificationAsync();
            }
            else
            {
                loggerService.Info($"MatchedKeyCount: {userExposureSummary.MatchedKeyCount}, but no new exposure detected");
            }

            loggerService.EndMethod();
        }

        private static int fetchExposureKeysIsRunning = 0;

        // this will be called when they keys need to be collected from the server
        public async Task FetchExposureKeyBatchFilesFromServerAsync(Func<IEnumerable<string>, Task> submitBatches, CancellationToken cancellationToken)
        {
            var loggerService = LoggerService;
            var exposureNotificationService = ExposureNotificationService;

            if (Interlocked.Exchange(ref fetchExposureKeysIsRunning, 1) == 1)
            {
                loggerService.Info("Skipped");
                return;
            }

            loggerService.StartMethod();

            try
            {
                await MigrationService.MigrateAsync();

                foreach (var serverRegion in AppSettings.Instance.SupportedRegions)
                {
                    var lastCreated = exposureNotificationService.GetLastProcessTekTimestamp(serverRegion);
                    loggerService.Info($"region: {serverRegion}, lastCreated: {lastCreated}");

                    cancellationToken.ThrowIfCancellationRequested();

                    loggerService.Info("Start download files");

                    var (newCreated, downloadedFiles) = await DownloadBatchAsync(serverRegion, lastCreated, cancellationToken);
                    loggerService.Info("End to download files");
                    loggerService.Info($"Downloaded files: {downloadedFiles.Count}, newCreated: {newCreated}");

                    if (newCreated == -1 || downloadedFiles.Count == 0)
                    {
                        continue;
                    }

                    loggerService.Info("C19R Submit Batches");
                    await submitBatches(downloadedFiles);

                    exposureNotificationService.SetLastProcessTekTimestamp(serverRegion, newCreated);
                    loggerService.Info($"region: {serverRegion}, lastCreated: {newCreated}");

                    // delete all temporary files
                    foreach (var file in downloadedFiles)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            // no-op
                            loggerService.Exception("Fail to delete downloaded files", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // any exceptions, throw and wait for retry
                loggerService.Exception("Fail to download files", ex);

                throw ex;
            }
            finally
            {
                loggerService.EndMethod();
                Interlocked.Exchange(ref fetchExposureKeysIsRunning, 0);
            }
        }

        private async Task<(long, List<string>)> DownloadBatchAsync(string region, long startTimestamp, CancellationToken cancellationToken)
        {
            var loggerService = LoggerService;
            loggerService.StartMethod();

            var downloadedFiles = new List<string>();
            var tmpDir = Path.Combine(FileSystem.CacheDirectory, region);

            try
            {
                if (!Directory.Exists(tmpDir))
                {
                    Directory.CreateDirectory(tmpDir);
                }
            }
            catch (Exception ex)
            {
                loggerService.Exception("Failed to create directory", ex);
                loggerService.EndMethod();
                // catch error return newCreated -1 / downloadedFiles 0
                return (-1, downloadedFiles);
            }

            var httpDataService = HttpDataService;

            List<TemporaryExposureKeyExportFileModel> tekList = await httpDataService.GetTemporaryExposureKeyList(region, cancellationToken);
            if (tekList.Count == 0)
            {
                loggerService.EndMethod();
                return (-1, downloadedFiles);
            }
            Debug.WriteLine("C19R Fetch Exposure Key");

            var newCreated = startTimestamp;
            foreach (var tekItem in tekList)
            {
                loggerService.Info($"tekItem.Created: {tekItem.Created}");
                if (tekItem.Created > startTimestamp)
                {
                    var tmpFile = Path.Combine(tmpDir, Guid.NewGuid().ToString() + ".zip");
                    Debug.WriteLine(JsonConvert.SerializeObject(tekItem));
                    Debug.WriteLine(tmpFile);

                    loggerService.Info($"Download TEK file. url: {tekItem.Url}");
                    using (Stream responseStream = await httpDataService.GetTemporaryExposureKey(tekItem.Url, cancellationToken))
                    using (var fileStream = File.Create(tmpFile))
                    {
                        try
                        {
                            await responseStream.CopyToAsync(fileStream, cancellationToken);
                            fileStream.Flush();
                        }
                        catch (Exception ex)
                        {
                            loggerService.Exception("Fail to copy", ex);
                        }
                    }
                    newCreated = tekItem.Created;
                    downloadedFiles.Add(tmpFile);
                    Debug.WriteLine($"C19R FETCH DIAGKEY {tmpFile}");
                }
            }
            loggerService.Info($"Downloaded files: {downloadedFiles.Count()}");

            loggerService.EndMethod();

            return (newCreated, downloadedFiles);
        }

        // this will be called when the user is submitting a diagnosis and the local keys need to go to the server
        public async Task UploadSelfExposureKeysToServerAsync(IEnumerable<TemporaryExposureKey> temporaryExposureKeys)
        {
        }
    }
}
