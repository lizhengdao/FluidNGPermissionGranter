﻿using System.Linq;
using System.Windows;
using SharpAdbClient;

namespace FluidNGPermissionGranter
{
    public partial class MainWindow : Window
    {
        private readonly AdbServer server = new AdbServer();
        private readonly string adbPath;
        //private LogWindow logWindow;
        private ADBGuide adbGuideWindow;

        public MainWindow()
        {
            InitializeComponent();
            adbPath = (System.IO.Directory.GetCurrentDirectory() + @"\adb\adb.exe"); // Getting path of ADB tools 
        }

        // Closing adb server after closing program
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var proc in System.Diagnostics.Process.GetProcessesByName("adb"))
            {
                proc.Kill();
            }
            Application.Current.Shutdown();
        }
        #region Button actions 

        private void GuideButton_Click(object sender, RoutedEventArgs e)
        {
            adbGuideWindow = new ADBGuide { ShowInTaskbar = false, Owner = Application.Current.MainWindow };
            adbGuideWindow.ShowDialog();
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            StartAdb();
            FindDevice();
        }

        private void GrantButton_Click(object sender, RoutedEventArgs e)
        {
            SendToAdb("pm grant com.fb.fluid android.permission.WRITE_SECURE_SETTINGS");
            MessageBox.Show("Done! Restart application to see the changes", "Congratulations");
            CloseButton.IsEnabled = true;
        }
       
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SendToAdb("am force-stop com.fb.fluid");
            SendToAdb("am start -n com.fb.fluid/com.fb.fluid.ActivityMain");
        }
        #endregion
        private void StartAdb()
        {
            // Trying to start ADB server and find device 
            try
            {
                var result = server.StartServer(adbPath, false);
                LogWindow.RichText += "\nADB server started successful: " + result;
            }
            catch
            {
                MessageBox.Show("Can't start ADB server. Please check Log and ask developer for it");
                LogWindow.RichText += "Can't start ADB server";
            }
        }

        //  Trying to find device and show message about it 
        private void FindDevice()
        {
            var devices = AdbClient.Instance.GetDevices();
            if(devices.Count != 0)
            {
                foreach (var device in devices)
                {
                    MessageBox.Show("The device has been found. Your device is: " + device.Name, "Message");
                    GrantButton.IsEnabled = true;
                }
            }
            else
            {
                MessageBox.Show("Can't find device. Please check your connection", "Message");
            }
        }

        private static string SendToAdb(string command)
        {
            try
            {
                var device = AdbClient.Instance.GetDevices().First();
                var receiver = new ConsoleOutputReceiver();
                AdbClient.Instance.ExecuteRemoteCommand(command, device, receiver);
                return receiver.ToString();
            }
            catch
            {
                MessageBox.Show("Connection lost. Check your USB cable and try again.");
                return "Connection lost. Check your USB cable and try again";
            }
        }
    }
}