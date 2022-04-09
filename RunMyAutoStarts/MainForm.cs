using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace RunMyAutoStarts
{
    public partial class MainForm : Form
    {
        string curr_directory = AppDomain.CurrentDomain.BaseDirectory;
        string nircmd = AppDomain.CurrentDomain.BaseDirectory + "nircmdc.exe";

        KeyboardHook hook = new KeyboardHook();
        string[] appList = { };

        string configFile = "config.csv";
        Dictionary<string, ProcessInfo> processes = new Dictionary<string, ProcessInfo>();
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            Hide();

            RegisterHotKey();
            ReadProcessList();
            AutoStart();
            CreateMenu();
            PopulateDropDown();
            this.WindowState = FormWindowState.Minimized;
            Hide();
        }
        private void RegisterHotKey()
        {
            // register the event that is fired after the key press.
            hook.KeyPressed +=
                new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);
            // register the control + alt + F12 combination as hot key.
            hook.RegisterHotKey(RunMyAutoStarts.ModifierKeys.Shift, Keys.F1);
        }

        void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            ShowForm();
        }

        private void PopulateDropDown()
        {
            Array.Clear(appList, 0, appList.Length);
            appList = processes.Where(x => (x.Key != "Open Config" && x.Value.CommandType != "S")).Select(x => x.Key).ToArray();
            appList = appList.Skip(2).ToArray();
            cb_apps.DataSource = appList.ToArray();
            cb_apps.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cb_apps.AutoCompleteSource = AutoCompleteSource.ListItems;

        }
        private void ReadProcessList()
        {
            processes.Clear();
            if (File.Exists(configFile))
            {
                string[] lines = File.ReadAllLines(configFile);

                int cnt = 1;
                foreach (string line in lines)
                {
                    if (cnt == 1)
                    {
                        cnt++;
                        continue; //Skip the title
                    }

                    string[] vs = line.Split(',');

                    vs[0] = vs[0] == "Separator" ? cnt.ToString() : vs[0];
                    
                    processes.Add(vs[0], new ProcessInfo{ Path = vs[1],
                        Arguments = vs[2].Replace("\\x",","),                      
                        CommandType = vs[3],
                        Category = vs[4]
                    });

                    cnt++;
                }
            }            
        }

        private void AutoStart()
        {
            foreach(KeyValuePair<string, ProcessInfo> kv in processes)
            {
                if(kv.Value.CommandType == "AS")
                {
                    System.Diagnostics.Process.Start(kv.Value.Path);
                }
            }


            Thread.Sleep(2000);
            string pname = "everything";
            Process[] runList = Process.GetProcessesByName(pname);
            string min_cmd = "";
            if (pname == "everything")
            {
                min_cmd = "win close ititle \"" + "everything" + "\"";
                if (runList.Length > 0)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(nircmd);
                    startInfo.WindowStyle = ProcessWindowStyle.Minimized;

                    startInfo.Arguments = min_cmd;
                    Process.Start(startInfo);

                }
            }

        }

        private void CreateMenu()
        {
            notifyIcon.ContextMenu = new ContextMenu();

            string lv_current_category = "";

            for (int i = 0; i < processes.Count; i++)
            {
                if (i == 0)
                {
                    lv_current_category = processes.ElementAt(i).Value.Category;
                }
                else if (lv_current_category != processes.ElementAt(i).Value.Category)
                {
                    //  notifyIcon.ContextMenu.MenuItems.Add("-");
                    lv_current_category = processes.ElementAt(i).Value.Category;
                }

                if (processes.ElementAt(i).Value.Category == "Main")
                {
                    if(processes.ElementAt(i).Value.CommandType == "S")
                    {
                        notifyIcon.ContextMenu.MenuItems.Add("-");
                    }
                    else
                    {
                        MenuItem newMainMenuItem = new MenuItem(processes.ElementAt(i).Key, new EventHandler(menuItem_Click));
                        notifyIcon.ContextMenu.MenuItems.Add(newMainMenuItem);

                        if (i < processes.Count - 2)
                            if (processes.ElementAt(i + 1).Value.Category != processes.ElementAt(i).Value.Category)
                            {
                                notifyIcon.ContextMenu.MenuItems.Add("-");
                            }
                    }                    
                }
                else
                {
                    MenuItem newMainMenuItem = new MenuItem(processes.ElementAt(i).Value.Category);
                    notifyIcon.ContextMenu.MenuItems.Add(newMainMenuItem);

                    while (true)
                    {
                        if (processes.ElementAt(i).Value.CommandType == "S")
                        {
                            
                            newMainMenuItem.MenuItems.Add("-");
                        }
                        else
                        {
                            newMainMenuItem.MenuItems.Add(new MenuItem(processes.ElementAt(i).Key, new EventHandler(menuItem_Click)));
                        } 
                        
                        if (i == processes.Count - 1)
                        {
                            break;
                        }
                        else if (processes.ElementAt(i + 1).Value.Category == processes.ElementAt(i).Value.Category) //Check whether the next item is also same category
                        {
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (i < processes.Count - 2)
                        if (processes.ElementAt(i + 1).Value.Category == "Main")
                        {
                            notifyIcon.ContextMenu.MenuItems.Add("-");
                        }
                }
            }

        }


        private void menuItem_Click(object sender, EventArgs e)
        {
            string processName = ((System.Windows.Forms.MenuItem)sender).Text;

            LaunchApplication(processName);
        }

        private void LaunchApplication(string processName)
        {
            ProcessInfo lv_process_detail = processes[processName];
            if ((File.Exists(processes[processName].Path) && (processes[processName].CommandType == "A" || processes[processName].CommandType == "AS")) || processes[processName].CommandType == "SC")
            {

                Process new_process = new Process();
                new_process.StartInfo.FileName = processes[processName].Path;
                if (!string.IsNullOrEmpty(processes[processName].Arguments))
                {
                    new_process.StartInfo.Arguments = processes[processName].Arguments;
                }
                new_process.Start();
            }
            else if (lv_process_detail.CommandType == "CC")
            {
                if (processName == "Exit")
                {
                    notifyIcon.Visible = false;
                    Application.Exit();
                }
                else if (processName == "Refresh Menu")
                    RefreshMenu();

            }
            else
                MessageBox.Show("Application Not Available");
        }

        private void menuCommand_Click(object sender, EventArgs e)
        {
            string commandName = ((System.Windows.Forms.MenuItem)sender).Text;
            LaunchCommand(commandName);
        }

        private void LaunchCommand(string commandName)
        {
            if (File.Exists(processes[commandName].Path))
            {
                Process new_process = new Process();
                new_process.StartInfo.FileName = "pwsh.exe";
                new_process.StartInfo.Arguments = processes[commandName].Path;
                new_process.Start();
            }
            else
                MessageBox.Show("Command File Not Available");
        }

        void Exit(object sender, EventArgs e)
        {
            // We must manually tidy up and remove the icon before we exit.
            // Otherwise it will be left behind until the user mouses over.
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if (me.Button == MouseButtons.Left)
            {
                ShowForm();
                this.Left = MousePosition.X - 315;
                this.Top = MousePosition.Y - 60;

            }
        }


        public void RefreshMenu()
        {
            ReadProcessList();          
            CreateMenu();
            PopulateDropDown();
        }
        private void ShowForm()
        {
            Show();
            //this.Left = MousePosition.X - 600;
            //this.Top = MousePosition.Y - 400;
            this.WindowState = FormWindowState.Normal;
            this.CenterToScreen();
            cb_apps.Text = "";
            //notifyIcon.Visible = false;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.WindowState = FormWindowState.Minimized;
                Hide();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                OpenComboxApp();
                this.WindowState = FormWindowState.Minimized;
                Hide();
            }
        }


        private void but_open_Click(object sender, EventArgs e)
        {
            OpenComboxApp();
        }

        private void OpenComboxApp()
        {
            string lv_processName = cb_apps.Text;
            foreach (KeyValuePair<string, ProcessInfo> kv in processes)
            {
                if (kv.Key.ToLower() == lv_processName.ToLower())
                {
                    if (kv.Value.CommandType == "SC")
                    {
                        LaunchApplication(kv.Key);
                    }
                    else
                    {
                        LaunchApplication(kv.Key);
                    }
                    this.WindowState = FormWindowState.Minimized;
                    Hide();
                }
            }
        }

        private void cb_apps_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                OpenComboxApp();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
        }
    }


    public class ProcessInfo
    {
        public string Path { get; set; }
        public string Arguments { get; set; }
        public string CommandType { get; set; }  
        public string Category { get; set; }
    }


}
