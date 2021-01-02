using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BurndownChartReporter;
using Microsoft.TeamFoundation.Client.CommandLine;

namespace BurndownChartReporter
{
    public partial class MainWindow : Form
    {
        //Global Varibles
        BurndownChartReporter bcr = null;
        Task<int> connection = null;

        //Paths
        string serverPath = "https://venus.tfs.siemens.net:443/tfs/TIA";
        public static readonly string rootDirectory = $"{Environment.CurrentDirectory}\\Charts\\";
        public static readonly string date = DateTime.Now.Day + "." + DateTime.Now.Month + "." + DateTime.Now.Year;
        string workTxtPath = rootDirectory + "RemaingWorkTimes.txt";

        //Connection Vairables
        Boolean newlyOpenned = true;
        int waitTimeBeforeConnect = 1;

        //Autorun variables
        DateTime startDate;
        int sprintLength;
        Boolean autoRun = false;

        public MainWindow()
        {
            InitializeComponent();
            disableAll();
            bcr = new BurndownChartReporter(feedBox);
            this.userNameEnterBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this.userNameEnterBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            checkedListBox1.IntegralHeight = true ;
            //To be able to changed from another thread
            TextBox.CheckForIllegalCrossThreadCalls = false;
            ProgressBar.CheckForIllegalCrossThreadCalls = false;

            //Adding collection
            string userTxtPath = rootDirectory + "Users.txt";
            string[] users = System.IO.File.ReadAllLines(userTxtPath);
            foreach (string user in users)
            {
                string[] name = user.Split(';');
                this.userNameEnterBox.AutoCompleteCustomSource.Add(name[0]);
            }
            feedBox.Text += "Users added.\n";
        }

        //If program is wanted to autorun
        public void autorun(object sender, EventArgs e)
        {
            string settingsTxtPath = rootDirectory + "Settings.txt";
            string[] settings = System.IO.File.ReadAllLines(settingsTxtPath);

            if (settings[0].Equals("1"))
            {
                //Autorun the program
                autoRun = true;
                startDate = DateTime.Parse(settings[1]).Date;
                sprintLength = int.Parse(settings[2]);
                bcr.changeFeed(startDate.ToString());
                addAllButton_Click(sender,e);
                burndownchartreport();
            }
            else
            {
                //Normal run
                if(dateTimePicker1.Value != null)
                {
                    startDate = dateTimePicker1.Value;
                }
                if(!string.IsNullOrEmpty(sprintLengthBox.Text))
                {
                    sprintLength = int.Parse(sprintLengthBox.Text);
                }
            }

        }

        //Dissabling all components
        public void disableAll()
        {
            checkedListBox1.Enabled = false;
            userNameEnterBox.Enabled = false;
            button1.Enabled = false;
            deleteButton.Enabled = false;
            dateTimePicker1.Enabled = false;
            sprintLengthBox.Enabled = false;
            startButton.Enabled = false;
            deleteAll.Enabled = false;
            addAllButton.Enabled = false;
        }

        //Reopnening components after connecting
        public void reopen()
        {
            checkedListBox1.Enabled = true;
            userNameEnterBox.Enabled = true;
            button1.Enabled = true;
            deleteButton.Enabled = true;
            dateTimePicker1.Enabled = true;
            sprintLengthBox.Enabled = true;
            startButton.Enabled = true;
            deleteAll.Enabled = true;
            addAllButton.Enabled = true;
        }

        //Add Button adds new users
        private void button1_Click(object sender, EventArgs e)
        {
            if (!( string.IsNullOrEmpty(userNameEnterBox.Text) ) && !checkedListBox1.Items.Contains(userNameEnterBox.Text))
                checkedListBox1.Items.Add(userNameEnterBox.Text);

            userNameEnterBox.Text = null;
        }

        //Delete button deletes checked users
        private void deleteButton_Click(object sender, EventArgs e)
        {
            for(int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                if (checkedListBox1.CheckedIndices.Contains(i))
                {
                    checkedListBox1.Items.RemoveAt(i);
                    i--;
                }
            }
        }

        //Start button Starts the proccess
        private void button2_Click(object sender, EventArgs e)
        {   
            if ( string.IsNullOrEmpty(sprintLengthBox.Text) || checkedListBox1.Items.Count == 0)
            {
                bcr.changeFeed("Wrong input.");
                reopen();
                return;
            }
            disableAll();
            autorun(sender,e);
            burndownchartreport();
        }

        //Auto connect func of program. Tries to connect to server, if cannot connect waits exponentially to reconnect.
        private async void autoconnect()
        {
            //Auto connect
            connectButton.Enabled = false;
            int con = 0;
            for (int i = 1; i < 6; i++)
            {
                bcr.changeFeed("Auto connect try number:" + i);
                con = connect();

                if (con == 1)
                    break;
                else
                {
                    Application.DoEvents();
                    await Task.Delay(waitTimeBeforeConnect * 1000);
                }
            }

            //If auto connect finishes but couldnt connect during this proccess
            if(con == 0)
            {
                connectButton.BackColor = System.Drawing.Color.Red;
                connectButton.Enabled = true;
            }
        }

        //Connect to server button connects to tfs server
        private int connect()
        {
            connection = new Task<int>(() =>
            {
                return bcr.connectToTFS(serverPath);
            });

            connection.Start();
            int con = connection.Result;
            if (con == 1)
            {
                //Connected
                connectButton.BackColor = System.Drawing.Color.Green;
                connectButton.Text = "Connected";
                connectButton.Enabled = false;
                bcr.changeFeed("Connected to TFS servers.");
                reopen();
            }
            else
            {
                waitTimeBeforeConnect = waitTimeBeforeConnect * 2;
                bcr.changeFeed("Attempt failed, retry after "+ waitTimeBeforeConnect +" seconds");
            }
            return con;
        }

        //When connect button is clicked by hand
        private async void connectButton_Click(object sender, EventArgs e)
        {
            connectButton.BackColor = System.Drawing.Color.Maroon;
            connectButton.Enabled = false;
            bcr.changeFeed("Connecting to TFS servers.");
            int con = connect();

            if (con == 0)
            {
                Application.DoEvents();
                await Task.Delay(waitTimeBeforeConnect * 1000);
                connectButton.Enabled = true;
                connectButton.BackColor = System.Drawing.Color.Red;
            }
        }

        private void burndownchartreport()
        {
            bcr.changeFeed("-----------------------------------\nProgram started.");

            //Get to the current sprint
            while ((startDate.AddDays(sprintLength).Day < DateTime.Now.Day && startDate.AddDays(sprintLength).Month == DateTime.Now.Month)
            || startDate.AddDays(sprintLength).Month < DateTime.Now.Month)
            {
                startDate = startDate.AddDays(sprintLength);
            }

            //If its the begining of a sprint, program creates a new txt file to store data
            if (startDate.Day == DateTime.Now.Day && startDate.Month == DateTime.Now.Month)
                Reporter.createTxt(checkedListBox1, workTxtPath);

            connection.Wait(1000);
            connection.ContinueWith(t =>
            {
                try
                {
                    progressBar1.Value = 0;
                    progressBar1.Maximum = (checkedListBox1.Items.Count * 2) + 1;
                    Boolean runStore = false;
                    string path = rootDirectory + date;
                    if (!System.IO.Directory.Exists(path))
                    {
                        System.IO.Directory.CreateDirectory(rootDirectory + date);
                        runStore = true;
                    }

                    //Create chart for ever entered user
                    bcr.changeFeed("Creating user charts.");
                    foreach (string user in checkedListBox1.Items)
                    {
                        progressBar1.Value++;
                        bcr.burndownChartCreate(startDate, sprintLength, user, workTxtPath, runStore,rootDirectory,date);
                    }
                    bcr.changeFeed("User charts created.");

                    //Create team chart
                    bcr.changeFeed("Creating user team chart.");
                    Reporter.createTeamChart(startDate, sprintLength, workTxtPath, checkedListBox1.Items.Count,rootDirectory);
                    progressBar1.Value++;
                    bcr.changeFeed("Team chart created.");

                    /* Mail part is closed
                     * 
                    //Send mail if its not weekend
                    bcr.changeFeed("Sending mails.");
                    if (!(DateTime.Today.DayOfWeek == DayOfWeek.Saturday) || (DateTime.Today.DayOfWeek == DayOfWeek.Sunday))
                    {
                        //Send user chart and team chart to every user
                        foreach (string user in checkedListBox1.Items)
                        {
                            progressBar1.Value++;
                            string worker = user.Replace(" ", "");
                            string chartPath = $"{rootDirectory}{date}\\{worker}-BurnDownChart.png";
                            Reporter.sendMail(chartPath, user,rootDirectory,date);
                        }
                        bcr.changeFeed("Mails send");
                    }
                    */

                    progressBar1.Value = progressBar1.Maximum;
                    reopen();

                    //Program exit when its autorun
                    if (autoRun)
                    {
                        connection.Wait();
                        System.Windows.Forms.Application.Exit();
                    }
                }
                catch (Exception ex)
                {
                    bcr.changeFeed(ex.ToString());
                }
            });

        }
        
        //Adds all users in collection
        private void addAllButton_Click(object sender, EventArgs e)
        {
            foreach (string name in userNameEnterBox.AutoCompleteCustomSource)
            {
                if ( string.IsNullOrEmpty(name) || checkedListBox1.Items.Contains(name))
                { continue; }

                checkedListBox1.Items.Add(name);
            }
        }

        //Deletes all checkedlist items
        private void deleteAll_Click(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
        }

        //When main window pops-up program tries to autonconnect
        private void MainWindow_Load(object sender, EventArgs e)
        {
            if (newlyOpenned == true)
            {
                autoconnect();
                newlyOpenned = false;
                autorun(sender, e);
            }
            
        }
    }
}
