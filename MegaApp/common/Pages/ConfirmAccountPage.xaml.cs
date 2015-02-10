﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MegaApp.Classes;
using MegaApp.Enums;
using MegaApp.Models;
using MegaApp.Services;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace MegaApp.Pages
{
    public partial class ConfirmAccountPage : PhoneApplicationPage
    {
        private readonly ConfirmAccountViewModel _confirmAccountViewModel;
        public ConfirmAccountPage()
        {
            _confirmAccountViewModel = new ConfirmAccountViewModel(App.MegaSdk, this);
            this.DataContext = _confirmAccountViewModel;

            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (NavigateService.ProcessQueryString(NavigationContext.QueryString) != NavigationParameter.UriLaunch) return;

            if (NavigationContext.QueryString.ContainsKey("confirm"))
            {
                _confirmAccountViewModel.ConfirmCode = HttpUtility.UrlDecode(NavigationContext.QueryString["confirm"]);
                if(_confirmAccountViewModel.ConfirmCode.StartsWith("mega://"))
                    _confirmAccountViewModel.ConfirmCode = _confirmAccountViewModel.ConfirmCode.Replace("mega://", "https://mega.co.nz/#");

                App.MegaSdk.querySignupLink(_confirmAccountViewModel.ConfirmCode, _confirmAccountViewModel);
            }                
        }
    }
}