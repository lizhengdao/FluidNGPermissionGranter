﻿using System;
using System.Windows;
using System.Diagnostics;

namespace FluidNGPermissionGranter
{
    public partial class ADBGuide
    {
        public ADBGuide()
        {
            InitializeComponent();
        }

        // Removing icon on the top of window 
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            IconHelper.RemoveIcon(this);
        }

        private void PixelButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://tgraph.io/Enable-developer-options-and-debugging-11-24-2");
        }

        private void SamsungButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.syncios.com/android/how-to-debug-samsung-galaxy-s9.html");
        }

        private void HuaweiButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.syncios.com/android/how-to-debug-huawei-honor-9.html");
        }

        private void XiaomiButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://tgraph.io/Enable-developer-options-and-debugging-11-25");
        }

        private void OtherNewButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://tgraph.io/Enable-developer-options-and-debugging-11-24-2");
        }

        private void OtherOldButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://tgraph.io/Enable-developer-options-and-debugging-11-24"); 
        }
    }
}