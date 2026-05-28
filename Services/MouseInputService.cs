using System.Runtime.InteropServices;

namespace QuickTools.Services;

public sealed class MouseInputService
{
    private const uint InputMouse = 0;
    private const uint LeftDown = 0x0002;
    private const uint LeftUp = 0x0004;
    private const uint RightDown = 0x0008;
    private const uint RightUp = 0x0010;
    private const uint MiddleDown = 0x0020;
    private const uint MiddleUp = 0x0040;

    public void Click(string button, string clickType)
    {
        var (down, up) = button switch
        {
            "Right Click" => (RightDown, RightUp),
            "Middle Click" => (MiddleDown, MiddleUp),
            _ => (LeftDown, LeftUp)
        };

        SendClick(down, up);

        if (clickType == "Double click")
        {
            Thread.Sleep(60);
            SendClick(down, up);
        }
    }

    private static void SendClick(uint down, uint up)
    {
        var inputs = new[]
        {
            CreateMouseInput(down),
            CreateMouseInput(up)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    }

    private static Input CreateMouseInput(uint flags)
    {
        return new Input
        {
            Type = InputMouse,
            MouseInput = new MouseInput { Flags = flags }
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, Input[] inputs, int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public MouseInput MouseInput;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
