//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
//
//*********************************************************

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using BLE_UART;
using BLE_UART.Common;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;



namespace BLE_UART
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UARTMain : Page
    {
        
        public UARTMain()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached. The Parameter property is typically
        /// used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (UARTService.Instance.IsServiceInitialized)
            {
                foreach (var measurement in UARTService.Instance.DataPoints)
                {
                    outputListBox.Items.Add("RX: "+measurement.ToString());
                }
                
                RunButton.IsEnabled = false;
            }
            UARTService.Instance.ValueChangeCompleted += Instance_ValueChangeCompleted;

        }
        
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            UARTService.Instance.ValueChangeCompleted -= Instance_ValueChangeCompleted;
        }

        private async void Instance_ValueChangeCompleted(UARTElement UARTElementValue)
        {
            // Serialize UI update to the the main UI thread.
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                statusTextBlock.Text = "Received: " +
                    UARTElementValue.ToString();                
                outputListBox.Items.Insert(0, UARTElementValue);
            });
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;

            //var devices = await DeviceInformation.FindAllAsync();


            var devices = await DeviceInformation.FindAllAsync(
                GattDeviceService.GetDeviceSelectorFromUuid(UARTService.Instance.SERVICE_UUID),
                new string[] { "System.Devices.ContainerId" });

            DevicesListBox.Items.Clear();

            if (devices.Count > 0)
            {
                foreach (var device in devices)
                {
                    DevicesListBox.Items.Add(device);
                }
                DevicesListBox.Visibility = Visibility.Visible;
            }
            else
            {
                statusTextBlock.Text = "No Devices with UART Service Found!";
            }
            RunButton.IsEnabled = true;
        }

        private async void DevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            RunButton.IsEnabled = false;
            var device = DevicesListBox.SelectedItem as DeviceInformation;
            DevicesListBox.Visibility = Visibility.Collapsed;

            statusTextBlock.Text = "Initializing device...";
            UARTService.Instance.DeviceConnectionUpdated += OnDeviceConnectionUpdated;
            await UARTService.Instance.InitializeServiceAsync(device);

            try
            {
                // Check if the device is initially connected, and display the appropriate message to the user
                var deviceObject = await PnpObject.CreateFromIdAsync(PnpObjectType.DeviceContainer,
                    device.Properties["System.Devices.ContainerId"].ToString(), 
                    new string[] { "System.Devices.Connected" });
            
                bool isConnected;
                if (Boolean.TryParse(deviceObject.Properties["System.Devices.Connected"].ToString(), out isConnected))
                {
                    OnDeviceConnectionUpdated(isConnected);
                }
            } 
            catch (Exception)
            {

            }
        }

        private async void OnDeviceConnectionUpdated(bool isConnected)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (isConnected)
                {
                    statusTextBlock.Text = "Connected!";
                }
                else
                {
                    statusTextBlock.Text = "Waiting for device to connect...";
                }
            });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // get data from the textbox the user types into
                string data = sendThisText.Text;

                // check to make sure length is less than 20 characters
                int length = data.Length;

                // send the data using the function UART_Transmit
                await UARTService.Instance.UART_Transmit(data, length);
                sendThisText.Text = "";

                outputListBox.Items.Add("TX: " + data);

            }
            catch
            {
                
            }

        }

        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
        }
    }
}
