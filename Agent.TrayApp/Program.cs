using System;
using System.Windows.Forms;

namespace Agent.TrayApp;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        try
        {
            Console.WriteLine("[Agent.TrayApp] Starting...");
            ApplicationConfiguration.Initialize();
            Console.WriteLine("[Agent.TrayApp] WinForms initialized. Launching TrayApplicationContext...");
            Application.Run(new TrayApplicationContext());
            Console.WriteLine("[Agent.TrayApp] Application exited normally.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Agent.TrayApp] FATAL ERROR: {ex}");
            MessageBox.Show($"ComputerDoctor Agent failed to start:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "ComputerDoctor Agent Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
