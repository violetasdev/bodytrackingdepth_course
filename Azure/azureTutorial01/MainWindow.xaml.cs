using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.ComponentModel;


// Kinect Library
using Microsoft.Azure.Kinect.Sensor;

namespace azureTutorial01
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Device kinect = null;

        public MainWindow()
        {
            // Open the device
            kinect = Device.Open();

            // Configure camera modes, otherwise it wont simply open/turn on
            this.kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true
            });

            this.InitializeComponent();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.kinect != null)
            {
                this.kinect.Dispose();
            }
        }

    }
}
