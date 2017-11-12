using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace AVD_Manager
{
    public partial class MainWindow : Window
    {
        const string SDK1 = "ANDROID_HOME";
        const string SDK2 = "ANDROID_SDK_ROOT";
        const string AVD = "ANDROID_SDK_HOME";

        readonly CommonOpenFileDialog _commonDialog;
        string _emulator = "";

        public MainWindow()
        {
            _commonDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                AddToMostRecentlyUsedList = true,
                ShowPlacesList = true
            };
            InitializeComponent();
        }

        void OnSelectSdkClicked(object sender, RoutedEventArgs e)
        {
            if (_commonDialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            edtSdk.Text = _commonDialog.FileName;
            _emulator = Path.Combine(edtSdk.Text, @"tools\emulator.exe");
            SetEnvironmentAsync(edtSdk.Text).Wait();
            PopulateDeviceList();
        }

        void OnSelectAvdClicked(object sender, RoutedEventArgs e)
        {
            if (_commonDialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            edtAvd.Text = _commonDialog.FileName;
            Environment.SetEnvironmentVariable(AVD, edtAvd.Text, EnvironmentVariableTarget.Process);
            PopulateDeviceList();
            Debug.WriteLine($"Inside:{nameof(MainWindow)}\n" +
                            $" %{SDK1}%: {Environment.GetEnvironmentVariable(SDK1, EnvironmentVariableTarget.Process)}\n" +
                            $" %{SDK2}%: {Environment.GetEnvironmentVariable(SDK2, EnvironmentVariableTarget.Process)}\n" +
                            $" %{AVD}%: {Environment.GetEnvironmentVariable(AVD, EnvironmentVariableTarget.Process)}");
        }

        void OnRemoveClicked(object sender, RoutedEventArgs e)
        {
            var avdManager = Path.Combine(edtSdk.Text, @"tools\bin\avdmanager.bat");
            if (!File.Exists(avdManager))
            {
                Debug.WriteLine($"Inside:{nameof(OnRemoveClicked)} avdmanager.bat does not exist");
                return;
            }
            var ini = Path.Combine(edtAvd.Text, @".android\avd", $"{lsbAvds.SelectedItem.ToString()}.ini");
            Debug.WriteLine("INI: " + ini);
            var avd = Path.Combine(edtAvd.Text, @".android\avd", $"{lsbAvds.SelectedItem.ToString()}.avd");
            Debug.WriteLine("AVD: " + avd);

            var result = MessageBox.Show("Do you want to delete this AVD?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                if (File.Exists(ini)) File.Delete(ini);
                if (Directory.Exists(avd)) Directory.Delete(avd, true);
            }

            PopulateDeviceList();
        }

        void OnResetClicked(object sender, RoutedEventArgs e)
        {
            //var result = RunCmd(_emulator, $"{lsbAvds.SelectedItem.ToString()} -wipe-data", false);
            // Debug.WriteLine($"Inside:{nameof(OnResetClicked)} success: {result.isSuccess} message: {result.message} stdOutput: {result.stdOutput} stdError: {result.stdError}");
        }

        void OnCreateClicked(object sender, RoutedEventArgs e)
        {
            // todo: code to create an avd
        }

        void OnLaunchClicked(object sender, RoutedEventArgs e)
        {
            var result = RunCmd(_emulator, $"-avd {lsbAvds.SelectedItem.ToString()}");
            Debug.WriteLine($"Inside:{nameof(OnLaunchClicked)} success: {result.isSuccess} message: {result.message} stdOutput: {result.stdOutput} stdError: {result.stdError}");
        }

        void PopulateDeviceList()
        {
            if (!File.Exists(_emulator)) return;
            var result = RunCmd(_emulator, "-list-avds");
            if (result.isSuccess) lsbAvds.ItemsSource = GetVertualAndroidDevices(result.stdOutput);
            Debug.WriteLine($"Inside:{nameof(PopulateDeviceList)} success: {result.isSuccess} message: {result.message} stdOutput: {result.stdOutput} stdError: {result.stdError}");
        }

        static (bool isSuccess, string message, string stdError, string stdOutput)
            RunCmd(string fileName, string args, bool hideWindow = true)
        {
            var result = (isSuccess: false, message: "", stdError: "", stdOutput: "");

            if (!File.Exists(fileName))
            {
                result.message = "file does not exist";
                return result;
            }

            var p = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = hideWindow,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            p.Start();

            result.stdOutput = p.StandardOutput.ReadToEndAsync().Result;
            result.stdError = p.StandardError.ReadToEndAsync().Result;
            result.isSuccess = result.stdError == "";
            p.WaitForExit();
            return result;
        }

        static async Task SetEnvironmentAsync(string fileName)
        {
            Environment.SetEnvironmentVariable(SDK1, fileName, EnvironmentVariableTarget.Process);
            while (fileName != Environment.GetEnvironmentVariable(SDK1, EnvironmentVariableTarget.Process))
            {
                await Task.Delay(1000);
            }
            Environment.SetEnvironmentVariable(SDK2, fileName, EnvironmentVariableTarget.Process);
            while (fileName != Environment.GetEnvironmentVariable(SDK2, EnvironmentVariableTarget.Process))
            {
                await Task.Delay(1000);
            }
            Debug.WriteLine($"Inside:{nameof(MainWindow)}\n" +
                            $" %{SDK1}%: {Environment.GetEnvironmentVariable(SDK1, EnvironmentVariableTarget.Process)}\n" +
                            $" %{SDK2}%: {Environment.GetEnvironmentVariable(SDK2, EnvironmentVariableTarget.Process)}\n" +
                            $" %{AVD}%: {Environment.GetEnvironmentVariable(AVD, EnvironmentVariableTarget.Process)}");
        }

        static IEnumerable<string> GetVertualAndroidDevices(string stdOutput)
            => stdOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Where(l => l != "");
    }
}
