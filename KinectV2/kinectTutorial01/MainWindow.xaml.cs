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

// Kinect V2 Extensions
using Microsoft.Kinect;

/***
Body Tracking with depth sensor course SS2021
Kinect Tutorial 01: Access the sensor

Goal: Get access to the Kinect by adding the libraries
Result: An empty Window and the Kinect V2 lights on. We still havent call the feed into the interface!
**/
namespace kinectTutorial01
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Create the Kinect sensor object
        private KinectSensor kinectSensor = null;
        public MainWindow()
        {
            // Initialize the sensor
            kinectSensor = KinectSensor.GetDefault();
            // Open the sensor
            kinectSensor.Open();
            InitializeComponent();
        }
    }
}
