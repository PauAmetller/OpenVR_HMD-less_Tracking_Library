using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using TMPro;
using UnityEngine;
using Newtonsoft.Json;

public static class CalibrationUtils
{
    // ==== Start: Update Positions Block ====  ///Still needs to be corrected after the changes///

    /// <summary>
    /// Calculates the calibrated position
    /// </summary>
    //Return the given raw position as a calibrated position
    public static Vector3 CalibrateRawPos(Vector3 rawPos, bool enableYAxis, Calibration calibration, Vector3 virtualWorldSpace)
    {
        //Apply the calibration transform (rotation)
        Vector3 calibratedPos = ApplyCalibrationTransform(rawPos, calibration);

        // Apply scaling to the position
        calibratedPos = ApplyScale(calibratedPos, calibration.GetCalibrationRealWorldSize(), virtualWorldSpace);

        // Apply the Y offset and decide if Y should be enabled or set to zero
        calibratedPos.y = enableYAxis ? (rawPos.y - calibration.GetCalibrationCenter().y) : 0;

        return calibratedPos;
    }

    private static Vector2 ApplyCalibrationTransform(Vector3 rawPos, Calibration calibration)
    {
        return calibration.GetCalibrationRotation() * (rawPos - calibration.GetCalibrationCenter());
    }

    private static Vector3 ApplyScale(Vector3 calibratedPos, Vector3 realWorldScale, Vector3 virtualWorldSpace)
    {
        Vector3 calibrationScale = new Vector3(
            virtualWorldSpace.x / realWorldScale.x,
            virtualWorldSpace.y / realWorldScale.y,
            virtualWorldSpace.z / realWorldScale.z
        );

        return new Vector3(
            calibratedPos.x * calibrationScale.x,
            calibratedPos.y * calibrationScale.y,
            calibratedPos.z * calibrationScale.z
        );
    }

    // ==== Ends: Update Positions Block ====

    // ==== Start: Update Rotation Block ====

    public static Quaternion CalibratedRawRot(Quaternion playerRotation, Calibration calibration)
    {
        return playerRotation * calibration.GetCalibrationRotation();
    }

    // ==== Ends: Update Rotation Block ====

    // ==== Starts: Fill Calibration Data ====

    /// <summary>
    /// Calculates each attribute of the Calibration class, assigns it, and returns the filled class
    /// </summary>
    public static Calibration CalculateCalibrationData(Vector3[] calibrationPoints, Vector3 virtualWorldSpace)
    {
        Calibration calibrationData = new Calibration();

        //Calculate the point (0, 0, 0) of the calibrated world
        calibrationData.SetCalibrationCenter(CalculateCalibrationCenter(calibrationPoints));

        //Calculate the Scale of the calibrated world
        calibrationData.SetCalibrationRealWorldSize(CalculateRealWorldSize(calibrationPoints, virtualWorldSpace));

        //Calculate the CalibrationTransform
        calibrationData.SetCalibrationRotation(CalculateRotationMatrix(calibrationPoints));

        //Calculate rotation matrix

        return calibrationData;
    }

    private static Vector3 CalculateCalibrationCenter(Vector3[] points)
    {
        return CalibrationPointsUtils.ComputeCentroid(points);
    }

    private static Vector3 CalculateRealWorldSize(Vector3[] points, Vector3 virtualWorldSpace)
    {
        // Calculate lengths of opposing sides
        float vertical1 = (points[0] - points[1]).magnitude;
        float vertical2 = (points[3] - points[2]).magnitude;
        float horizontal1 = (points[1] - points[2]).magnitude;
        float horizontal2 = (points[0] - points[3]).magnitude;

        // Average lengths of opposing sides
        float horizontal = (horizontal1 + horizontal2) / 2f;
        float vertical = (vertical1 + vertical2) / 2f;

        // Verify consistency of edges (for debugging)
        float maxDifference = Mathf.Max(Mathf.Abs(horizontal1 - horizontal2), Mathf.Abs(vertical1 - vertical2));
        if (maxDifference > 0.1f) // Tolerance for square consistency
            Debug.LogWarning("Square edges are inconsistent. Check calibration points.");

        Vector3 centroid = CalibrationPointsUtils.ComputeCentroid(points.Take(4).ToArray());
        float up = (centroid - points[4]).magnitude;

        // Calculate average scale
        Vector3 realWorldScale = new Vector3(horizontal, up, vertical) ;

        return realWorldScale * (10f/9f); //Rebuild scale from 90% to 100% of the screen size
    }

    private static Quaternion CalculateRotationMatrix(Vector3[] points)
    {
        // Approximate initial axes
        Vector3 initialYAxis = CalibrationPointsUtils.GetNormalOfPlaneFormedBySquare(points);
        Vector3 horizontal1 = points[1] - points[0];
        Vector3 horizontal2 = points[2] - points[3];
        Vector3 initialXAxis = (horizontal1 + horizontal2).normalized;
        Vector3 vertical1 = points[2] - points[1];
        Vector3 vertical2 = points[3] - points[0];
        Vector3 initialZAxis = (vertical1 + vertical2).normalized;

        // Gram-Schmidt with averaging to minimize deviation
        Vector3 x = initialXAxis;
        Vector3 y = initialYAxis;
        Vector3 z = initialZAxis;

        // Orthogonalize axes iteratively
        for (int i = 0; i < 3; i++) 
        {
            // Adjust each axis to be orthogonal to the others
            x = (x - Vector3.Dot(x, y) * y - Vector3.Dot(x, z) * z).normalized;
            y = (y - Vector3.Dot(y, x) * x - Vector3.Dot(y, z) * z).normalized;
            z = (z - Vector3.Dot(z, x) * x - Vector3.Dot(z, y) * y).normalized;
        }

        // Construct the rotation matrix
        Matrix4x4 rotationMatrix = new Matrix4x4();

        rotationMatrix.SetColumn(0, new Vector4(x.x, x.y, x.z, 0)); // X-axis
        rotationMatrix.SetColumn(1, new Vector4(y.x, y.y, y.z, 0)); // Y-axis
        rotationMatrix.SetColumn(2, new Vector4(z.x, z.y, z.z, 0)); // Z-axis

        return rotationMatrix.rotation;
    }

    // ==== Ends: Fill Calibration Data ====

    // ==== Starts: Load File Block ====

    /// <summary>
    /// Load the calibration data saved in the file and return the calibrationData.
    /// </summary>
    public static Calibration LoadCalibrationJson(string fullCalibrationSaveFilePath)
    {
        string jsonString = File.ReadAllText(fullCalibrationSaveFilePath);
        Calibration calibrationData = JsonConvert.DeserializeObject<Calibration>(jsonString);
        return calibrationData;
    }

    // ==== Ends: Load File Block ====

    public static void SaveCalibrationJson(Calibration calibrationData, string fullCalibrationSaveFilePath)
    {

    }
}