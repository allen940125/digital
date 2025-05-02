using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System;
using TMPro;
public class CarTransfer : MonoBehaviour
{
    public ConnectRosBridge connectRos;
    public float limit = 2000.0f;
    public float speedRate = 50.0f;
    string carFrontInputTopic = "/car_C_front_wheel";
    string carRearInputTopic = "/car_C_rear_wheel";
    string carOutputTopic = "/wheel_speed";
    private bool isFrontWheelDataReceived = false;
    private bool isRearWheelDataReceived = false;
    private bool receiveFlag = false;
    private float[] frontWheelData = new float[2];
    private float[] rearWheelData = new float[2];
    // Start is called before the first frame update
    void Start()
    {
        receiveFlag = false;
        connectRos.ws.OnMessage += OnWebSocketMessage;
        SubscribeToTopic(carFrontInputTopic);
        SubscribeToTopic(carRearInputTopic);
    }

    // Update is called once per frame
    private void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        string jsonString = e.Data;
        string carWheelType = "";
        var genericMessage = JsonUtility.FromJson<GenericRosMessage>(jsonString);
        if(genericMessage.topic == carFrontInputTopic)
        {
            Debug.Log("Front");
            HandleFloat32MultiArrayMessage(jsonString, "Front");
        }
        else if(genericMessage.topic == carRearInputTopic)
        {
            HandleFloat32MultiArrayMessage(jsonString, "Rear");
        }
    }

    private void HandleFloat32MultiArrayMessage(string jsonString, string carWheelType)
    {
        Float32MultiArrayMessage message = JsonUtility.FromJson<Float32MultiArrayMessage>(jsonString);

        float carLeftSpeed = SpeedLimited(message.msg.data[0] * speedRate);
        float carRightSpeed = SpeedLimited(message.msg.data[1] * speedRate);

        // Debug.Log("carLeftSpeed: " + carLeftSpeed + " carRightSpeed: " + carRightSpeed);

        // 更新前輪或後輪的資料
        if (carWheelType == "Front")
        {
            frontWheelData[0] = carLeftSpeed;
            frontWheelData[1] = carRightSpeed;
            isFrontWheelDataReceived = true;
        }
        else if (carWheelType == "Rear")
        {
            rearWheelData[0] = carLeftSpeed;
            rearWheelData[1] = carRightSpeed;
            isRearWheelDataReceived = true;
        }

        // 當前後輪資料都接收到時，處理並發布資料
        if (isFrontWheelDataReceived && isRearWheelDataReceived)
        {
            ProcessAndPublishWheelData();
            receiveFlag = true;
            isFrontWheelDataReceived = false;
            isRearWheelDataReceived = false;
        }

    }
    private float MapSpeed(float speed, float maxInput, float minOutput, float maxOutput) {
        float inputRange = maxInput; // 正负速度均映射到0到maxInput
        float outputRange = maxOutput - minOutput;
        return (Math.Abs(speed) / inputRange) * outputRange * Math.Sign(speed) + (Math.Sign(speed) * minOutput);
    }
    private float SpeedLimited(float value) {
        if (Math.Abs(value) > limit) {
            value = limit * Math.Sign(value);
        }
        return value;
    }

    public bool IsWheelDataReceived()
    {
        return receiveFlag;
    }

    public void ChangeDataReceivedStatus()
    {
        receiveFlag = false;
    }

    private void ProcessAndPublishWheelData()
    {
        float[] finalWheelData = new float[4];
        finalWheelData[0] = frontWheelData[0];
        finalWheelData[1] = frontWheelData[1];
        finalWheelData[2] = rearWheelData[0];
        finalWheelData[3] = rearWheelData[1];
        PublishFloat32MultiArray(carOutputTopic, finalWheelData);
    }

    private void SubscribeToTopic(string topic)
    {

        string typeMsg = "std_msgs/msg/Float32MultiArray";
        string subscribeMessage = "{\"op\":\"subscribe\",\"id\":\"1\",\"topic\":\"" + topic + "\",\"type\":\""+typeMsg+"\"}";
        connectRos.ws.Send(subscribeMessage);
    }

    public void PublishFloat32MultiArray(string topic, float[] data)
    {
        string jsonMessage = $@"{{
            ""op"": ""publish"",
            ""topic"": ""{topic}"",
            ""msg"": {{
                ""layout"": {{
                    ""dim"": [{{""size"": {data.Length}, ""stride"": 1}}],
                    ""data_offset"": 0
                }},
                ""data"": [{string.Join(", ", data)}]
            }}
        }}";

        connectRos.ws.Send(jsonMessage);
    }

    [System.Serializable]
    public class GenericRosMessage
    {
        public string op;
        public string topic;
    }
    [System.Serializable]
    public class RobotNewsMessageString
    {
        public string op;
        public string topic;
        public StringMessage msg;
    }
    [System.Serializable]
    public class StringMessage
    {
        public string data;
    }
    [System.Serializable]
    public class TargetVelocityData
    {
        public Data data;
    }

    [System.Serializable]
    public class Data
    {
        public float[] target_vel;
    }

    [System.Serializable]
    public class Float32MultiArrayMessage
    {
        public string op;
        public string topic;
        public Float32MultiArray msg;
    }

    [System.Serializable]
    public class Float32MultiArray
    {
        public Layout layout;
        public float[] data;
    }

    [System.Serializable]
    public class Layout
    {
        public Dim[] dim;
        public int data_offset;
    }

    [System.Serializable]
    public class Dim
    {
        public int size;
        public int stride;
    }


}
