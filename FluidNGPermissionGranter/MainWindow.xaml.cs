﻿using SharpAdbClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace FluidNGPermissionGranter
{
    public partial class MainWindow
    {
        #region ADB stuff

        private readonly AdbServer server = new AdbServer();
        private readonly string adbPath;
        private DeviceMonitor monitor;  //  Device monitor. Its searching for device after creating 

        #endregion

        #region Windows

        private ADBGuide adbGuideWindow;    //  There you can find guide about activating ADB on phone
        private ConnectGuide connectWindow;     // That's a connect message 
        private AuthorizeWindow authorizeWindow;      // That's an authorize adb server message
        private DoneWindow doneWindow;      //  This window appears when everything is done

        #endregion

        private readonly Action phoneAuthorized;
        private static DeviceData device;
        private Thread authorizationCheckThread;

        public MainWindow()
        {
            InitializeComponent();
            adbPath = (Directory.GetCurrentDirectory() + @"\adb\adb.exe"); // Getting path of ADB tools 
            phoneAuthorized += AfterAuthorization;  // Setting method for action 
        }

        // Removing icon on the top of the window 
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            IconHelper.RemoveIcon(this);
        }

        #region Button methods 

        private void GuideButton_Click(object sender, RoutedEventArgs e)
        {
            OpenGuideWindow();
        }

        private void GrantButton_Click(object sender, RoutedEventArgs e)
        {
            StartAdb();
            try
            {
                var devices = AdbClient.Instance.GetDevices();
                if (devices != null && devices.Count != 0)
                {
                    StartDeviceMonitor();
                }
                else
                {
                    StartDeviceMonitor();
                    OpenConnectWindow();
                }
            }
            catch (Exception exception)
            {
                MessageBoxResult result = MessageBox.Show("Some error detected. Please, send screenshot with this text to developer. Do you want to send now?: \n \n" + exception, "Error", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes) { Process.Start("https://dubzer.github.io"); }
            }
        }

        #endregion

        #region Windows methods

        private void OpenGuideWindow()
        {
            adbGuideWindow = new ADBGuide { ShowInTaskbar = false, Owner = Application.Current.MainWindow };
            adbGuideWindow.ShowDialog();
        }

        private void OpenConnectWindow()
        {
            connectWindow = new ConnectGuide { Title = "", Owner = Application.Current.MainWindow };
            connectWindow.ShowDialog();
        }

        private void CloseConnectWindow()
        {
            connectWindow.Close();
            connectWindow = null;
        }

        private void CloseAuthorizeWindow()
        {
            authorizeWindow.Close();
            authorizeWindow = null;
        }

        #endregion

        private void StartAdb()
        {
            // Trying to start ADB server and find device 
            try
            {
                server.StartServer(adbPath, false);
            }
            catch
            {
                MessageBox.Show(File.Exists(adbPath)
                    ? "Can't start ADB server. Please, check log and ask developer for it"
                    : "Can't find ADB server EXE. Please, redownload app. ");
            }
        }

        private void StartDeviceMonitor()
        {
            if (monitor == null)
            {
                CreateDeviceMonitor();
                monitor.Start();
            }
            else
            {
                monitor = null;
                CreateDeviceMonitor();
                monitor.Start();
            }
        }

        private void CreateDeviceMonitor()
        {
            monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
            monitor.DeviceConnected += OnDeviceConnected;
        }

        private void OnDeviceConnected(object sender, DeviceDataEventArgs e)
        {
            Thread.Sleep(500);  //  Giving time for thinking to device
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (connectWindow != null)
                    CloseConnectWindow();

                switch (AdbClient.Instance.GetDevices().First().State)
                {
                    case DeviceState.Online:
                        try
                        {
                            GrantPermission();
                        }
                        catch
                        {
                            // ignored
                        }
                        break;
                    case DeviceState.Unauthorized:
                        device = AdbClient.Instance.GetDevices().First();

                        //Showing help window to authorize server
                        authorizeWindow = new AuthorizeWindow() { Owner = Application.Current.MainWindow };

                        authorizeWindow.Show();
                        AdbClient.Instance.GetDevices();

                        authorizeWindow.ContentRendered += AuthorizeWindowContentRendered;
                        break;
                    default:
                        OpenConnectWindow();
                        break;
                }
            }));
        }

        private void AuthorizeWindowContentRendered(object sender, EventArgs e)
        {
            authorizationCheckThread = new Thread(StartCheckForAuthorization);
            authorizationCheckThread.Start();
        }

        private void StartCheckForAuthorization()
        {
            //  Waiting to get Online phone
            while (AdbClient.Instance.GetDevices().Any() &&
                   AdbClient.Instance.GetDevices().First().State == DeviceState.Unauthorized)
            {
                Thread.Sleep(300);
            }
            if(AdbClient.Instance.GetDevices().Any())
                phoneAuthorized();
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    CloseAuthorizeWindow();
                    OpenConnectWindow();
                }));
            }
        }

        private void AfterAuthorization()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => { authorizeWindow.Close(); }));

            GrantPermission();
            authorizeWindow.ContentRendered -= AuthorizeWindowContentRendered;

        }

        private void GrantPermission()
        {

            string output = SendToAdb("pm grant com.fb.fluid android.permission.WRITE_SECURE_SETTINGS");    //  Trying to grant permission 
            //  If output is *nothing*, then it means that there is no any errors, and we can show Successful window
            if (output == "")
            {
                AfterGranting("success");
            }

        }

        private static string SendToAdb(string command)
        {
            //  Trying to send command to device via ADB
            try
            {
                device = AdbClient.Instance.GetDevices().First();
                var receiver = new ConsoleOutputReceiver();
                AdbClient.Instance.ExecuteRemoteCommand(command, device, receiver);
                return receiver.ToString();
            }
            // if can't => show error message and give a link to contact developer
            catch (Exception exception)
            {
                return exception.ToString();
            }
        }

        private void AfterGranting(string grantOutput)
        {
            if (grantOutput == "success")
            {

                monitor.DeviceConnected -= OnDeviceConnected;

                //  Restarting Fluid Navigation Gestures app on device
                SendToAdb("am force-stop com.fb.fluid");
                SendToAdb("am start -n com.fb.fluid/com.fb.fluid.ActivityMain");
                //  Showing "done" window 
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    doneWindow = new DoneWindow { Title = "", Owner = Application.Current.MainWindow };
                    doneWindow.Show();
                }));
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Can`t send command by ADB. Please check your connection and try again. Also, try to redo the second step. Moreover, you can ask for help to the developer: \n \n" + grantOutput, "Error", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                    Process.Start("https://dubzer.github.io");
            }

        }

        // Stopping ADB server after closing program
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopApplication();
        }
        
        public static void StopApplication()
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName("adb"))
                {
                    proc.Kill();
                }
            }
            catch
            {
                Environment.Exit(0);
            }
            Environment.Exit(0);
        }

        //  will refactor later
        private void RestoreAdb()
        {
            using (WebClient wc = new WebClient())
            {
                wc.DownloadFile(new Uri("https://dubzer.github.io/redirects/adb.html"),
                    "adb.zip");
            }
        }

        private void DebugButton_OnClick(object sender, RoutedEventArgs e)
        {
            RestoreAdb();
        }
    }
}