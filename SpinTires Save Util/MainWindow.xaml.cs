using System;
using System.Linq;
using System.Windows;
//customs
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Threading;

namespace SpinTires_Save_Util
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] defMaps = { "level_coast.sts", "level_hill.sts", "level_plains.sts", "level_river.sts", "level_volcano.sts" };
        const string configFileName = "configPath.txt";
        const string savesDirectoryName = "saves";
        string savesDirectoryPath;
        const string defsavesDirectoryName = "UserSaves";
        string usersavesPath;
        string configPath;
        string[] saves;
        string[] maps;
        string workingDirectory;
        string lastMap;
        const string gameExe = "SpinTires";
        DispatcherTimer saveGuard = new DispatcherTimer();
        DispatcherTimer delay = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {//search for config, load maps
            try
            {
                if (!File.Exists(configFileName))
                {
                    OpenFileDialog selectPath = new OpenFileDialog();
                    selectPath.FileName = "Users/USER/Roaming/AppData/Spintires/Config.xml";
                    selectPath.Title = "Select path to your Config.xml";
                    selectPath.Filter = "Config.xml (*.xml)|*.xml";
                    selectPath.ShowDialog();
                    using (StreamWriter sw = new StreamWriter(configFileName, true))
                    {
                        sw.Write(selectPath.FileName);
                    }
                }
                using (StreamReader sr = new StreamReader(configFileName))
                {
                    configPath = sr.ReadLine();
                }
                //read the config
                using (StreamReader sr = new StreamReader(configPath))
                {
                    string line = sr.ReadLine();
                    while (!line.Contains("WorkingDirectory"))
                    {
                        line = sr.ReadLine();
                    }
                    string[] temp3 = line.Split('"');
                    workingDirectory = temp3[1];
                }
                //save directory - fill up the listbox
                if (!Directory.Exists(savesDirectoryName))
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\" + savesDirectoryName);
                }
                savesDirectoryPath = String.Format(Directory.GetCurrentDirectory() + "\\" + savesDirectoryName);
                usersavesPath = configPath.Replace("Config.xml", "") + defsavesDirectoryName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            loadMapsListbox();
            loadSavesListbox();

        //saveguard - dispatchertimer
            saveGuard.Tick += saveGuard_Tick;
            delay.Tick += delay_Tick;
        }

        void delay_Tick(object sender, EventArgs e)
        {
            delay.Stop();
            runSaveGuard();
        }

        private void bSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (lbMaps.SelectedIndex == -1)
            {
                MessageBox.Show("You have not selected save or map");
                return;
            }
            if (!File.Exists(usersavesPath + "\\" + lbMaps.SelectedItem.ToString()))
            {
                MessageBox.Show("You do not have any save for this map");
                return;
            }
            if (tbSaveName.Text == String.Empty)
            {
                MessageBox.Show("Please, give a name for your save");
                return;
            }
            try
            {
                File.Copy(usersavesPath + "\\" + lbMaps.SelectedItem.ToString(), savesDirectoryPath + "\\" + tbSaveName.Text + ".sts", true);
                MessageBox.Show("Saved as: " + tbSaveName.Text + ".sts");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            loadSavesListbox();
        }

        public void loadSavesListbox()
        {
            try
            {
                lbSaves.Items.Clear();
                long size = 0;
                FileInfo[] fis = new DirectoryInfo(savesDirectoryPath).GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }
                if (size == 0)
                    return;

                saves = Directory.GetFiles(savesDirectoryPath).Select(path => Path.GetFileName(path)).ToArray();
                foreach (string saveName in saves)
                {
                    lbSaves.Items.Add(saveName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void loadMapsListbox()
        {
            try
            {
                lbMaps.Items.Clear();
                long size = 0;
                FileInfo[] fis = new DirectoryInfo(usersavesPath).GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }
                if (size == 0)
                    return;

                maps = Directory.GetFiles(usersavesPath).Select(path => Path.GetFileName(path)).ToArray();
                foreach (string mapName in maps)
                {
                    lbMaps.Items.Add(mapName);
                }
                bSelectLastMap.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void bLoadSave_Click(object sender, RoutedEventArgs e)
        {
            if (lbMaps.SelectedIndex == -1 || lbSaves.SelectedIndex == -1)
            {
                MessageBox.Show("You have not selected save or map");
                return;
            }
            loadSave();
            MessageBox.Show("Loaded: " + lbSaves.SelectedItem.ToString() + "\nYou can load save only for current playing map\nIf you want to change the map, quit game and use utility and Start with selected save button");
        }

        public void loadSave()
        {
            try
            {
                File.Delete(usersavesPath + "\\" + lbMaps.SelectedItem.ToString());
                File.Copy(savesDirectoryPath + "\\" + lbSaves.SelectedItem.ToString(), usersavesPath + "\\" + lbMaps.SelectedItem.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void bLoadSaveAndRun_Click(object sender, RoutedEventArgs e)
        {
            if (lbMaps.SelectedIndex == -1 || lbSaves.SelectedIndex == -1)
            {
                MessageBox.Show("You have not selected save or map");
                return;
            }
            if (Process.GetProcessesByName(gameExe).Length == 0)
            {
                loadSave();
                try
                {
                    string text = File.ReadAllText(configPath);
                    for (int i = 0; i < defMaps.Length; i++)
                    {
                        text = text.Replace(defMaps[i].Replace(".sts", ""), "_._._");
                    }
                    text = text.Replace("_._._", lbMaps.SelectedItem.ToString().Replace(".sts", ""));
                    File.WriteAllText(configPath, text);
                    if (Process.GetProcessesByName(gameExe).Length == 0)
                    {
                        //Process.Start(workingDirectory + "\\SpinTires.exe");
                        Process.Start(workingDirectory);
                    }
                    delay.Interval = new TimeSpan(0, 0, 30);
                    delay.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Game is already running\nIf you want to load this save, please, quit the game and try again");
            }

        }

        private void bDeleteSave_Click(object sender, RoutedEventArgs e)
        {
            if (lbSaves.SelectedIndex == -1)
            {
                MessageBox.Show("You have not selected save");
                return;
            }
            File.Delete(savesDirectoryPath + "\\" + lbSaves.SelectedItem.ToString());
            MessageBox.Show("Deleted save: " + lbSaves.SelectedItem.ToString());
            loadSavesListbox();
        }

        public void runSaveGuard()
        {
            try
            {
                bRunSaveguard.IsEnabled = false;
                saveGuard.Interval = new TimeSpan(0, 0, 5);
                saveGuard.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void saveGuard_Tick(object sender, EventArgs e)
        {
            if (Process.GetProcessesByName(gameExe).Length == 0)
            {
                saveGuard.Stop();
                bRunSaveguard.IsEnabled = true;
                MessageBoxResult result = MessageBox.Show("Do you want to save your game ?", "SaveGuard", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                    this.Activate();
                else
                    this.Close();
            }
        }

        private void bRunSaveguard_Click(object sender, RoutedEventArgs e)
        {
            runSaveGuard();
        }

        private void bFAQ_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("#1 First step for savegame ? Pause the game, select Main Menu, confirm Yes, resume (Continue) to map if you want\n#2 How to savegame with utility ? First do #1, then select map you want to save (in list on the left), type name for save and click the button Save selected map as (can overwrite existing)\n#3 How to load my save ? You need to pause the game, go to the main menu and start utility, select your save in the SAVES list and corresponding map in the MAPS list, then click the Load selected save button and click Continue from menu in game - now you can try again from saved position :)\n#4 How to launch the game with my save ? Select your save in the SAVES list, corresponding map in the MAPS list and click the Start game with selected save button, when game is launched, now you can click Continue and start playing your old save (SaveGuard will automatically start after 30 seconds)\n#5 SaveGuard - If you have launched the game yet, you can start SaveGuard - He will check every 5 seconds if the game is still running, if it doesnt, he will ask if you want to save your game, so you cant lose/overwrite your progress at the end of playing\n\nTIPS:\nDont close this program if you are using SaveGuard - you will terminate it\nName your saves properly, so you now which map corresponds\nYou can load only the save for current playing map, if you want to change the map, quit game and use utility and Start with selected save button\n\n.created by banyy for ALL versions of SpinTires, ENJOY :)", "FAQ/HELP/ABOUT");
        }

        private void bSelectLastMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DirectoryInfo mapsDir = new DirectoryInfo(usersavesPath);
                var myFile = mapsDir.GetFiles().OrderByDescending(f => f.LastWriteTime).First();
                lastMap = myFile.Name;

                int i = 0;
                while (lastMap != lbMaps.Items[i].ToString())
                    i++;

                lbMaps.SelectedIndex = i;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
