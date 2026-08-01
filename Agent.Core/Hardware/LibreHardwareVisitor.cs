using LibreHardwareMonitor.Hardware;

namespace Agent.Core.Hardware;

public class LibreHardwareVisitor : IVisitor
{
    public void VisitComputer(IComputer computer) => computer.Traverse(this);

    public void VisitHardware(IHardware hardware)
    {
        try
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }
        catch
        {
            // Suppress hardware update exceptions for unsupported components/sensors
        }
    }

    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}
