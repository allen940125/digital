import time
import math
import keyboard
from ws_client import RosbridgeClient

# 初始角度：共10個關節，先控制前6個（手臂）
joint_angles = [0] * 10
step = 5  # 每次按鍵改變的角度
wait_time = 0.05

# 關節控制對應：按鍵 -> (關節index, 角度改變值)
key_bindings = {
    '1': (0, +step), 'q': (0, -step),
    '2': (1, +step), 'w': (1, -step),
    '3': (2, +step), 'e': (2, -step),
    '4': (3, +step), 'r': (3, -step),
    '5': (4, +step), 't': (4, -step),
    '6': (5, +step), 'y': (5, -step),
}

preset_pose = [90, 10, 160, 90, 90, 90]

# 每軸的邏輯角度限制 (degree)
joint_limits = [
    (0, 180),    # link_1_1
    (0, 170),    # link_2_1
    (90, 180),   # link_3_1
    (0, 180),    # Wrist1_1
    (0, 180),    # Wrist2_1
    (0, 180),    # link4_1
    (-180, 180), # grap2_2_1 (未用)
    (-180, 180), # grap1_2_1 (未用)
    (-180, 180), # grap2_1   (未用)
    (-180, 180), # grap1_1   (未用)
]

# 每軸對應的角度偏移
angle_offsets = [
    0,    # link_1_1
    -65,  # link_2_1
    -70,    # link_3_1
    -90,    # Wrist1_1
    -90,    # Wrist2_1
    -90,    # link4_1
    0,    # grap2_2_1
    0,    # grap1_2_1
    0,    # grap2_1
    0,    # grap1_1
]

def apply_offset(logic_angle, idx):
    return logic_angle + angle_offsets[idx]

def connect_rosbridge(ros_client):
    ros_ip = input("Enter ROSBridge WebSocket IP: ").strip()
    if ros_client.connect(ros_ip):
        ros_client.advertise_topic("/robot_arm", "trajectory_msgs/JointTrajectoryPoint")
    else:
        print("[ERROR] Failed to connect to ROSBridge")
        connect_rosbridge(ros_client)

def publishArm(ros_client, joint_positions_degree):
    real_angles = [apply_offset(angle, idx) for idx, angle in enumerate(joint_positions_degree)]
    joint_positions_radian = [math.radians(degree) for degree in real_angles]
    ros_client.publish("/robot_arm", {"positions": joint_positions_radian})
    print(f"[INFO] Sent (with offset): {real_angles[:6]}")

def reset_angles(ros_client):
    for i in range(6):
        joint_angles[i] = preset_pose[i]
    publishArm(ros_client, joint_angles)
    print("[INFO] Angles reset.")

def main():
    ros_client = RosbridgeClient(rosbridge_port=9090)
    connect_rosbridge(ros_client)

    reset_angles(ros_client)
    print("Controls:")
    print("  1/q = link_1_1  | 2/w = link_2_1  | 3/e = link_3_1")
    print("  4/r = Wrist1_1  | 5/t = Wrist2_1  | 6/y = link4_1")
    print("  z   = Reset to default angles")

    while True:
        for key, (idx, delta) in key_bindings.items():
            if keyboard.is_pressed(key):
                joint_angles[idx] += delta
                min_angle, max_angle = joint_limits[idx]
                joint_angles[idx] = max(min_angle, min(max_angle, joint_angles[idx]))
                publishArm(ros_client, joint_angles)
                time.sleep(wait_time)

        if keyboard.is_pressed("z"):
            reset_angles(ros_client)
            time.sleep(0.3)  # 防止重複觸發

        time.sleep(0.01)

if __name__ == "__main__":
    main()
