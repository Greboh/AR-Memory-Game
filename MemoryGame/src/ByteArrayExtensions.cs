using Emgu.CV;

namespace MemoryGame;

public static class ByteArrayExtensions
{
    public static bool GetMarkerData(this byte[,] markerData, out MarkerData data)
    {
        foreach (var marker in Globals.g_Markers)
        {
            var rotationIndex = markerData.GetRotation(marker.Data.Rotations);
                
            if (rotationIndex == -1)
            {
                continue;
            }

            data = new(marker.Data.Id, marker.Data.Type)
            {
                Rotations = marker.Data.Rotations,
                RotationIndex = rotationIndex
            };
            return true;
        }
        
        data = new(-1, default);
        return false;
    }
    
    private static int GetRotation(this byte[,] markerData, Matrix<byte>[] rotations)
    {
        var tmp = new Matrix<byte>(markerData);
        
        for (var i = 0; i < rotations.Length; i++)
        {
            if (rotations[i].Equals(tmp))
            {
                return i;
            }
        }
        
        return -1;
    }
}