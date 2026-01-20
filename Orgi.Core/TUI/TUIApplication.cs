using Terminal.Gui;

namespace Orgi.Core.TUI;

public static class TUIApplication
{
    public static void Run()
    {
        Application.Init();
        
        if (Application.Top == null)
        {
            Console.Error.WriteLine("Failed to initialize Terminal.Gui");
            Environment.Exit(1);
        }

        try
        {
            Application.UseSystemConsole = true;
            Colors.Base.Normal = Application.Driver.MakeAttribute(Color.White, Color.Black);
            Colors.Base.Focus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray);
            Colors.Base.HotNormal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black);
            Colors.Base.HotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.DarkGray);
            Colors.Menu.Normal = Application.Driver.MakeAttribute(Color.White, Color.Black);
            Colors.Menu.HotNormal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black);
            Colors.Menu.Disabled = Application.Driver.MakeAttribute(Color.Gray, Color.Black);
            Colors.Dialog.Normal = Application.Driver.MakeAttribute(Color.White, Color.Black);
            Colors.Dialog.Focus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray);
            Colors.Dialog.HotNormal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black);
            Colors.Dialog.HotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.DarkGray);

            var mainView = new MainView();
            Application.Top.Add(mainView);
            Application.Run();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"An error occurred: {ex.Message}", "OK");
            Environment.Exit(1);
        }
        finally
        {
            Application.Shutdown();
        }
    }
}
