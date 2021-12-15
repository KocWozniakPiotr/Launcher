using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Diagnostics;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;
using System.Text;

namespace Launcher
{
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();
        Version ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        Uri urlSys = new Uri("https://www.asyllion.duckdns.org/system.zip");
        Uri urlGame = new Uri("https://www.asyllion.duckdns.org/Lineage2-H5-Asyllion.zip");
        bool buttonsActive = true;
        bool upgradeReady = false;
        string currentVersion, latestVersion;
        bool summoning = false;
        bool updateAll = false;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        string prevContent = "";
        bool extracting = false;
        string currentPath = Directory.GetCurrentDirectory();
        float prevDirSize;
        double lastPercent;
        string[] gameDirs = { "ForceFeedback", "system", "System", "Animations", "L2text", "MAPS", "music", "Replay", "Sounds", "StaticMeshes", "SysTextures", "Textures", "Voice" };
        string[] gameFiles = { "patchw32.dll" };
        string gamePath = " \"" + Directory.GetCurrentDirectory() + @"\system\l2.exe" +"\" ";
        string[] lines;
        int fullscreen_line = -1;
        int[] resX = { 800, 1024, 1280, 1366, 1440, 1600, 1920, 2560, 3840 };
        int[] resY = { 600, 768, 720, 768, 900, 900, 1080, 1440, 2160 };

        public MainWindow()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; // IMPORTANT FOR WEB CLIENT CONNECTION !!!
            CheckArgs();
            if (summoning)
            {
                this.WindowState = WindowState.Minimized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            versionLabel.Content = "v. " + $"{ver}";
            MouseDown += Window_MouseDown;
            OnAppStartUp();
            CheckConfig();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string addR = "REG ADD ";
            ProcessStartInfo startInfo = new ProcessStartInfo("CMD.exe");
            startInfo.Arguments = @"/C " + addR + "\"HKCU\\Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers\"" + @" /V" + gamePath + "/T REG_SZ /D ~DPIUNAWARE /F";
            startInfo.CreateNoWindow = true;
            Process.Start(startInfo);

            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            CheckServer();
            SearchUpdate();

            for (int i = 0; i < resX.Length; i++)
            {
                ResBox.Items.Add(resX[i].ToString() + " x " + resY[i].ToString());
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void DisableButtons()
        {
            buttonsActive = false;
            LaunchGameButton.Opacity = 0.3f;
            UpdaterButton.Opacity = 0.3f;
        }

        private void EnableButtons()
        {
            buttonsActive = true;
            LaunchGameButton.Opacity = 1f;
            UpdaterButton.Opacity = 1f;
        }

        private void LaunchGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (buttonsActive)
            {
                DisableButtons();
                if (Directory.Exists(@"system"))
                {
                    
                    if (File.Exists(Directory.GetCurrentDirectory() + @"\system\Option.ini"))
                    {
                        File.WriteAllLines(Directory.GetCurrentDirectory() + @"\system\Option.ini", lines, Encoding.UTF8);
                    }
                    ProcessStartInfo l2 = new ProcessStartInfo(@"system/l2.exe");
                    l2.UseShellExecute = false;
                    l2.CreateNoWindow = false;
                    Process.Start(l2);
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show("Either system/l2.exe doesn't exist or you didn't put Launcher inside the game main directory. Be sure to disable Hyper-V if you get 'Virtual Machine' error");
                }
            }
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            string url = " https://www.discord.gg/rJYsMyKvaB";
            ProcessStartInfo discordPage = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = url
            };
            Process.Start(discordPage);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ShowUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            ShowUpdateButton.Visibility = Visibility.Hidden;
            numOne.Visibility = Visibility.Hidden;
            DownloadMode.Visibility = Visibility.Hidden;
            upgradeReady = true;
            StatusBox.Content = "                >-- NEW VERSION " + latestVersion.Substring(0, 7) + " AVAILABLE ! --<" + Environment.NewLine;
            StatusBox.Content += latestVersion.Substring(8);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (buttonsActive)
            {
                DisableButtons();
                if (upgradeReady)
                {
                    StartUpgradeAsync();
                }
                else
                {
                    StatusBox.Content = "";
                    if (updateAll)
                    {
                        var _ = UpdateGame();
                    }
                    else
                    {
                        var _ = UpdateSystem();
                    }
                }
            }
        }

        private async Task UpdateSystem()
        {
            if (Directory.Exists(@"system")) { Directory.Delete(@"system", true); }
            if (Directory.Exists(@"System")) { Directory.Delete(@"System", true); }
            prevContent = StatusBox.Content.ToString();
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChangedEventHandler);
            await client.DownloadFileTaskAsync(urlSys, @"system.zip");
            StatusBox.Content += "extracting files... ";
            Directory.CreateDirectory(@"system");
            await Task.Run(() => ZipFile.ExtractToDirectory(@"system.zip", @"system"));
            await Task.Run(() => File.Delete(@"system.zip"));
            StatusBox.Content += "DONE!" + Environment.NewLine;
            StatusBox.Content += "now start the game ^^";
            EnableButtons();
        }

        private void DownloadProgressChangedEventHandler(object sender, DownloadProgressChangedEventArgs e)
        {
            float current = e.BytesReceived / 1024;
            float total = e.TotalBytesToReceive / 1024;
            StatusBox.Content = prevContent + "downloading system files... " + Math.Round((current / total) * 100, 1) + "%";
            if (current == total)
            {
                StatusBox.Content = prevContent + "downloading system files... DONE!" + Environment.NewLine;
            }
        }

        private async Task UpdateGame()
        {
            string currentPath = Directory.GetCurrentDirectory();
            StatusBox.Content = "";
            CleanDirectories();
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadGameProgressChangedEventHandler);
            await client.DownloadFileTaskAsync(urlGame, @"Lineage2-H5-Asyllion.zip");
            prevContent = StatusBox.Content.ToString();
            prevDirSize = GetDirectorySize(currentPath);
            extracting = true;
            await Task.Run(() => ZipFile.ExtractToDirectory(@"Lineage2-H5-Asyllion.zip", currentPath));
            StatusBox.Content += "cleaning temp...";
            await Task.Run(() => File.Delete(@"Lineage2-H5-Asyllion.zip"));
        }

        private void CleanDirectories()
        {
            StatusBox.Content = "cleaning old data...";
            foreach (string dir in gameDirs)
                if (Directory.Exists(@dir))
                {
                    Directory.Delete(@dir);
                }
            foreach (string file in gameFiles)
                if (File.Exists(@file))
                {
                    File.Delete(@file);
                }
        }

        private void DownloadGameProgressChangedEventHandler(object sender, DownloadProgressChangedEventArgs e)
        {
            float current = e.BytesReceived / 1024;
            float total = e.TotalBytesToReceive / 1024;
            StatusBox.Content = "downloading game files... " + Math.Round((current / total) * 100, 1) + "%";
            if (current == total)
            {
                StatusBox.Content = "downloading game files... DONE!" + Environment.NewLine;
            }
        }

        private static long GetDirectorySize(string folderPath)
        {
            DirectoryInfo di = new DirectoryInfo(folderPath);
            return di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (extracting)
            {
                if (Directory.Exists(currentPath))
                {
                    float total = 11611800004;
                    double current = Math.Round(((GetDirectorySize(currentPath) - prevDirSize) / total) * 100, 1);
                    if (current < lastPercent)
                    {
                        StatusBox.Content = prevContent + "extracting files ... DONE!" + Environment.NewLine;
                        extracting = false;
                        var _ = UpdateSystem();
                    }
                    lastPercent = current;
                    StatusBox.Content = prevContent + "extracting files ... " + current.ToString() + "%";
                }
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CheckServer()
        {
            WebClient client = new WebClient();
            string status = "";
            string news = "";
            try
            {
                status = client.DownloadString("https://www.asyllion.duckdns.org/status.txt");
                if (status[1].ToString() == "0")
                {
                    GameStatus.Foreground = new SolidColorBrush(Colors.Red);
                    GameStatus.Content = "SERVER - DOWN";
                }
                else if (status[1].ToString() == "1")
                {
                    GameStatus.Foreground = new SolidColorBrush(Colors.LimeGreen);
                    GameStatus.Content = "SERVER - UP";
                }
                news = client.DownloadString("https://www.asyllion.duckdns.org/news.txt");
                StatusBox.Content = news;
            }
            catch
            {
                StatusBox.Content = "Couldn't connect to the server... ";
            }
        }

        private void SearchUpdate()
        {
            WebClient client = new WebClient();
            try
            {
                currentVersion = ver.ToString().Substring(0, 7);
                latestVersion = client.DownloadString("https://www.asyllion.duckdns.org/build-latest/version.txt");
                if (currentVersion != latestVersion.Substring(0, 7))
                {
                    ShowUpdateButton.Visibility = Visibility.Visible;
                    numOne.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                StatusBox.Content = "Couldn't connect to the server... ";
            }
        }

        private void StartUpgradeAsync()
        {
            File.Copy(@"Launcher.exe", @"upgrade.exe");
            Process.Start(@"upgrade.exe", "upgrading");
            Application.Current.Shutdown();
        }

        private async void SummonLatestAsync()
        {
            WebClient client = new WebClient();
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadDataCompletedEventHandler);
            await client.DownloadFileTaskAsync("https://www.asyllion.duckdns.org/build-latest/Launcher.exe", @"Launcher.exe");
        }

        private void DownloadDataCompletedEventHandler(object sender, AsyncCompletedEventArgs e)
        {
            Process.Start(@"Launcher.exe");
            Application.Current.Shutdown();
        }

        private void OnAppStartUp()
        {

            if (summoning)
            {
                if (File.Exists(@"Launcher.exe"))
                {
                    File.Delete(@"Launcher.exe");
                }
                SummonLatestAsync();
            }
            else
            {
                if (File.Exists(@"upgrade.exe"))
                {
                    File.Delete(@"upgrade.exe");
                }
                if (File.Exists(@"_launcher.exe"))
                {
                    File.Delete(@"_launcher.exe");
                }
            }
        }

        private void DownloadMode_Checked(object sender, RoutedEventArgs e)
        {
            if (DownloadMode.IsChecked == true)
            {
                updateAll = true;
            }
        }

        private void DownloadMode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DownloadMode.IsChecked == false)
            {
                updateAll = false;
            }
        }

        private void Fullscr_Checked(object sender, RoutedEventArgs e)
        {
            if (Fullscr.IsChecked == true)
            {
                if (fullscreen_line != -1)
                {
                    lines[fullscreen_line] = "StartupFullScreen=True";
                }

            }
        }

        private void Fullscr_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Fullscr.IsChecked == false)
            {
                if (fullscreen_line != -1)
                {
                    lines[fullscreen_line] = "StartupFullScreen=False";
                }
            }
        }

        private void CheckArgs()
        {
            try
            {
                if (Environment.GetCommandLineArgs()[1].ToString() == "upgrading")
                {
                    summoning = true;
                }
            }
            catch { }
        }

        private void CheckConfig()
        {
            if (Directory.Exists(Directory.GetCurrentDirectory() + @"\system"))
            {
                if (File.Exists(Directory.GetCurrentDirectory() + @"\system\Asyllion.ini") == false)
                {
                    var sw = new StreamWriter(Directory.GetCurrentDirectory() + @"\system\Asyllion.ini");
                    sw.Close();
                }
            }

            if (Directory.Exists(Directory.GetCurrentDirectory() + @"\system"))
            {
                int counter = 0;
                foreach (string line in System.IO.File.ReadLines(Directory.GetCurrentDirectory() + @"\system\Option.ini"))
                {
                    if (line == "StartupFullScreen=True")
                    {
                        Fullscr.IsChecked = true;
                        fullscreen_line = counter;
                    }
                    else if (line == "StartupFullScreen=False")
                    {
                        Fullscr.IsChecked = false;
                        fullscreen_line = counter;
                    }
                    counter++;
                }
                lines = new string[counter];
                counter = 0;
                foreach (string line in File.ReadLines(Directory.GetCurrentDirectory() + @"\system\Option.ini"))
                {
                    lines[counter] = line;
                    counter++;
                }
            }
            else
            {
                Fullscr.Visibility = Visibility.Hidden;
            }
        }
    }
}
