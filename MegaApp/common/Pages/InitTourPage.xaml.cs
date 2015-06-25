﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using MegaApp.Enums;
using MegaApp.Services;

namespace MegaApp.Pages
{
    public partial class InitTourPage : PhoneApplicationPage
    {
        public InitTourPage()
        {
            InitializeComponent();

            // Determine the visibility of the dark background.
            if(((Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"]) != Visibility.Visible)
            {
                ImgMegaSpace.Source = new BitmapImage(new Uri("/Assets/Images/01B_storage.png", UriKind.Relative));
                ImgMegaSpeed.Source = new BitmapImage(new Uri("/Assets/Images/02B_speed.png", UriKind.Relative));
                ImgMegaPrivacy.Source = new BitmapImage(new Uri("/Assets/Images/03B_security.png", UriKind.Relative));
                ImgMegaAccess.Source = new BitmapImage(new Uri("/Assets/Images/04B_access.png", UriKind.Relative));
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Remove the main page from the stack. If user presses back button it will then exit the application
            // Also removes the login page and the create account page after the user has created the account succesful            
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/LoginPage.xaml?item=0", UriKind.RelativeOrAbsolute));
        }

        private void btnCreateAccount_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/LoginPage.xaml?item=1", UriKind.RelativeOrAbsolute));            
        }
    }
}