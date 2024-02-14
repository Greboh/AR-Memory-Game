using System.Drawing;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace MemoryGame;

public static class Globals
{
    public const int g_Size = 300;
    public const int g_Count = 6;
    
    public static Dictionary<int, MarkerData> g_DetectedMarkers = new();
    public static Dictionary<int, MarkerData> g_AddedDataMarkers = new();


    public static readonly VectorOfPoint g_ScreenRelativePoints = new(new[]
    {
        new Point(0, 0),
        new Point(g_Size, 0),
        new Point(g_Size, g_Size),
        new Point(0, g_Size)
    });

    public static readonly MCvPoint3D32f[][] g_WorldRelativePoints =
    {
        new MCvPoint3D32f[]
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(1, 1, 0),
            new(0, 1, 0)
        },
        new MCvPoint3D32f[]
        {
            new(1, 0, 0),
            new(1, 1, 0),
            new(0, 1, 0),
            new(0, 0, 0)
        },
        new MCvPoint3D32f[]
        {
            new(1, 1, 0),
            new(0, 1, 0),
            new(0, 0, 0),
            new(1, 0, 0)
        },
        new MCvPoint3D32f[]
        {
            new(0, 1, 0),
            new(0, 0, 0),
            new(1, 0, 0),
            new(1, 1, 0)
        }
    };

    public static Marker[] g_Markers =
    {
        new(0, MARKERTYPE.Type_01, new byte[,]
        {
            { 0, 0, 0, 0, 0, 0 },
            { 0, 255, 0, 255, 255, 0 },
            { 0, 0, 255, 0, 0, 0 },
            { 0, 255, 255, 0, 0, 0 },
            { 0, 255, 255, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0 },
        }),
        new(1, MARKERTYPE.Type_01, new byte[,]
        {
            { 0, 0, 0, 0, 0, 0 },
            { 0, 255, 0, 255, 255, 0 },
            { 0, 0, 255, 0, 255, 0 },
            { 0, 0, 0, 255, 255, 0 },
            { 0, 0, 0, 255, 0, 0 },
            { 0, 0, 0, 0, 0, 0 },
        }),
        new(2,MARKERTYPE.Type_02, new byte[,]
        {
            { 0, 0, 0, 0, 0, 0 },
            { 0, 0, 255, 255, 0, 0 },
            { 0, 0, 255, 0, 255, 0 },
            { 0, 0, 255, 0, 0, 0 },
            { 0, 0, 255, 255, 255, 0 },
            { 0, 0, 0, 0, 0, 0 },
        }),
        new(3,MARKERTYPE.Type_02, new byte[,]
        {
            { 0, 0, 0, 0, 0, 0 },
            { 0, 0, 255, 255, 255, 0 },
            { 0, 255, 0, 0, 255, 0 },
            { 0, 255, 255, 0, 0, 0 },
            { 0, 255, 255, 0, 255, 0 },
            { 0, 0, 0, 0, 0, 0 }
        })
    };
}