using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class LidarToROS : MonoBehaviour
{
    public ConnectRosBridge connectRos;
    string topicName = "/scan_tmp";
    bool isTopicAdvertised = false;
    // public SimulateClockToROS clock;

    void Start()
    {
        AdvertiseTopic();
    }

    void AdvertiseTopic()
    {
        // Prepare the advertise message JSON
        string advertiseMessage = $@"{{
            ""op"": ""advertise"",
            ""topic"": ""{topicName}"",
            ""type"": ""sensor_msgs/LaserScan""
        }}";

        // Send the advertise message
        connectRos.ws.Send(advertiseMessage);
        isTopicAdvertised = true;
    }

    public void PublishLidar(List<float> rangeData, List<float> intensitiesData)
    {
        // Ensure the topic is advertised before publishing
        if (!isTopicAdvertised)
        {
            AdvertiseTopic();
        }

        // Use Unity's built-in time for the timestamp instead of RosTime.
        float t = Time.realtimeSinceStartup;
        long secs = (long)t;
        long nsecs = (long)((t - secs) * 1e9);

        float angleMin = -Mathf.PI;
        float angleMax = Mathf.PI;
        float angleIncrement = 2 * Mathf.PI / rangeData.Count;
        float timeIncrement = 0.00005f;
        float scanTime = 0.1f;
        float rangeMin = 0.15f;
        float rangeMax = 16.0f;

        // Filter NaN, Infinity, and clamp the values between rangeMin and rangeMax
        List<float> filteredRanges = rangeData
            .Select(v => Mathf.Clamp(v, rangeMin, rangeMax))
            .ToList();

        string ranges = string.Join(", ", rangeData);
        string intensityValues = string.Join(", ", intensitiesData);

        string jsonMessage = $@"{{
            ""op"": ""publish"",
            ""topic"": ""{topicName}"",
            ""msg"": {{
                ""header"": {{
                    ""stamp"": {{
                        ""secs"": {secs},
                        ""nsecs"": {nsecs}
                    }},
                    ""frame_id"": ""laser""
                }},
                ""angle_min"": {angleMin},
                ""angle_max"": {angleMax},
                ""angle_increment"": {angleIncrement},
                ""time_increment"": {timeIncrement},
                ""scan_time"": {scanTime},
                ""range_min"": {rangeMin},
                ""range_max"": {rangeMax},
                ""ranges"": [{ranges}],
                ""intensities"": [{intensityValues}]
            }}
        }}";

        connectRos.ws.Send(jsonMessage);
    }
}
