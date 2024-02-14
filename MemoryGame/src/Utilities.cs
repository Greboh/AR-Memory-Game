using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace MemoryGame;

public static class Utilities
{
    /// <summary>
    /// Reads and returns the intrinsics matrices from the file intrinsics.json.<br/>
    /// See <see cref="CalibrateCamera"/>.
    /// </summary>
    /// <param name="intrinsics">The resulting intrinsics read from the file</param>
    /// <param name="distCoeffs">The resulting distortion coefficients read from the file</param>
    public static void ReadIntrinsicsFromFile(out Matrix<float> intrinsics, out Matrix<float> distCoeffs)
    {
        Mat intrinsicsMat = new Mat();
        Mat distCoeffsMat = new Mat();

        using FileStorage fs = new FileStorage("intrinsics.json", FileStorage.Mode.Read);

        FileNode intrinsicsNode = fs.GetNode("Intrinsics");
        FileNode distCoeffsNode = fs.GetNode("DistCoeffs");

        intrinsicsNode.ReadMat(intrinsicsMat);
        distCoeffsNode.ReadMat(distCoeffsMat);

        intrinsics = new(3, 3);
        distCoeffs = new(1, 5);

        intrinsicsMat.ConvertTo(intrinsics, DepthType.Cv32F);
        distCoeffsMat.ConvertTo(distCoeffs, DepthType.Cv32F);
    }
    
    public static void DrawCube(IInputOutputArray img, Matrix<float> projection, float scale = 1)
    {
        var color = Globals.g_AddedDataMarkers.Count(x => x.Value.Type == MARKERTYPE.Type_02) >= 2
                    && Globals.g_AddedDataMarkers.Count <= 2
            ? new MCvScalar(0, 255, 0)
            : new(0, 0, 255);

        Matrix<float>[] worldPoints = 
        {
            new(new float[] { 0, 0, 0, 1 }), new(new[] { scale, 0, 0, 1 }),
            new(new[] { scale, scale, 0, 1 }), new(new[] { 0, scale, 0, 1 }),
            new(new[] { 0, 0, -scale, 1 }), new(new[] { scale, 0, -scale, 1 }),
            new(new[] { scale, scale, -scale, 1 }),
            new(new[] { 0, scale, -scale, 1 })
        };

        var screenPoints = worldPoints
            .Select(x => WorldToScreen(x, projection)).ToArray();

        _ = new[]
        {
            Tuple.Create(0, 1), Tuple.Create(1, 2), // Floor
            Tuple.Create(2, 3), Tuple.Create(3, 0),
            Tuple.Create(4, 5), Tuple.Create(5, 6), // Top
            Tuple.Create(6, 7), Tuple.Create(7, 4),
            Tuple.Create(0, 4), Tuple.Create(1, 5), // Pillars
            Tuple.Create(2, 6), Tuple.Create(3, 7)
        };

        // Draw top
        Point[] topSquare = { screenPoints[4], screenPoints[5], screenPoints[6], screenPoints[7] };
        CvInvoke.FillPoly(img, new VectorOfPoint(topSquare), color);

        // Draw sides
        for (var i = 0; i < 4; i++)
        {
            Point[] sideSquare = { screenPoints[i], screenPoints[(i + 1) % 4], screenPoints[((i + 1) % 4) + 4], screenPoints[i + 4] };
            CvInvoke.FillPoly(img, new VectorOfPoint(sideSquare), color);
        }
    }
    
    public static void DrawCircle(IInputOutputArray img, Matrix<float> projection, float scale = 1)
    {
        var color = Globals.g_AddedDataMarkers.Count(x => x.Value.Type == MARKERTYPE.Type_01) >= 2
                    && Globals.g_AddedDataMarkers.Count <= 2
            ? new MCvScalar(0, 255, 0)
            : new(0, 0, 255);

        Matrix<float>[] worldPoints = {
            new(new float[] { 0, 0, 0, 1 }), new(new[] { scale, 0, 0, 1 }),
            new(new[] { scale, scale, 0, 1 }), new(new[] { 0, scale, 0, 1 }),
            new(new[] { 0, 0, -scale, 1 }), new(new[] { scale, 0, -scale, 1 }),
            new(new[] { scale, scale, -scale, 1 }),
            new(new[] { 0, scale, -scale, 1 })
        };

        var screenPoints = worldPoints
            .Select(x => WorldToScreen(x, projection)).ToArray();

        // Calculate the center point of the square based on a cube
        var centerX = (screenPoints[0].X + screenPoints[6].X) / 2;
        var centerY = (screenPoints[0].Y + screenPoints[6].Y) / 2;
        var center = new Point(centerX, centerY);

        // Enables scaling without affecting the world points
        var scalingFactor = 0.5; 
        
        // Calculate the radius of the circle using the Euclidean distance formula:
        // Used ChatGPT for this formula .. It finds the distance between two opposite corners of the square in screen space
        // And then it can just be divided by 2 to get a radius for the square
        var radius = Math.Sqrt(Math.Pow(screenPoints[0].X - screenPoints[6].X, 2) + Math.Pow(screenPoints[0].Y - screenPoints[6].Y, 2)) / 2 * scalingFactor;

        // Draw the circle
        CvInvoke.Circle(img, center, (int)radius, color, -1); // -1 means filled circle
    }
    
    /// <summary>
    /// Converts a homogeneous world coordinate to a screen point
    /// </summary>
    /// <param name="worldPoint">The homogeneous world coordinate</param>
    /// <param name="projection">The projection-matrix to use for converting world coordinates to screen coordinates</param>
    /// <returns>The Point in screen coordinates</returns>
    public static Point WorldToScreen(Matrix<float> worldPoint, Matrix<float> projection)
    {
        Matrix<float> result = projection * worldPoint;
        return new((int)(result[0, 0] / result[2, 0]), (int)(result[1, 0] / result[2, 0]));
    }
}