using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace MemoryGame;

public class Runner : FrameLoop
{
    private delegate void GetMarker(MarkerData data);
    private delegate void AddMarker(IEnumerable<KeyValuePair<int, MarkerData>> data);
    private delegate void RemoveMarker(IEnumerable<KeyValuePair<int, MarkerData>> data);

    private GetMarker e_OnGetMarker;
    private AddMarker e_OnAddMarker;
    private RemoveMarker e_OnRemoveMarker;

    private VideoCapture m_Capture;

    private Matrix<float> m_IntrinsicMatrix;
    private Matrix<float> m_DistCoeffsMatrix;
    
    private static int s_FrameWidth = 800;
    private static int s_FrameHeight = 600;
    static int s_ImgIndex = 0;
    
    public Runner()
    {
        m_Capture = new(0);

        m_Capture.Set(CapProp.FrameWidth, s_FrameWidth);
        m_Capture.Set(CapProp.FrameHeight, s_FrameHeight);

        e_OnAddMarker = OnAddMarker;
        e_OnGetMarker = OnGetMarker;
        e_OnRemoveMarker = OnRemoveMarker;
        
        // This calculates our intrinsics based on provided images of the calibration objects
        Utilities.ReadIntrinsicsFromFile(out m_IntrinsicMatrix, out m_DistCoeffsMatrix);
    }

    public override void OnFrame()
    {
        Globals.g_DetectedMarkers.Clear();

        p_BaseFrame = new();

        if (!m_Capture.Read(p_BaseFrame))
        {
            Console.WriteLine("Failed to grab frame");
            return;
        }

        GenerateFrames();

        CvInvoke.Imshow("Preview", p_BaseFrame);
        CvInvoke.Imshow("Binary", p_BinaryFrame);


        FindCornersPoints(out var contours);
        GetMarkers(contours);
        DetectMarkerChanges();

        // Uncomment these to capture and calibrate!
        // CaptureMarkers(contours);
        // CalibrateCamera();

        // Create a delay. This is used to keep the capturing alive.
        CvInvoke.WaitKey(1);
    }
    private void FindCornersPoints(out VectorOfVectorOfPoint validContours)
    {
        var contours = new VectorOfVectorOfPoint();
        CvInvoke.FindContours(p_BinaryFrame, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

        p_ContourFrame = new();
        p_BaseFrame.CopyTo(p_ContourFrame);

        validContours = FilterContours(contours);

        // CvInvoke.DrawContours(p_ContourFrame, validContours, -1, new MCvScalar(0, 255, 0), 2);
        CvInvoke.Imshow("Contours", p_ContourFrame);
    }
    private void GetMarkers(VectorOfVectorOfPoint validContours)
    {
        var undistortedMarkers = CalculateMarkerUndistortion(p_BaseFrame, validContours);

        for (var i = 0; i < undistortedMarkers.Size; i++)
        {
            var centerValues = GetMarkerValues(undistortedMarkers[i]);
            var marketDetected = centerValues.GetMarkerData(out var data);

            if (!marketDetected)
            {
                continue;
            }

            // Find the projection matrix for the current contour
            if (CalculatePerspective(data.RotationIndex, validContours[i], out var worldToScreenMatrix))
            {
                switch (data.Type)
                {
                    case MARKERTYPE.Type_02:
                        // Draw cube onto the contour frame using the projection matrix
                        Utilities.DrawCube(p_ContourFrame, worldToScreenMatrix);
                        break;
                    case MARKERTYPE.Type_01:
                        // Draw cube onto the contour frame using the projection matrix
                        Utilities.DrawCircle(p_ContourFrame, worldToScreenMatrix, 1);
                        break;
                }
            }

            e_OnGetMarker?.Invoke(data);
            CvInvoke.Imshow("Contours", p_ContourFrame);
        }
    }
    private void DetectMarkerChanges()
    {
        // Find markers in tmp that are not present in Globals.g_DetectedMarkers
        var markersToAdd = Globals.g_DetectedMarkers.Except(Globals.g_AddedDataMarkers);

        if (markersToAdd.Any())
        {
            e_OnAddMarker?.Invoke(markersToAdd);
        }

        // Find markers in Globals.g_DetectedMarkers that are not present in tmp
        var markersToRemove = Globals.g_AddedDataMarkers.Except(Globals.g_DetectedMarkers);

        if (markersToRemove.Any())
        {
            e_OnRemoveMarker?.Invoke(markersToRemove);
        }
    }

    #region Calculation Methods

    private static VectorOfVectorOfPoint FilterContours(VectorOfVectorOfPoint contours)
    {
        var validContours = new VectorOfVectorOfPoint();

        for (var i = 0; i < contours.Size; i++)
        {
            VectorOfPoint contour = contours[i];

            // Reduce number of points / corners

            VectorOfPoint approxPoly = new VectorOfPoint();
            CvInvoke.ApproxPolyDP(contour, approxPoly, 4, true);

            // Markers are squares. Therefor they must have exactly four points / corners
            if (approxPoly.Size != 4)
            {
                continue;
            }

            var lengthLimit = CvInvoke.ArcLength(approxPoly, true);
            var areaLimit = CvInvoke.ContourArea(approxPoly, true);

            // Valid contours must also be within the specified size and correct orientation
            var validSize = lengthLimit is > 100 and < 700;
            var validArea = areaLimit > 0;

            if (validSize && validArea)
            {
                validContours.Push(approxPoly);
            }
        }

        return validContours;
    }

    private VectorOfMat CalculateMarkerUndistortion(IInputArray image, VectorOfVectorOfPoint validContours)
    {
        VectorOfMat undistortedMarkers = new VectorOfMat();

        for (var i = 0; i < validContours.Size; i++)
        {
            VectorOfPoint contour = validContours[i];
            Mat homography = CvInvoke.FindHomography(contour, Globals.g_ScreenRelativePoints,
                RobustEstimationAlgorithm.Ransac);

            Mat markerContent = new Mat();

            CvInvoke.WarpPerspective
            (
                image,
                markerContent,
                homography,
                new(Globals.g_Size, Globals.g_Size)
            );

            undistortedMarkers.Push(markerContent);
        }

        return undistortedMarkers;
    }

    private static byte[,] GetMarkerValues(IInputArray undistoredMarker)
    {
        var grayMarker = new Mat();
        CvInvoke.CvtColor(undistoredMarker, grayMarker, ColorConversion.Bgr2Gray);

        var binaryMarker = new Mat();
        CvInvoke.Threshold(grayMarker, binaryMarker, 175, 255, ThresholdType.Otsu);

        var gridSize = Globals.g_Size / Globals.g_Count;
        var halfGridSize = gridSize / 2;
        
        var centerValues = new byte[Globals.g_Count, Globals.g_Count];
        
        for (var y = 0; y < Globals.g_Count; y++)
        {
            for (var x = 0; x < Globals.g_Count; x++)
            {
                var centerValue = binaryMarker.GetRawData
                (
                    x * gridSize + halfGridSize,
                    y * gridSize + halfGridSize
                );

                centerValues[x, y] = centerValue[0];
            }
        }

        return centerValues;
    }

    private bool CalculatePerspective(int rotationIndex, VectorOfPoint contour, out Matrix<float> perspectiveMatrix)
    {
        perspectiveMatrix = null;

        var worldSpacePoints = Globals.g_WorldRelativePoints[rotationIndex];
        var cornerPoints = contour.ToArray()
            .Select(point => new PointF(point.X, point.Y))
            .ToArray();

        var rotationVector = new Matrix<float>(3, 1);
        var translationVector = new Matrix<float>(3, 1);

        var pnpSolved = CvInvoke.SolvePnP(worldSpacePoints, cornerPoints, m_IntrinsicMatrix, m_DistCoeffsMatrix, rotationVector,
            translationVector);

        if (!pnpSolved)
        {
            return false;
        }

        var rotationMatrix = new Matrix<float>(3, 3);
        CvInvoke.Rodrigues(rotationVector, rotationMatrix);

        var rValues = rotationMatrix.Data;
        var tValues = translationVector.Data;

        var extrinsicMatrix = new Matrix<float>(new[,]
        {
            { rValues[0, 0], rValues[0, 1], rValues[0, 2], tValues[0, 0] },
            { rValues[1, 0], rValues[1, 1], rValues[1, 2], tValues[1, 0] },
            { rValues[2, 0], rValues[2, 1], rValues[2, 2], tValues[2, 0] }
        });

        // Multiply matrix by our intrinsics in order to make up for the camera characteristics  
        perspectiveMatrix = m_IntrinsicMatrix * extrinsicMatrix;
        return true;
    }

    #endregion

    #region Delegate methods

    private void OnGetMarker(MarkerData data)
    {
        Globals.g_DetectedMarkers.TryAdd(data.Id, data);
    }

    private void OnAddMarker(IEnumerable<KeyValuePair<int, MarkerData>> data)
    {
        foreach (var kvp in data)
        {
            if (!Globals.g_AddedDataMarkers.TryAdd(kvp.Value.Id, kvp.Value))
            {
                continue;
            }

            Console.WriteLine($"Marker was added with data >> {kvp.ToString()}");
        }
    }

    private void OnRemoveMarker(IEnumerable<KeyValuePair<int, MarkerData>> data)
    {
        // Make sure that the marker is not still detected before removing 
        foreach (var kvp in data)
        {
            // Check if the marker exists in the detected markers dictionary
            if (!Globals.g_DetectedMarkers.ContainsKey(kvp.Key))
            {
                // If the marker is not detected, remove it from the added markers
                Console.WriteLine(Globals.g_AddedDataMarkers.Remove(kvp.Key, out _)
                    ? $"Marker was removed with ID: {kvp.Key}"
                    : $"Failed to remove marker with ID: {kvp.Key}");
            }
        }
    }

    #endregion

    #region Capture & Calibration Methods

    private void CaptureMarkers(VectorOfVectorOfPoint validContours)
    {
        var contours = new VectorOfVectorOfPoint();
        CvInvoke.FindContours(p_BinaryFrame, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

        p_ContourFrame = new();
        p_BaseFrame.CopyTo(p_ContourFrame);

        validContours = FilterContours(contours);
        CvInvoke.DrawContours(p_ContourFrame, validContours, -1, new MCvScalar(0, 255, 0), 2);

        Mat pictureFrame = new Mat();
        p_BaseFrame.CopyTo(pictureFrame);

        CvInvoke.SetWindowTitle("Picture Frame", $"Preview for frame {s_ImgIndex + 1}");
        CvInvoke.Imshow("Picture Frame", pictureFrame);

        if (CvInvoke.PollKey() != -1)
            CvInvoke.Imwrite($"capture_{s_ImgIndex++}.jpg", pictureFrame);
    }

    public void CalibrateCamera(bool showPreview = true)
    {
        var images = Directory.GetFiles(Directory.GetCurrentDirectory(), "capture_*.jpg");

        var listOfObjectPoints = new List<MCvPoint3D32f[]>();
        var listOfCornerPoints = new List<VectorOfPointF>();

        if (images.Length == 0)
        {
            Console.WriteLine("No calibration images found");
            return;
        }

        foreach (string image in images)
        {
            Mat frame = CvInvoke.Imread(image);

            Mat grayFrame = new Mat();
            CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);

            Mat binaryFrame = new Mat();
            CvInvoke.Threshold(grayFrame, binaryFrame, 120, 255, ThresholdType.Otsu);

            var cornerPoints = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(binaryFrame, cornerPoints, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

            var validContours = FilterContours(cornerPoints);

            for (var i = 0; i < validContours.Size; i++)
            {
                VectorOfPoint vectorOfPoint = validContours[i];
                PointF[] points = Array.ConvertAll(vectorOfPoint.ToArray(), point => new PointF(point.X, point.Y));
                VectorOfPointF vectorOfPointF = new VectorOfPointF(points);
                listOfCornerPoints.Add(vectorOfPointF);

                // Draw corner points
                foreach (PointF point in points)
                {
                    CvInvoke.Circle(frame, Point.Round(point), 3, new MCvScalar(255, 0, 0), -1);
                }
            }

            var undistortedMarkers = CalculateMarkerUndistortion(frame, validContours);

            for (var i = 0; i < undistortedMarkers.Size; i++)
            {
                var centerValues = GetMarkerValues(undistortedMarkers[i]);
                var foundMarker = centerValues.GetMarkerData(out var data);

                if (!foundMarker)
                {
                    continue;
                }

                listOfObjectPoints.Add(Globals.g_WorldRelativePoints[data.RotationIndex]);
            }

            // CvInvoke.DrawContours(frame, validContours, -1, new MCvScalar(0, 255, 0), 2);

            while (CvInvoke.WaitKey(1) == -1)
                CvInvoke.Imshow("Calibration", frame);
        }

        var intrinsics = new Mat();
        var distCoeffs = new Mat();

        var frameSize = new Size(s_FrameWidth, s_FrameHeight);

        double reprojectionError = CvInvoke.CalibrateCamera
        (
            listOfObjectPoints.ToArray(),
            listOfCornerPoints.Select(x => x.ToArray()).ToArray(),
            frameSize,
            intrinsics,
            distCoeffs,
            CalibType.Default,
            new(30, 0.1),
            out Mat[] rVecs, out Mat[] tVecs
        );

        using FileStorage fs = new FileStorage("intrinsics.json", FileStorage.Mode.Write);
        fs.Write(intrinsics, "Intrinsics");
        fs.Write(distCoeffs, "DistCoeffs");
    }

    #endregion
}