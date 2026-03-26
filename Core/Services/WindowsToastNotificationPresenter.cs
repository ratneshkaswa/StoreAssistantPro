using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Shows native Windows desktop toasts through a hidden PowerShell WinRT invocation.
/// The presenter also registers a Start Menu shortcut with an explicit AppUserModelID
/// so notifications can flow into the Windows Notification Center for this unpackaged app.
/// </summary>
public sealed class WindowsToastNotificationPresenter : IWindowsNotificationPresenter
{
    private const string AppUserModelId = "StoreAssistantPro.Desktop";
    private const string ShortcutName = "Store Assistant Pro.lnk";
    private readonly ILogger<WindowsToastNotificationPresenter> _logger;
    private readonly Lock _registrationGate = new();
    private bool _registrationAttempted;
    private bool _registrationSucceeded;

    public WindowsToastNotificationPresenter(ILogger<WindowsToastNotificationPresenter> logger)
    {
        _logger = logger;
    }

    public void EnsureRegistered()
    {
        lock (_registrationGate)
        {
            if (_registrationAttempted)
                return;

            _registrationAttempted = true;

            try
            {
                var result = SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
                if (result != 0)
                    Marshal.ThrowExceptionForHR(result);

                EnsureStartMenuShortcut();
                _registrationSucceeded = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Windows notification registration failed");
            }
        }
    }

    public bool TryShow(AppNotification notification)
    {
        EnsureRegistered();

        if (!_registrationSucceeded)
            return false;

        try
        {
            var encodedCommand = BuildEncodedToastCommand(notification);
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = ResolvePowerShellPath(),
                Arguments = $"-NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            return process is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Windows notification display failed");
            return false;
        }
    }

    private static string BuildEncodedToastCommand(AppNotification notification)
    {
        var title = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{GetSeverityGlyph(notification.Level)} {notification.Title}".Trim()));
        var message = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(notification.Message));
        var launchArgs = SecurityElement.Escape(WindowsNotificationLaunchArguments.Build(notification)) ?? string.Empty;
        var script = $$"""
$ErrorActionPreference = 'Stop'
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] > $null
$title = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{title}}'))
$message = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{message}}'))
$xml = [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime]::new()
$xml.LoadXml("<toast launch='{{launchArgs}}'><visual><binding template='ToastGeneric'><text></text><text></text></binding></visual><actions><action content='Open notifications' activationType='foreground' arguments='{{launchArgs}}'/></actions><audio silent='true'/></toast>")
$textNodes = $xml.GetElementsByTagName('text')
$textNodes.Item(0).InnerText = $title
$textNodes.Item(1).InnerText = $message
$toast = [Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType = WindowsRuntime]::new($xml)
$toast.ExpirationTime = [DateTimeOffset]::Now.AddMinutes(10)
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime]::CreateToastNotifier('{{AppUserModelId}}').Show($toast)
""";

        return Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
    }

    private static string GetSeverityGlyph(AppNotificationLevel level) => level switch
    {
        AppNotificationLevel.Success => "\u2713",
        AppNotificationLevel.Warning => "\u26A0",
        AppNotificationLevel.Error => "\u2715",
        _ => "\u2139"
    };

    private static void EnsureStartMenuShortcut()
    {
        var shortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            ShortcutName);

        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);

        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            executablePath = Process.GetCurrentProcess().MainModule?.FileName;
        }

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            executablePath = Path.Combine(AppContext.BaseDirectory, "StoreAssistantPro.exe");
        }

        var shellLink = (IShellLinkW)new ShellLink();

        try
        {
            shellLink.SetPath(executablePath);
            shellLink.SetWorkingDirectory(Path.GetDirectoryName(executablePath)!);
            shellLink.SetIconLocation(executablePath, 0);

            var propertyStore = (IPropertyStore)shellLink;
            using var appIdProp = PropVariant.FromString(AppUserModelId);
            var propertyKey = AppUserModelIdPropertyKey;
            propertyStore.SetValue(ref propertyKey, appIdProp);
            propertyStore.Commit();

            ((IPersistFile)shellLink).Save(shortcutPath, true);
        }
        finally
        {
            if (Marshal.IsComObject(shellLink))
                Marshal.FinalReleaseComObject(shellLink);
        }
    }

    private static readonly PropertyKey AppUserModelIdPropertyKey =
        new(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);

    private static string ResolvePowerShellPath()
    {
        var windowsPowerShell = Path.Combine(
            Environment.SystemDirectory,
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");

        return File.Exists(windowsPowerShell)
            ? windowsPowerShell
            : "powershell.exe";
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pvar);

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink;

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, nint pfd, uint fFlags);
        void GetIDList(out nint ppidl);
        void SetIDList(nint pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(nint hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    private interface IPropertyStore
    {
        uint GetCount();
        void GetAt(uint iProp, out PropertyKey pkey);
        void GetValue(ref PropertyKey key, out PropVariant pv);
        void SetValue(ref PropertyKey key, [In] PropVariant pv);
        void Commit();
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private readonly struct PropertyKey(Guid formatId, uint propertyId)
    {
        public Guid FormatId { get; } = formatId;

        public uint PropertyId { get; } = propertyId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant : IDisposable
    {
        private ushort _valueType;
        private ushort _reserved1;
        private ushort _reserved2;
        private ushort _reserved3;
        private nint _value;
        private int _value2;

        public static PropVariant FromString(string value)
        {
            return new PropVariant
            {
                _valueType = 31,
                _value = Marshal.StringToCoTaskMemUni(value)
            };
        }

        public void Dispose() => PropVariantClear(ref this);
    }
}
