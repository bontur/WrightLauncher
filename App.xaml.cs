using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Reflection;
using System.IO;

namespace WrightLauncher;

public partial class App : Application
{
    public App()
    {
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        try
        {
            var assemblyName = new AssemblyName(args.Name).Name;
            if (assemblyName == null) return null;

            var resourceName = assemblyName + ".dll";
            
            var embeddedDlls = new[]
            {
                "CommunityToolkit.Mvvm.dll",
                "Newtonsoft.Json.dll",
                "SocketIO.Core.dll",
                "SocketIO.Serializer.Core.dll",
                "SocketIO.Serializer.SystemTextJson.dll",
                "SocketIOClient.dll"
            };

            if (!embeddedDlls.Contains(resourceName))
                return null;

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null) return null;

            var assemblyData = new byte[stream.Length];
            stream.Read(assemblyData, 0, assemblyData.Length);
            return Assembly.Load(assemblyData);
        }
        catch
        {
            return null;
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;

        Services.LocalizationService.Instance.Load("en_US");

        if (Current.Dispatcher != null)
        {
            Current.Dispatcher.Thread.Priority = System.Threading.ThreadPriority.AboveNormal;
        }

        this.Exit += App_Exit;
        this.SessionEnding += App_SessionEnding;

        var splashWindow = new SplashWindow();
        splashWindow.Show();

        base.OnStartup(e);
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        try
        {
            CleanupAllProcesses();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"App_Exit hatası: {ex.Message}");
        }
    }

    private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        try
        {
            CleanupAllProcesses();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"App_SessionEnding hatası: {ex.Message}");
        }
    }

    private void CleanupAllProcesses()
    {
        try
        {
            var currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var processesToKill = new[] { "mod-tools", "WrightLauncher" };

            foreach (var processName in processesToKill)
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.Id != currentProcessId)
                        {
                            process.Kill();
                            process.Dispose();
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
        }
    }
}


