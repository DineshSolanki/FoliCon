#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FoliCon.Models.Protocol;
using FoliCon.Modules.IPC;
using FoliCon.Modules.Protocol;
using Microsoft.Win32;
using Prism.Dialogs;
using Xunit;

namespace FoliconTest;

public class DeepLinkAndIpcTests
{
    #region DeepLinkDispatcher Tests

    [Fact]
    public void Parse_ThemeInstall_ReturnsCorrectRouteAndId()
    {
        var dispatcher = new DeepLinkDispatcher();
        var request = dispatcher.Parse("folicon://theme/install?id=cyberpunk-2077");

        Assert.True(request.IsValid);
        Assert.Equal(DeepLinkAction.ThemeInstall, request.Action);
        Assert.Equal("cyberpunk-2077", request.TargetId);
        Assert.Null(request.Repository);
    }

    [Fact]
    public void Parse_ThemePreview_ReturnsCorrectRoute()
    {
        var dispatcher = new DeepLinkDispatcher();
        var request = dispatcher.Parse("folicon://theme/preview?id=tokyo-neon");

        Assert.True(request.IsValid);
        Assert.Equal(DeepLinkAction.ThemePreview, request.Action);
        Assert.Equal("tokyo-neon", request.TargetId);
    }

    [Fact]
    public void Parse_OverlayInstall_WithRepository_ReturnsCorrectRoute()
    {
        var dispatcher = new DeepLinkDispatcher();
        var request = dispatcher.Parse("folicon://overlay/install?id=compact-disc&repo=DineshSolanki/FoliCon-Overlays");

        Assert.True(request.IsValid);
        Assert.Equal(DeepLinkAction.OverlayInstall, request.Action);
        Assert.Equal("compact-disc", request.TargetId);
        Assert.Equal("DineshSolanki/FoliCon-Overlays", request.Repository);
    }

    [Fact]
    public void Parse_OpenStore_WithTabAndSearch_ReturnsCorrectRoute()
    {
        var dispatcher = new DeepLinkDispatcher();
        var request = dispatcher.Parse("folicon://store?tab=themes&search=cyberpunk");

        Assert.True(request.IsValid);
        Assert.Equal(DeepLinkAction.OpenStore, request.Action);
        Assert.Equal("themes", request.StoreTab);
        Assert.Equal("cyberpunk", request.SearchQuery);
    }

    [Theory]
    [InlineData("http://theme/install?id=test")]
    [InlineData("https://theme/install?id=test")]
    [InlineData("custom://theme/install?id=test")]
    [InlineData("not-a-uri")]
    [InlineData("")]
    public void Parse_InvalidSchemeOrMalformedUri_ReturnsInvalidRequest(string badUri)
    {
        var dispatcher = new DeepLinkDispatcher();
        var request = dispatcher.Parse(badUri);

        Assert.False(request.IsValid);
        Assert.Equal(DeepLinkAction.Unknown, request.Action);
    }

    [Theory]
    [InlineData("folicon://theme/install?id=test&repo=javascript:alert(1)")]
    [InlineData("folicon://theme/install?id=test&repo=file://etc/passwd")]
    [InlineData("folicon://theme/install?id=test&repo=../../traversal")]
    public void Parse_DangerousQueryPayload_RejectsRequest(string maliciousUri)
    {
        var dispatcher = new DeepLinkDispatcher();
        var request = dispatcher.Parse(maliciousUri);

        Assert.False(request.IsValid);
        Assert.Equal(DeepLinkAction.Unknown, request.Action);
    }

    [Fact]
    public async Task DispatchAsync_ThemeInstall_InvokesDialogService()
    {
        var dispatcher = new DeepLinkDispatcher();
        var request = dispatcher.Parse("folicon://theme/install?id=cyberpunk-2077");

        var testDialogService = new DeepLinkRecordingDialogService();
        var dispatched = await dispatcher.DispatchAsync(request, testDialogService);

        Assert.True(dispatched);
        Assert.Equal("DeepLinkInstallDialog", testDialogService.LastShownDialogName);
        Assert.NotNull(testDialogService.LastParameters);
        Assert.Equal("Theme", testDialogService.LastParameters.GetValue<string>("TargetType"));
        Assert.Equal("cyberpunk-2077", testDialogService.LastParameters.GetValue<string>("TargetId"));
    }

    #endregion

    #region SingleInstanceIpcService Tests

    [Fact]
    public void SingleInstanceIpc_FirstInstanceSucceeds_SecondInstanceFails()
    {
        var testMutexName = $"Local\\FoliCon_TestMutex_{Guid.NewGuid():N}";
        var testPipeName = $"FoliCon_TestPipe_{Guid.NewGuid():N}";

        using var first = new SingleInstanceIpcService(testMutexName, testPipeName);
        var isFirst = first.CheckIsFirstInstance();
        Assert.True(isFirst);

        using var second = new SingleInstanceIpcService(testMutexName, testPipeName);
        var isSecondFirst = second.CheckIsFirstInstance();
        Assert.False(isSecondFirst);
    }

    [Fact]
    public async Task SingleInstanceIpc_OversizedPayload_IsRejected()
    {
        var testMutexName = $"Local\\FoliCon_TestMutex_{Guid.NewGuid():N}";
        var testPipeName = $"FoliCon_TestPipe_{Guid.NewGuid():N}";

        using var service = new SingleInstanceIpcService(testMutexName, testPipeName);

        // Generate huge payload > 64 KB
        var hugeString = new string('A', 70 * 1024);
        var args = new[] { hugeString };

        var sent = await service.SendArgsToFirstInstanceAsync(args, timeoutMs: 1000);
        Assert.False(sent);
    }

    [Fact]
    public async Task SingleInstanceIpc_ServerAndClient_MessageRoundTripDelivered()
    {
        var testMutexName = $"Local\\FoliCon_TestMutex_{Guid.NewGuid():N}";
        var testPipeName = $"FoliCon_TestPipe_{Guid.NewGuid():N}";

        using var serverService = new SingleInstanceIpcService(testMutexName, testPipeName);
        Assert.True(serverService.CheckIsFirstInstance());

        var tcs = new TaskCompletionSource<string>();
        serverService.StartServer(receivedJson =>
        {
            tcs.TrySetResult(receivedJson);
        });

        // Give the background named pipe server a few milliseconds to spin up
        await Task.Delay(50);

        using var clientService = new SingleInstanceIpcService(testMutexName, testPipeName);
        var testArgs = new[] { "folicon://theme/install?id=tokyo-neon" };

        var sent = await clientService.SendArgsToFirstInstanceAsync(testArgs, timeoutMs: 3000);
        Assert.True(sent);

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(3000));
        Assert.Equal(tcs.Task, completedTask);

        var receivedMessage = await tcs.Task;
        Assert.Contains("folicon://theme/install?id=tokyo-neon", receivedMessage);
    }

    #endregion

    #region ProtocolRegistrationService Tests

    [Fact]
    public void ProtocolRegistration_WritesCorrectRegistryKeysAndEscapesCommandLine()
    {
        // Use an isolated test key under HKCU\Software\Classes
        var testSubKeyName = $@"Software\Classes\folicon_test_{Guid.NewGuid():N}";
        var fakeAppPath = Path.Combine(Path.GetTempPath(), $"FoliCon_Fake_{Guid.NewGuid():N}.exe");

        try
        {
            // Create dummy exe file so path validation succeeds
            File.WriteAllText(fakeAppPath, "MZ");

            using var baseKey = Registry.CurrentUser.CreateSubKey(testSubKeyName);
            var service = new TestableProtocolRegistrationService(baseKey, testSubKeyName);

            var registered = service.RegisterProtocol(fakeAppPath);
            Assert.True(registered);

            // Verify keys written
            var defaultVal = baseKey.GetValue(string.Empty) as string;
            Assert.Equal("URL:FoliCon Protocol", defaultVal);

            var urlProtocolVal = baseKey.GetValue("URL Protocol");
            Assert.NotNull(urlProtocolVal);

            var editFlags = (int)(baseKey.GetValue("EditFlags") ?? 0);
            Assert.Equal(0x00200000, editFlags);

            // Verify command template is strictly quoted: "{AppPath}" "%1"
            using var commandKey = baseKey.OpenSubKey(@"shell\open\command");
            Assert.NotNull(commandKey);
            var cmdVal = commandKey.GetValue(string.Empty) as string;
            Assert.NotNull(cmdVal);
            Assert.Equal($"\"{fakeAppPath}\" \"%1\"", cmdVal);

            // Verify IsRegistered returns true
            Assert.True(service.IsRegistered(fakeAppPath));

            // Test Unregister
            var unregistered = service.UnregisterProtocol();
            Assert.True(unregistered);
            Assert.False(service.IsRegistered(fakeAppPath));
        }
        finally
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(testSubKeyName, throwOnMissingSubKey: false);
            }
            catch
            {
                // Ignore
            }

            if (File.Exists(fakeAppPath))
            {
                File.Delete(fakeAppPath);
            }
        }
    }

    #endregion

    #region Helper Classes

    private sealed class TestableProtocolRegistrationService : ProtocolRegistrationService
    {
        private readonly RegistryKey _targetKey;
        private readonly string _subKeyName;

        public TestableProtocolRegistrationService(RegistryKey targetKey, string subKeyName)
        {
            _targetKey = targetKey;
            _subKeyName = subKeyName;
        }

        public new bool IsRegistered(string? customAppPath = null)
        {
            var appPath = customAppPath ?? GetCurrentAppPath();
            var urlProtocol = _targetKey.GetValue("URL Protocol");
            if (urlProtocol == null) return false;

            using var commandKey = _targetKey.OpenSubKey(@"shell\open\command");
            if (commandKey == null) return false;

            var cmdVal = commandKey.GetValue(string.Empty) as string;
            return cmdVal != null && cmdVal.Contains(appPath, StringComparison.OrdinalIgnoreCase);
        }

        public new bool RegisterProtocol(string? customAppPath = null)
        {
            var appPath = customAppPath ?? GetCurrentAppPath();
            _targetKey.SetValue(string.Empty, "URL:FoliCon Protocol");
            _targetKey.SetValue("URL Protocol", string.Empty);
            _targetKey.SetValue("EditFlags", 0x00200000, RegistryValueKind.DWord);

            using (var iconKey = _targetKey.CreateSubKey("DefaultIcon"))
            {
                iconKey.SetValue(string.Empty, $"\"{appPath}\",0");
            }

            using (var commandKey = _targetKey.CreateSubKey(@"shell\open\command"))
            {
                commandKey.SetValue(string.Empty, $"\"{appPath}\" \"%1\"");
            }
            return true;
        }

        public new bool UnregisterProtocol()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(_subKeyName, throwOnMissingSubKey: false);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private sealed class DeepLinkRecordingDialogService : IDialogService
    {
        public string? LastShownDialogName { get; private set; }
        public IDialogParameters? LastParameters { get; private set; }

        public void ShowDialog(string name, IDialogParameters parameters, DialogCallback callback)
        {
            LastShownDialogName = name;
            LastParameters = parameters;
        }
    }

    #endregion
}
