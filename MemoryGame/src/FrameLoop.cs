using Emgu.CV;
using Emgu.CV.CvEnum;

namespace MemoryGame;

public abstract class FrameLoop
{
    protected Mat p_BaseFrame;
    protected Mat p_BinaryFrame;
    protected Mat p_ContourFrame;

    public void Run()
    {
        while (true)
        {
            OnFrame();
        }
    }

    public abstract void OnFrame();
    protected void GenerateFrames()
    {
        var grayFrame = new Mat();
        CvInvoke.CvtColor(p_BaseFrame, grayFrame, ColorConversion.Bgr2Gray);

        p_BinaryFrame = new Mat();
        CvInvoke.Threshold(grayFrame, p_BinaryFrame, 120, 255, ThresholdType.Otsu);
    }
}