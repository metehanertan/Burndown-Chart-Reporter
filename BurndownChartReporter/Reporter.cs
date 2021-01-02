using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;

namespace BurndownChartReporter
{
    class Reporter
    {
        //Creating chart for a user
        public static void createUserChart(string path, DateTime dateTime,
            int sprintlength, string workTxtPath, string username)
        {
            //Get chart values
            string[] lines = System.IO.File.ReadAllLines(workTxtPath);
            int length = lines[0].Split(';').Length;
            double[] remainingWorks = new double[length];
            for (int i = 0; i < lines.Length; i++)
            {
                //Checks whether the data is connected to user
                string[] parsedStr = lines[i].Split(';');
                if (parsedStr[0].Equals(username))
                {
                    for (int a = 1; a < parsedStr.Length-1; a++)
                    {
                        remainingWorks[a] = Convert.ToDouble(parsedStr[a]);
                    }
                }
            }
            createChart(path, dateTime, sprintlength, remainingWorks,1);
        }
        
        //Creating chart for the team
        public static void createTeamChart(DateTime dateTime, int sprintlength, string workTxtPath, int userSize,string rootDirectory)
        {
            //Calculate chart values
            string[] lines = System.IO.File.ReadAllLines(workTxtPath);
            int length = lines[0].Split(';').Length;
            double[] remainingWorks = new double[length];
            foreach (string str in lines)
            {
                string[] parsedStr = str.Split(';');
                for (int i = 1; i < parsedStr.Length-1; i++)
                {
                    remainingWorks[i] += Convert.ToDouble(parsedStr[i]);
                }
            }
            //Create chart
            string date = DateTime.Now.Day + "." + DateTime.Now.Month + "." + DateTime.Now.Year;
            string chartPath = $"{rootDirectory}\\Team Charts\\{date}.png";
            createChart(chartPath, dateTime, sprintlength, remainingWorks,userSize);
        }

        //Creating chart with given data
        public static void createChart( string chartPath, DateTime dateTime, int sprintlength, double[] remainingWorks, int userSize)
        {
            try
            {   
                //Creating chart
                Chart mych = new Chart();
                mych.Titles.Add("Burndown Chart");
                mych.Width = sprintlength * 90;
                //Adding series
                mych.Series.Clear();
                //Remaining work series
                mych.Series.Add("burndown");
                mych.Series["burndown"].SetDefault(true);
                mych.Series["burndown"].Enabled = true;
                mych.Series["burndown"].ChartType = SeriesChartType.Line;
                mych.Series["burndown"].BorderWidth = 8;
                mych.Series["burndown"].IsValueShownAsLabel = true;
                mych.Series["burndown"].Color = System.Drawing.Color.DarkGreen;
                //Estimate line that starts where burndown ends
                mych.Series.Add("estimate");
                mych.Series["estimate"].SetDefault(true);
                mych.Series["estimate"].Enabled = true;
                mych.Series["estimate"].ChartType = SeriesChartType.Line;
                mych.Series["estimate"].BorderWidth = 5;
                mych.Series["estimate"].BorderDashStyle = ChartDashStyle.Dash;
                mych.Series["estimate"].Color = System.Drawing.Color.Orange;
                //Estimate line starts from day 1 to end of the sprint
                mych.Series.Add("total");
                mych.Series["total"].SetDefault(true);
                mych.Series["total"].Enabled = true;
                mych.Series["total"].ChartType = SeriesChartType.Line;
                mych.Series["total"].BorderWidth = 5;
                mych.Series["total"].BorderDashStyle = ChartDashStyle.Dot;
                mych.Series["total"].Color = System.Drawing.Color.LightBlue;

                mych.Visible = true;
                ChartArea chA = new ChartArea();
                chA.AxisX.LabelStyle.Interval = 1;
                chA.AxisX.MajorGrid.Enabled = false;
                chA.AxisY.Minimum = 0;
                mych.ChartAreas.Add(chA);

                //Adding Points
                DateTime startDate = dateTime;
                double last = remainingWorks[remainingWorks.Length-2];
                double estimateSpeed = ((remainingWorks[1]) - last) / (remainingWorks.Length - 2);
                double weeklyWorkHour = 40.5;
                double total = ((int)(sprintlength/7))* weeklyWorkHour * userSize;
                double totalSpeed = (total) / (sprintlength);
                int i = 1;

                //Burndown line and total line
                for (; i < remainingWorks.Length-1;i++)
                {
                    mych.Series["burndown"].Points.AddXY(startDate.Date, remainingWorks[i]);
                    if ((startDate.DayOfWeek == DayOfWeek.Saturday) || (startDate.DayOfWeek == DayOfWeek.Sunday) || (startDate.DayOfWeek == DayOfWeek.Monday))
                    {
                        mych.Series["burndown"].Points[i - 1].Color = System.Drawing.Color.Gray;
                    }

                    mych.Series["total"].Points.AddXY(startDate.Date, total);
                    total -= totalSpeed;

                    startDate = startDate.AddDays(1);
                }

                //Estimation line starts where burndownline ends
                startDate = startDate.AddDays(-1);
                mych.Series["estimate"].Points.AddXY(startDate.Date, last);
                last -= estimateSpeed;
                startDate = startDate.AddDays(1);

                //Estimation line and total line
                for (; i < sprintlength+2;i++)
                {
                    mych.Series["estimate"].Points.AddXY(startDate.Date, last);
                    last -= estimateSpeed;

                    mych.Series["total"].Points.AddXY(startDate.Date, total);
                    total -= totalSpeed;

                    startDate = startDate.AddDays(1);
                }

                //Saving chart
                mych.Show();
                mych.SaveImage(chartPath, ChartImageFormat.Png);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString()); 
            }
        }

        //Create Txt
        public static void createTxt(CheckedListBox checkedListBox,string txtPath)
        {
            using (StreamWriter sw = File.CreateText(txtPath));
            string text="";
            //Adding users to txt file
            foreach (string user in checkedListBox.Items)
            {
                text += user + ";\n";
            }
            System.IO.File.WriteAllText(txtPath, text);
        }


        //Store Values
        public static void store(string username, double remaningWork,string workTxtPath)
        {
            
            string[] lines = System.IO.File.ReadAllLines(workTxtPath);
            for(int i = 0; i < lines.Length;i++)
            {
                //Adding new data to corresponding line
                string[] parsedStr = lines[i].Split(';');
                if (parsedStr[0].Equals(username))
                    lines[i] = String.Concat(lines[i], String.Format("{0};", remaningWork));
            }
            System.IO.File.WriteAllLines(workTxtPath, lines);
        }
        
        //Sending mail
        public static void sendMail(string chartPath, string userName, string rootDirectory, string date)
        {
            string teamChartPath = rootDirectory + "\\Team Charts\\" + date + ".png";
            string mailAddress = getMailAddress(userName,rootDirectory);
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("mail.siemens.de");
                SmtpServer.Port = 25;

                mail.IsBodyHtml = true;
                //Adding chart to mail
                mail.AlternateViews.Add(getEmbeddedImage(chartPath));
                mail.From = new MailAddress("metehan.ertan@siemens.com");
                mail.To.Add("metehan.ertan@siemens.com");
                mail.Subject = "Burndown Chart Reporter " + date + " Daily Report";
                mail.Body = date+"'s daily burndownchart.";

                //Adding burndown chart to attachment
                System.Net.Mail.Attachment chartAttachment;
                chartAttachment = new System.Net.Mail.Attachment(chartPath);
                mail.Attachments.Add(chartAttachment);

                //Adding team chart to attachment
                System.Net.Mail.Attachment teamAttachment;
                teamAttachment = new System.Net.Mail.Attachment(teamChartPath);
                mail.Attachments.Add(teamAttachment);

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential("metehan.ertan@siemens.com", "Kaju.1499@Kaju");
                SmtpServer.EnableSsl = true;

                //Sending mail
                SmtpServer.Send(mail);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        //Adding image to mail
        private static AlternateView getEmbeddedImage(String filePath)
        {
            LinkedResource res = new LinkedResource(filePath);
            res.ContentId = Guid.NewGuid().ToString();
            string htmlBody = @"<img src='cid:" + res.ContentId + @"'/>";
            AlternateView alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
            alternateView.LinkedResources.Add(res);
            return alternateView;
        }

        //Getting mail address of a wanted user
        private static string getMailAddress(string username, string rootDirectory)
        {
            string userTxtPath = rootDirectory + "Users.txt";
            string[] users = System.IO.File.ReadAllLines(userTxtPath);

            //If there isnt any mail address
            if (users.Length == 0)
                MessageBox.Show("There is no mail stored");

            //Checking email
            foreach (string user in users)
            {
                string[] mail = user.Split(';');
                if (mail[0].Equals(username))
                    return mail[1];
            }
            return "";
        }
    }
}