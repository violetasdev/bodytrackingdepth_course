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

using System.ComponentModel;

using Microsoft.Kinect;

namespace kinectTutorial05
{
    public enum DisplayFrameType
    {
        Infrared,
        Color,
        Depth,
        Body
    }
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Setup interface option selection
        private const DisplayFrameType DEFAULT_DISPLAYFRAMETYPE = DisplayFrameType.Depth;

        /// Kinect Sensor
        private KinectSensor kinectSensor = null;

        // Object to write the image to show on the interface for DEPTH, COLOR and INFRARED sources
        private WriteableBitmap bitmap = null;
        private FrameDescription currentFrameDescription;
        private DisplayFrameType currentDisplayFrameType;
        // Reader to receive the information from the camera
        private MultiSourceFrameReader multiSourceFrameReader = null;


        // VARIABLES NEEDED FOR EACH FUNCTIONALITY //
        //***************** FOR INFRARED //
        /** Scale the values from 0 to 255 **/
        /// Setup the limits (post processing)of the infrared data that we will render.
        /// Increasing or decreasing this value sets a brightness "wall" either closer or further away.
        private const float InfraredOutputValueMinimum = 0.01f;
        private const float InfraredOutputValueMaximum = 1.0f;
        private const float InfraredSourceValueMaximum = ushort.MaxValue;
        /// Scale of Infrared source data
        private const float InfraredSourceScale = 0.75f;

        //***************** FOR DEPTH //
        /** Scale values for depth input**/
        /// Map depth range to byte range
        private const int MapDepthToByte = 8000 / 256;
        /// Intermediate storage for frame data converted to color
        private byte[] depthPixels = null;

        //***************** FOR BODY TRACKING //

        /// Control negative values of depth map: Constant for clamping Z values of camera space points
        private const float InferredZPositionClamp = 0.1f;

        //** Variables for creating and storing bones coordinates
        /// Coordinate mapper to map one type of point to another from DEPTH to COLOR
        private CoordinateMapper coordinateMapper = null;
        /// Array for the bodies
        private Body[] bodies = null;
        /// definition of bones
        private List<Tuple<JointType, JointType>> bones;
        /// List of colors for each body tracked
        private List<Pen> bodyColors;

        //*** Variables for drawing the JOINTS (points of intersection between bones): 
        /// Thickness of drawn joint lines
        private const double JointThickness = 3;
        /// Thickness of clip edge rectangles
        private const double ClipBoundsThickness = 10;
        /// Brush for tracked joints
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        /// Brush for inferred joints
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        /// Pen for drawing inferred bones
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        //** Variables for drawing the HANDS:
        /// Radius of drawn hand circles
        private const double HandSize = 20;
        /// Brush for closed hands
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        /// Brush for opened hands
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        /// Brush for pointed hands
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        
        //** Variables for the Image object to import the results of the skeleton
        /// Drawing group for body rendering output
        private DrawingGroup drawingGroup;
        /// Drawing image that we will display
        private DrawingImage imageSource;
        /// Width of display (depth space)
        private int displayWidthBody;
        /// Height of display (depth space)
        private int displayHeightBody;

       

        public MainWindow()
        {
            // Initialize the sensor
            this.kinectSensor = KinectSensor.GetDefault();

            // Open the reader for the  frames
            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader
                (FrameSourceTypes.Infrared
                | FrameSourceTypes.Color
                | FrameSourceTypes.Depth
                | FrameSourceTypes.Body);
            // Wire handler for frame arrival - This is a later defined method 
            this.multiSourceFrameReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            // Set up display frame types:
            // 1. Get FrameDescription
            // 2. Create the image object to display your data
            SetupCurrentDisplay(DEFAULT_DISPLAYFRAMETYPE);

            // We transfer the data to our Window defined in the XAML file
            this.DataContext = this;
            // Open the sensor
            this.kinectSensor.Open();

            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public ImageSource ImageSource
        {
            get
            {
                return this.bitmap;
            }
        }

        public FrameDescription CurrentFrameDescription
        {
            get { return this.currentFrameDescription; }
            set
            {
                if (this.currentFrameDescription != value)
                {
                    this.currentFrameDescription = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentFrameDescription"));
                    }
                }
            }
        }

        private void SetupCurrentDisplay(DisplayFrameType newDisplayFrameType)
        {
            currentDisplayFrameType = newDisplayFrameType;

            switch (currentDisplayFrameType)

            {
                case DisplayFrameType.Infrared:
                    FrameDescription infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;
                    this.CurrentFrameDescription = infraredFrameDescription;
                    // create the bitmap to display
                    this.bitmap = new WriteableBitmap(infraredFrameDescription.Width, infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray32Float, null);
                    break;

                case DisplayFrameType.Color:
                    FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
                    this.CurrentFrameDescription = colorFrameDescription;
                    // create the bitmap to display
                    this.bitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                    break;

                case DisplayFrameType.Depth:
                    FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
                    this.CurrentFrameDescription = depthFrameDescription;
                    // allocate space to put the pixels being received and converted
                    this.depthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height];
                    // create the bitmap to display
                    this.bitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
                    break;

                case DisplayFrameType.Body:
                    this.coordinateMapper = this.kinectSensor.CoordinateMapper;
                    FrameDescription bodyDepthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
                    this.CurrentFrameDescription = bodyDepthFrameDescription;

                    // get size of the scene
                    this.displayWidthBody = bodyDepthFrameDescription.Width;
                    this.displayHeightBody = bodyDepthFrameDescription.Height;

                    // Define a bone as the line between two joints
                    this.bones = new List<Tuple<JointType, JointType>>();
                    // Create the body bones
                    this.defineBoneParts();

                    // Populate body colors that you wish to show, one for each BodyIndex:
                    this.bodyColors = new List<Pen>();
                    this.bodyIndexColors();

                    // We need to create a drawing group
                    this.drawingGroup = new DrawingGroup();
                   
                    break;

                default:
                    break;
            }
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            switch (currentDisplayFrameType)
            {
                case DisplayFrameType.Infrared:
                    using (InfraredFrame infraredFrame = multiSourceFrame.InfraredFrameReference.AcquireFrame())
                    {
                        ShowInfraredFrame(infraredFrame);
                    }
                    break;
                case DisplayFrameType.Color:
                    using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        ShowColorFrame(colorFrame);
                    }
                    break;
                case DisplayFrameType.Depth:
                    using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                    {
                        ShowDepthFrame(depthFrame);
                    }
                    break;
                case DisplayFrameType.Body:
                    using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                    {
                        ShowBodyFrame(bodyFrame);
                    }
                    break;
                default:
                    break;
            }
        }
        private void ShowColorFrame(ColorFrame colorFrame)
        {
            if (colorFrame != null)
            {
                FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                {
                    this.bitmap.Lock();

                    // verify data and write the new color frame data to the display bitmap
                    if ((colorFrameDescription.Width == this.bitmap.PixelWidth) && (colorFrameDescription.Height == this.bitmap.PixelHeight))
                    {
                        colorFrame.CopyConvertedFrameDataToIntPtr(this.bitmap.BackBuffer, (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                            ColorImageFormat.Bgra);

                        this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));
                    }

                    this.bitmap.Unlock();
                    FrameDisplayImage.Source = this.bitmap;
                }
            }

        }

        private void ShowInfraredFrame(InfraredFrame infraredFrame)
        {

            // InfraredFrame is IDisposable

            if (infraredFrame != null)
            {
                FrameDescription infraredFrameDescription = infraredFrame.FrameDescription;
                /// We are using WPF (Windows Presentation Foundation)
                using (KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                {
                    // verify data and write the new infrared frame data to the display bitmap
                    if (((infraredFrameDescription.Width * infraredFrameDescription.Height) == (infraredBuffer.Size / infraredFrameDescription.BytesPerPixel)) &&
                        (infraredFrameDescription.Width == this.bitmap.PixelWidth) && (infraredFrameDescription.Height == this.bitmap.PixelHeight))
                    {
                        this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size, infraredFrameDescription);
                    }
                }
            }
        }


        private void ShowDepthFrame(DepthFrame depthFrame)
        {
            if (depthFrame != null)
            {
                FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                // the fastest way to process the body index data is to directly access the underlying buffer
                using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                {
                    // verify data and write the color data to the display bitmap
                    if (((depthFrameDescription.Width * depthFrameDescription.Height) == (depthBuffer.Size / depthFrameDescription.BytesPerPixel)) &&
                        (depthFrameDescription.Width == this.bitmap.PixelWidth) && (depthFrameDescription.Height == this.bitmap.PixelHeight))
                    {
                        // Note: In order to see the full range of depth (including the less reliable far field depth) we are setting maxDepth to the extreme potential depth threshold
                        ushort maxDepth = ushort.MaxValue;
                        this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth, depthFrameDescription);

                    }
                }
            }
        }

        private void ShowBodyFrame(BodyFrame bodyFrame)
        {
            bool dataReceived = false;
            if (bodyFrame != null)
            {

                if (this.bodies == null)
                {
                    this.bodies = new Body[bodyFrame.BodyCount];
                }

                // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                // As long as those body objects are not disposed/eliminated and not set to null in the array,
                // those body objects will be re-used.
                bodyFrame.GetAndRefreshBodyData(this.bodies);
                dataReceived = true;
            }


            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.DarkViolet, null, new Rect(0.0, 0.0, this.displayWidthBody, this.displayHeightBody));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                Console.WriteLine(position.Z);
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);
                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // Draw only in the area visible for the camera
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidthBody, this.displayHeightBody));
                }
                // Send to our UI/Interface the created bodies to display in the Image:
                FrameDisplayImage.Source = new DrawingImage(this.drawingGroup);

            }
        }

        // ***************************************************************************//
        // ************************* BODY DATA PROCESSING **************************//

        /// <summary>
        /// Draws a body
        /// </summary>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// Draws one bone of a body (joint to joint)
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            // A bone results from the union of two joints/vertices
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, we cannot draw them! Exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso ergo pointing
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// Draws indicators to show which edges are clipping body data
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Indigo,
                    null,
                    new Rect(0, this.displayHeightBody - ClipBoundsThickness, this.displayWidthBody, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Indigo,
                    null,
                    new Rect(0, 0, this.displayWidthBody, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Indigo,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeightBody));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Indigo,
                    null,
                    new Rect(this.displayWidthBody - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeightBody));
            }
        }

        /// <summary>
        ///  This colors are for the bodies detected by the camera, for the Kinect V2 a maximum of 6
        /// </summary>
        private void bodyIndexColors()
        {

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

        }

        /// <summary>
        /// Define which parts are connected between them
        /// </summary>
        private void defineBoneParts()
        {
            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

        }

        // ***************************************************************************//
        // ************************* DEPTH DATA PROCESSING **************************//

        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth, FrameDescription depthFrameDescription)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            this.bitmap.Lock();

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }

            this.bitmap.WritePixels(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight), this.depthPixels, this.bitmap.PixelWidth, 0);

            this.bitmap.Unlock();
            FrameDisplayImage.Source = this.bitmap;
        }


        // ***************************************************************************//
        // ************************* INFRARED DATA PROCESSING **************************//


        /// Directly accesses the underlying image buffer of the InfraredFrame to create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct access to the native memory pointed to by the infraredFrameData pointer.
        /// Activate "unsafe" in the solution properties > on the left >Build > Check Allow unsafe code
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize, FrameDescription infraredFrameDescription)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // lock the target bitmap
            this.bitmap.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)this.bitmap.BackBuffer;

            // process the infrared data
            for (int i = 0; i < (int)(infraredFrameDataSize / infraredFrameDescription.BytesPerPixel); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            // mark the entire bitmap as needing to be drawn
            this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));

            // unlock the bitmap
            this.bitmap.Unlock();
            FrameDisplayImage.Source = this.bitmap;
        }

        // ***************************************************************************//
        // *************************    BUTTONS ACTIONS    **************************//

        private void Button_Color(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Color);

        }

        private void Button_Infrared(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Infrared);
        }

        private void Button_Depth(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Depth);
        }

        private void Button_Body(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Body);
        }
    }
}
