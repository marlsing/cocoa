﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Covid19Radar.Repository;
using Covid19Radar.Resources;
using Covid19Radar.Services.Logs;
using Covid19Radar.Views;
using Prism.Navigation;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Covid19Radar.ViewModels
{
    public class SendLogSettingsPageViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IUserDataRepository _userDataRepository;
        private readonly ILoggerService _loggerService;

        private Destination _destination = Destination.HomePage;
        private INavigationParameters _navigationParameters;

        private SendEventLogState _sendEventLogState;
        public SendEventLogState SendEventLogState
        {
            get { return _sendEventLogState; }
            set
            {
                SetProperty(ref _sendEventLogState, value);
                UpdateButtonLabels(value);
            }
        }

        private void UpdateButtonLabels(SendEventLogState value)
        {
            if (value == SendEventLogState.Enable)
            {
                DisableButtonLabel = AppResources.ButtonFinishCooperate;
            }
            else if(value == SendEventLogState.Disable)
            {
                // Do nothing
            }
            else
            {
                DisableButtonLabel = AppResources.ButtonNotNow;
            }
        }

        private string _enableButtonLabel = AppResources.ButtonStartCooperate;
        public string EnableButtonLabel
        {
            get { return _enableButtonLabel; }
            set
            {
                SetProperty(ref _enableButtonLabel, value);
            }
        }

        private string _disableButtonLabel = AppResources.ButtonNotNow;
        public string DisableButtonLabel
        {
            get { return _disableButtonLabel; }
            set
            {
                SetProperty(ref _disableButtonLabel, value);
            }
        }

        public SendLogSettingsPageViewModel(
            INavigationService navigationService,
            IUserDataRepository userDataRepository,
            ILoggerService loggerService
            ) : base(navigationService)
        {
            _navigationService = navigationService;
            _userDataRepository = userDataRepository;
            _loggerService = loggerService;
        }

        public override void Initialize(INavigationParameters parameters)
        {
            base.Initialize(parameters);

            _navigationParameters = parameters;

            if (parameters.ContainsKey(SendLogSettingsPage.DestinationKey))
            {
                _destination = parameters.GetValue<Destination>(SendLogSettingsPage.DestinationKey);
            }

            SendEventLogState = _userDataRepository.GetSendEventLogState();

        }

        public Command OnClickEnableSendLog => new Command(async () =>
        {
            _loggerService.StartMethod();

            await UserDialogs.Instance.AlertAsync(
                AppResources.SendLogEnableSendLogDialogMessage,
                AppResources.SendLogEnableSendLogDialogTitle,
                AppResources.ButtonOk
                );

            _userDataRepository.SetSendEventLogState(SendEventLogState.Enable);

            _ = await NavigationService.NavigateAsync(_destination.ToPath(), _navigationParameters);

            _loggerService.EndMethod();

        });

        public Command OnClickDisableSendLog => new Command(async () =>
        {
            _loggerService.StartMethod();

            var title = AppResources.SendLogDisableSendLogDialogTitle1;
            if(SendEventLogState == SendEventLogState.Enable)
            {
                title = AppResources.SendLogDisableSendLogDialogTitle2;
            }

            await UserDialogs.Instance.AlertAsync(
                AppResources.SendLogDisableSendLogDialogMessage,
                title,
                AppResources.ButtonOk
                );

            _userDataRepository.SetSendEventLogState(SendEventLogState.Disable);

            _ = await NavigationService.NavigateAsync(_destination.ToPath(), _navigationParameters);

            _loggerService.EndMethod();
        });

        public Func<string, BrowserLaunchMode, Task> BrowserOpenAsync = Browser.OpenAsync;

        public Command OpenPrivacyPolicy => new Command(async () =>
        {
            _loggerService.StartMethod();

            var url = AppResources.UrlPrivacyPolicy;
            await BrowserOpenAsync(url, BrowserLaunchMode.SystemPreferred);

            _loggerService.EndMethod();
        });
    }
}
