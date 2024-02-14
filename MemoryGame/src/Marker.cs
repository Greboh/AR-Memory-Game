using Emgu.CV;
using Emgu.CV.CvEnum;

namespace MemoryGame;

public enum MARKERTYPE
{
    None = 0,
    Type_01,
    Type_02
}

public struct MarkerData
{
    public int Id { get; private set; }
    public int RotationIndex { get; set; } = -1;
    public Matrix<byte>[] Rotations { get; set; }= new Matrix<byte>[4];
    public MARKERTYPE Type { get; set; }

    public MarkerData(int id, MARKERTYPE type) : this()
    {
        Id = id;
        Type = type;
    }

    public override string ToString()
    {
        return $"MarkerType >> {Type}:";
    }
}
    
public class Marker
{
    public MarkerData Data { get; }
        
    public Marker(int id, MARKERTYPE type, byte[,] initialData)
    {
        Data = new(id, type);

        CalculateRotations(initialData);
    }
        
    /// <summary>
    /// Calculates all possible rotations for any given marker
    /// This is needed because we cannot predict what the markers rotation is in the real world
    /// </summary>
    /// <param name="initialData"></param>
    private void CalculateRotations(byte[,] initialData)
    {
        // First rotation index should always be the captured rotation 
        Data.Rotations[0] = new(initialData);

        // Rotate three times with 90 degrees this insures that we get all possible rotations
        // NOTE: first param should be an already known rotation. Second param should be the next rotation
        for (int i = 0; i < 3; i++)
        {
             Data.Rotations[i + 1] = new(Globals.g_Count, Globals.g_Count);
            CvInvoke.Rotate( Data.Rotations[i],  Data.Rotations[i + 1], RotateFlags.Rotate90CounterClockwise);
        }    
    }
}