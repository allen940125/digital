using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


public class LidarSensor : MonoBehaviour
{
    public float minRange = 0.2f;
    public float maxRange = 30.0f;
    public int numMeasurementsPerScan = 1800;
    // private float publishInterval;
    public int lineNum = 36;
    public LineRenderer line;
    public LidarToROS lidarToRos;

    float scanAngleStartDegrees = 0;
    float scanAngleEndDegrees = -359;
    // lidar反射強度
    List<float> intensities = new List<float>();
    List<float> intensities_tmp;


    List<float> ranges = new List<float>();
    List<float> range_tmp;

    int m_NumMeasurementsTaken;
    int lineInterval;
    bool isScanning = false;
    private float lastPublishTime = 0.0f; // 记录上次发布的时间


    List<Vector3> directionVectors = new List<Vector3>();
    List<Vector3> directionVectors_tmp;
    private float publishInterval = 0.03f;
    private float timeSinceLastPublish = 0.0f;
    private bool readyToScan = false;

    void Start()
    {
        line.positionCount = lineNum * 2;
        lineInterval = numMeasurementsPerScan / lineNum;
    }

    void BeginScan()
    {
        isScanning = true;
        m_NumMeasurementsTaken = 0;
        ranges.Clear();
        intensities.Clear();
        directionVectors.Clear();

        if (lastPublishTime == 0.0f) // 首次调用
        {
            lastPublishTime = Time.time;
        }
    }
    int i = 0;
    public void EndScan()
    {
        float currentTime = Time.time;
        float interval = currentTime - lastPublishTime;
        lastPublishTime = currentTime; // 更新上次发布时间为当前时间

        // 計算頻
        float frequency = 1 / interval; // 頻率 = 1 / 時間間隔
        // Debug.Log($"Publish frequency: {frequency} Hz");

        // 測試lidar第一個index
        // if (ranges.Count > 0)
        // {
        //     Debug.Log($"First range data: {ranges[0]}");
        // }
        if (ranges.Count == 0)
        {
            Debug.LogWarning($"Took {m_NumMeasurementsTaken} measurements but found no valid ranges");
        }
        else if (ranges.Count != m_NumMeasurementsTaken || ranges.Count != numMeasurementsPerScan)
        {
            Debug.LogWarning($"Expected {numMeasurementsPerScan} measurements. Actually took {m_NumMeasurementsTaken}" +
                             $"and recorded {ranges.Count} ranges.");
        }
        range_tmp = new List<float>(ranges);
        directionVectors_tmp = new List<Vector3>(directionVectors);
        intensities_tmp = new List<float>(intensities);


        // Publish lidar data to ROS
        // i++;
        // if(i % 10 == 0){
        lidarToRos.PublishLidar(range_tmp, intensities_tmp);
        // }


        m_NumMeasurementsTaken = 0;
        ranges.Clear();
        intensities.Clear();
        directionVectors.Clear();
        isScanning = false;
    }

    // Update is called once per frame
    void Update()
    {
        timeSinceLastPublish += Time.deltaTime;
        if (timeSinceLastPublish >= publishInterval)
        {

            PerformLidarScan();
            timeSinceLastPublish = 0.0f;
        }


    }

    void PerformLidarScan(){
        var yawBaseDegrees = transform.rotation.eulerAngles.y;
        //經過0.n秒才進來
        while (m_NumMeasurementsTaken < numMeasurementsPerScan)
        {
            var t = m_NumMeasurementsTaken / (float)numMeasurementsPerScan;
            var yawSensorDegrees = Mathf.Lerp(scanAngleStartDegrees, scanAngleEndDegrees, t);
            // rotate lidar
            var yawDegrees = yawBaseDegrees + yawSensorDegrees;
            // scanning direction
            var directionVector = Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward;
            // ray scan in z axis
            var measurementStart = minRange * directionVector + transform.position;
            // Simulate laser light from the starting point in a specific direction
            Ray measurementRay = new Ray(measurementStart, directionVector);
            RaycastHit hit;

            // Returns whether an object was detected
            var foundValidMeasurement = Physics.Raycast(measurementRay, out hit, maxRange);
            // Only record measurement if it's within the sensor's operating range
            if (foundValidMeasurement)
            {
                ranges.Add(hit.distance);
                float intensity = CalculateIntensity(hit.distance, hit.collider.gameObject);
                intensities.Add(intensity);
                if (m_NumMeasurementsTaken % lineInterval == 0 && (m_NumMeasurementsTaken / lineInterval) < line.positionCount)
                    DrawLine(measurementRay, (m_NumMeasurementsTaken / lineInterval), hit);
            }
            else
            {
                ranges.Add(float.PositiveInfinity);
                intensities.Add(0);
            }

            // Even if Raycast didn't find a valid hit, we still count it as a measurement
            m_NumMeasurementsTaken++;
            directionVectors.Add(directionVector);

        }
        if (m_NumMeasurementsTaken >= numMeasurementsPerScan)
        {
            EndScan();
        }

    }
    float CalculateIntensity(float distance, GameObject hitObject)
    {
        Renderer hitRenderer = hitObject.GetComponent<Renderer>();
        float materialReflectivity = 1.0f; // 默認反射率
        if (hitRenderer != null)
        {
            // 基於顏色亮度去計算
            materialReflectivity = hitRenderer.material.color.maxColorComponent;
        }
        // 假設強度與距離成反比
        float intensity = materialReflectivity / distance;
        return intensity;
    }

    public List<float> GetRange()
    {
        return range_tmp;
    }
    public List<float> GetIntensities()
    {
        return intensities_tmp;
    }

    public List<Vector3> GetRangeDirection()
    {
        return directionVectors_tmp;
    }

    public int GetRangeSize()
    {
        return m_NumMeasurementsTaken;
    }

    public void lidarOn()
    {
        line.positionCount = lineNum * 2;
    }

    public void lidarOff()
    {
        line.positionCount = 0;
    }

    private void DrawLine(Ray ray, int index, RaycastHit hit)
    {
        line.SetPosition(index * 2, ray.origin);
        Vector3 rayEndPoint = hit.point;
        line.SetPosition(index * 2 + 1, rayEndPoint);
    }
}