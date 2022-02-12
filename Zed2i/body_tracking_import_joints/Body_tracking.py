########################################################################
#
# Copyright (c) 2021, STEREOLABS.
#
# All rights reserved.
#
# THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
# "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
# LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
# A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
# OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
# SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
# LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
# DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
# THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
# (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
# OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#
########################################################################

"""
   This sample shows how to detect a human bodies and draw their 
   modelised skeleton in an OpenGL window
"""
import cv2
import sys
import pyzed.sl as sl
from ogl_viewer import viewer as gl
from cv_viewer import tracking_viewer as cv_viewer
from cv_viewer import tracking_importer as cv_importer
import numpy as np

import pandas as pd
import datetime

if __name__ == "__main__":
    print("Running Body Tracking sample ... Press 'q' to quit")

    # Create a Camera object
    zed = sl.Camera()

    # Create a InitParameters object and set configuration parameters
    init_params = sl.InitParameters()
    init_params.camera_resolution = sl.RESOLUTION.HD1080  # Use HD1080 video mode
    init_params.coordinate_units = sl.UNIT.METER          # Set coordinate units
    init_params.depth_mode = sl.DEPTH_MODE.ULTRA
    init_params.coordinate_system = sl.COORDINATE_SYSTEM.IMAGE #HERE EDIT YOUR COORDINATE SYSTEM IF YOU NEED SOMETHING DIFFERENT There are 6 options
    # If applicable, use the SVO given as parameter
    # Otherwise use ZED live stream
    if len(sys.argv) == 2:
        filepath = sys.argv[1]
        print("Using SVO file: {0}".format(filepath))
        init_params.svo_real_time_mode = True
        init_params.set_from_svo_file(filepath)

    # Open the camera
    err = zed.open(init_params)
    if err != sl.ERROR_CODE.SUCCESS:
        exit(1)

    # Enable Positional tracking (mandatory for object detection)
    positional_tracking_parameters = sl.PositionalTrackingParameters()
    # If the camera is static, uncomment the following line to have better performances and boxes sticked to the ground.
    # positional_tracking_parameters.set_as_static = True
    zed.enable_positional_tracking(positional_tracking_parameters)

    
    obj_param = sl.ObjectDetectionParameters()
    obj_param.enable_body_fitting = True            # Smooth skeleton move
    obj_param.enable_tracking = True                # Track people across images flow
    obj_param.detection_model = sl.DETECTION_MODEL.HUMAN_BODY_FAST 

    obj_param.body_format = sl.BODY_FORMAT.POSE_34  # Choose the BODY_FORMAT you wish to use 18 or 34

    # Enable Object Detection module
    zed.enable_object_detection(obj_param)

    obj_runtime_param = sl.ObjectDetectionRuntimeParameters()
    obj_runtime_param.detection_confidence_threshold = 40

    # Get ZED camera information
    camera_info = zed.get_camera_information()

    # 2D viewer utilities
    display_resolution = sl.Resolution(min(camera_info.camera_resolution.width, 1280), min(camera_info.camera_resolution.height, 720))
    image_scale = [display_resolution.width / camera_info.camera_resolution.width
                 , display_resolution.height / camera_info.camera_resolution.height]

    # Create OpenGL viewer
    viewer = gl.GLViewer()
    viewer.init(camera_info.calibration_parameters.left_cam, obj_param.enable_tracking,obj_param.body_format)

    # Create ZED objects filled in the main loop
    bodies = sl.Objects()
    image = sl.Mat()

    #Create variables for JSON file
    date_exp=datetime.datetime.now()
    data_body_list=[]

    exp_name=f"Zed2i_{date_exp.date().strftime('%Y-%m-%d')}_{date_exp.hour}{date_exp.minute}"

    if len(bodies.object_list) > 0:
        first_object = bodies.object_list[0]
        print(first_object)
        # Display the 3D keypoint coordinates of the first detected person
        print("\n Keypoint 3D ")

    start_time = datetime.datetime.now()
    while viewer.is_available():
        # Grab an image
        if zed.grab() == sl.ERROR_CODE.SUCCESS:
            
            # Retrieve left image
            zed.retrieve_image(image, sl.VIEW.LEFT, sl.MEM.CPU, display_resolution)
            # Retrieve objects
            zed.retrieve_objects(bodies, obj_runtime_param)

            # Update GL view
            viewer.update_view(image, bodies) 
            # Update OCV view
            image_left_ocv = image.get_data()
            cv_viewer.render_2D(image_left_ocv,image_scale,bodies.object_list, obj_param.enable_tracking, obj_param.body_format)

            end_time = datetime.datetime.now()

            time_diff = (end_time - start_time)
            execution_time = time_diff.total_seconds() * 1000

            if execution_time > 200:                
                id_body, date, time_body, joints_body,x,y=cv_importer.import_body3D(bodies.object_list, obj_param.enable_tracking, obj_param.body_format)
                if id_body >-1:         
                    data_body_list.append([exp_name, date, '183', id_body,x,y, time_body, joints_body])
                
                start_time=datetime.datetime.now()

            cv2.imshow("ZED | 2D View", image_left_ocv)
            cv2.waitKey(10)

    viewer.exit()

    df = pd.DataFrame(data_body_list, columns=['ID_exp','date_exp','height','ID_subject','x','y','time', 'joints'])

    df.to_json(f"{exp_name}.json", orient='index')

    image.free(sl.MEM.CPU)
    # Disable modules and close camera
    zed.disable_object_detection()
    zed.disable_positional_tracking()
    zed.close()