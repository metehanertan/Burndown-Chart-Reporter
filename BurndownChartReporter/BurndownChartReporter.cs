using System;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Net;
using System.Windows.Forms;

namespace BurndownChartReporter
{
    class BurndownChartReporter
    {
        TfsTeamProjectCollection tpc = null;
        WorkItemStore workItemStore = null;
        RichTextBox feedBox;

        public BurndownChartReporter(RichTextBox feedBox){
            this.feedBox = feedBox;
        }

        //Change feedback box text
        public void changeFeed(string str)
        {
            feedBox.Text += str + "\n";
        }

        //Connecting to TFS servers
        public int connectToTFS(string serverPath)
        {
            if (serverPath == null)
            {
                throw new Exception("serverPath can not be null!");
            }

            if (tpc != null)
            {
                tpc.Dispose();
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                tpc = new TfsTeamProjectCollection(new Uri(serverPath));
                workItemStore = tpc.GetService<WorkItemStore>();
                return 1;
            }
            catch
            {
                return 0;
            }
        }

        //Creating burndown chart
        public void burndownChartCreate(DateTime startDate, int sprintLength, string userName, string txtPath,Boolean runStore, 
            string rootDirectory,string date)
        {

            //Query to see active tasks that is assigned to wanted user
            string wiql = string.Format(
                "SELECT [System.Id] " +
                "FROM WorkItems WHERE [System.TeamProject] = 'TIA' AND [System.WorkItemType] = 'Task' " +
                "AND [System.WorkItemType] = 'Task' " +
                "AND [System.State] = 'Active' " +
                "AND ([System.AssignedTo] = '{0}') ",
                userName);

            //Adding paths to query
            string pathTxt = rootDirectory + "Paths.txt";
            string[] paths = System.IO.File.ReadAllLines(pathTxt);
            Boolean first = true;
            foreach(string path in paths)
            {
                if (first)
                {
                    wiql += string.Format("AND ([System.AreaPath] UNDER '{0}' ",path);
                    first = false;
                }
                else
                {
                    wiql += string.Format("OR [System.AreaPath] UNDER '{0}' ",path);
                }

            }
            wiql += ")";

            //Running query and storing result
            WorkItemCollection queryResults = workItemStore.Query(wiql);
            try
            {
                double totalRemainingWork = 0;
                foreach (WorkItem workItem in queryResults)
                {
                    if(workItem.Fields["Remaining Work"].Value != null)
                        totalRemainingWork += (double)workItem.Fields["Remaining Work"].Value;      
                }

                //Store total remaining time of that user
                string path = rootDirectory + date;
                //If program is run in the same day, dont double store the data
                if (runStore)
                {
                    Reporter.store(userName, totalRemainingWork, txtPath);
                }
                
                //Needed variables for creating chart
                string worker = userName.Replace(" ", "");
                string chartPath = $"{rootDirectory}{date}\\{worker}-BurnDownChart.png";
                //Create chart
                Reporter.createUserChart(chartPath, startDate, sprintLength, txtPath, userName);
            }
            catch (Exception ex)
            {
                changeFeed(ex.ToString());
            }
        }
    }
}