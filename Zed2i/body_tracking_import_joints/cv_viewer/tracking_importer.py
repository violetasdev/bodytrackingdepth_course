import cv2
import numpy as np

from cv_viewer.utils import *
import pyzed.sl as sl

import datetime


#----------------------------------------------------------------------
#       2D VIEW
#----------------------------------------------------------------------
def cvt(pt, scale):
    '''
    Function that scales point coordinates
    '''
    out = [pt[0]*scale[0], pt[1]*scale[1]]
    return out

def import_body3D(objects, is_tracking_on, body_format):
    '''
    Parameters
        left_display (np.array): numpy array containing image data
        img_scale (list[float])
        objects (list[sl.ObjectData]) 
    '''

    id_body=-1 
    time_body=-1
    date=-1
    joints_body=-1
    x=0
    y=0

    # Render skeleton joints and bones
    for obj in objects:
        if render_object(obj, is_tracking_on):
            
            if len(obj.keypoint) > 0:
                # POSE_18

                # if body_format == sl.BODY_FORMAT.POSE_18:
                #     #Get skeleton data joints

                #     joints={
                #         'Nose':[obj.keypoint[0][0]*-1,obj.keypoint[0][1],obj.keypoint[0][2]],
                #         'Neck':[obj.keypoint[1][0]*-1,obj.keypoint[1][1],obj.keypoint[1][2]],

                #         'ShoulderRight':[obj.keypoint[2][0]*-1,obj.keypoint[2][1],obj.keypoint[2][2]],
                #         'ElbowRight':[obj.keypoint[3][0]*-1,obj.keypoint[3][1],obj.keypoint[3][2]],
                #         'WristRight':[obj.keypoint[4][0]*-1,obj.keypoint[4][1],obj.keypoint[4][2]],
                #         'ShoulderLeft':[obj.keypoint[5][0]*-1,obj.keypoint[5][1],obj.keypoint[5][2]],
                #         'ElbowLeft':[obj.keypoint[6][0]*-1,obj.keypoint[6][1],obj.keypoint[6][2]],,
                #         'WristLeft':[obj.keypoint[7][0]*-1,obj.keypoint[7][1],obj.keypoint[7][2]],,

                #         'HipRight':obj.keypoint[8],
                #         'KneeRight':obj.keypoint[9],
                #         'AnkleRight':obj.keypoint[10],
                #         'HipLeft':obj.keypoint[11],
                #         'KneeLeft':obj.keypoint[12],
                #         'AnkleLeft':obj.keypoint[13],

                #         'EyeRight':obj.keypoint[14],
                #         'EyeLeft':obj.keypoint[15],

                #         'EarRight':obj.keypoint[16],
                #         'EarLeft':obj.keypoint[17],
                #     }
                    
                #     #ID_body, time_stamp, joints
                #     id_body=obj.id
                #     date=str(datetime.datetime.now())
                #     time_body=str(datetime.datetime.now())
                #     joints_body=joints
                #     x=obj.keypoint[1][0]
                #     y=obj.keypoint[1][1]

    
                # elif body_format == sl.BODY_FORMAT.POSE_34:
                    #Get skeleton data joints
                    joints={
                        'Pelvis':[obj.keypoint[0][0]*-1,obj.keypoint[0][1],obj.keypoint[0][2]],
                        'NavalSpine':[obj.keypoint[1][0]*-1,obj.keypoint[1][1],obj.keypoint[1][2]],
                        'ChestSpine':[obj.keypoint[2][0]*-1,obj.keypoint[2][1],obj.keypoint[2][2]],
                        'Neck':[obj.keypoint[3][0]*-1,obj.keypoint[3][1],obj.keypoint[3][2]],

                        'ClavicleLeft':[obj.keypoint[4][0]*-1,obj.keypoint[4][1],obj.keypoint[4][2]],
                        'ShoulderLeft':[obj.keypoint[5][0]*-1,obj.keypoint[5][1],obj.keypoint[5][2]],
                        'ElbowLeft':[obj.keypoint[6][0]*-1,obj.keypoint[6][1],obj.keypoint[6][2]],
                        'WristLeft':[obj.keypoint[7][0]*-1,obj.keypoint[7][1],obj.keypoint[7][2]],
                        'HandLeft':[obj.keypoint[8][0]*-1,obj.keypoint[8][1],obj.keypoint[8][2]],
                        'HandTipLeft':[obj.keypoint[9][0]*-1,obj.keypoint[9][1],obj.keypoint[9][2]],
                        'TumbLeft':[obj.keypoint[10][0]*-1,obj.keypoint[10][1],obj.keypoint[10][2]],

                        'ClavicleRight':[obj.keypoint[11][0]*-1,obj.keypoint[11][1],obj.keypoint[11][2]],
                        'ShoulderRight':[obj.keypoint[12][0]*-1,obj.keypoint[12][1],obj.keypoint[12][2]],
                        'ElbowRight':[obj.keypoint[13][0]*-1,obj.keypoint[13][1],obj.keypoint[13][2]],
                        'WristRight':[obj.keypoint[14][0]*-1,obj.keypoint[14][1],obj.keypoint[14][2]],
                        'HandRight':[obj.keypoint[15][0]*-1,obj.keypoint[15][1],obj.keypoint[15][2]],
                        'HandTipRight':[obj.keypoint[16][0]*-1,obj.keypoint[16][1],obj.keypoint[16][2]],
                        'TumbRight':[obj.keypoint[17][0]*-1,obj.keypoint[17][1],obj.keypoint[17][2]],

                        'HipLeft':[obj.keypoint[18][0]*-1,obj.keypoint[18][1],obj.keypoint[18][2]],
                        'KneeLeft':[obj.keypoint[19][0]*-1,obj.keypoint[19][1],obj.keypoint[19][2]],
                        'AnkleLeft':[obj.keypoint[20][0]*-1,obj.keypoint[20][1],obj.keypoint[20][2]],
                        'FootLeft':[obj.keypoint[21][0]*-1,obj.keypoint[21][1],obj.keypoint[21][2]],

                        'HipRight':[obj.keypoint[22][0]*-1,obj.keypoint[22][1],obj.keypoint[22][2]],
                        'KneeRight':[obj.keypoint[23][0]*-1,obj.keypoint[23][1],obj.keypoint[23][2]],
                        'AnkleRight':[obj.keypoint[24][0]*-1,obj.keypoint[24][1],obj.keypoint[24][2]],
                        'FootRight':[obj.keypoint[25][0]*-1,obj.keypoint[25][1],obj.keypoint[25][2]],

                        'Head':[obj.keypoint[26][0]*-1,obj.keypoint[26][1],obj.keypoint[26][2]],
                        'Nose': [obj.keypoint[27][0]*-1,obj.keypoint[27][1],obj.keypoint[27][2]],
                        'EyeLeft':[obj.keypoint[28][0]*-1,obj.keypoint[28][1],obj.keypoint[28][2]],
                        'EarLeft':[obj.keypoint[29][0]*-1,obj.keypoint[29][1],obj.keypoint[29][2]],
                        'EyeRight':[obj.keypoint[30][0]*-1,obj.keypoint[30][1],obj.keypoint[30][2]],
                        'EarRight':[obj.keypoint[31][0]*-1,obj.keypoint[31][1],obj.keypoint[31][2]],

                        'HeelLeft':[obj.keypoint[32][0]*-1,obj.keypoint[32][1],obj.keypoint[32][2]],
                        'HeelRight':[obj.keypoint[33][0]*-1,obj.keypoint[33][1],obj.keypoint[33][2]],
                    }


                    #ID_body, time_stamp, joints
                    id_body=obj.id
                    date=f"{datetime.datetime.now():%Y-%m-%d}"
                    time_body=f"{datetime.datetime.now():%H:%M:%S.%f}"
                    joints_body=joints
                    # The camera is mirrored, we multiplied by -1 to correct it
                    x=obj.keypoint[1][0]*-1
                    y=obj.keypoint[1][2]

        
    return id_body, date, time_body, joints_body, x, y


