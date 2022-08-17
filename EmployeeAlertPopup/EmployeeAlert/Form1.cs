using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ApplicationLogger;

namespace AlertFormV2
{
    public partial class Form1 : Form
    {
        public SqlConnection                conn                = new SqlConnection();
        public SqlCommand                   comm                = new SqlCommand();
        public DataTable                    table               = new DataTable();
        public DataTable                    table2              = new DataTable();
        public DataTable                    eventLogTable       = new DataTable();
        public DataTable                    settingsTable       = new DataTable();
        public List<string>                 list                = new List<string>();
        public static List<string>          queryList           = new List<string>();
        public Dictionary<int, DateTime>    dateDict            = new Dictionary<int, DateTime>();
        public static int                   dropdownCounter     = 1;
        public static int                   j                   = 0;
        public static int                   k                   = 0;
        public static bool                  isImmediate         = false;
        public static int                   immWsEvID           = 0;
        public static int                   currentFormsCounter = 0;
        UpcomingEventsForm                  uef                 = new UpcomingEventsForm();
        public static bool                  isConnected         = false;
        public Logger                       log                 = new Logger("Popup Program", "1.0.0", LoggerLogLevel.Info, "");
        public Timer                        tmrDriver           = new Timer();
        public Timer                        tmrUpdate           = new Timer();
        public Timer                        tmrPersist          = new Timer();
        public Timer                        tmrPersistAfterAck  = new Timer();
        public Timer                        tmrInitializer      = new Timer();
        public Form1()
        {
            //Ensures a connection can be made to the database
            //If it cannot, it displays an error message and then quits the application
            //*****
            DB_Connect(1);
            DB_Close();
            //*****
            InitializeComponent();
            tmrDriver.Tick           += TmrDriver_Tick;
            tmrDriver.Interval        = 900;
            tmrUpdate.Tick           += TmrUpdate_Tick;
            tmrPersist.Tick          += TmrPersist_Tick;
            tmrPersistAfterAck.Tick  += TmrPersistAfterAck_Tick;
            tmrInitializer.Tick      += TmrInitializer_Tick;
            tmrInitializer.Interval   = 500000;
            databaseQuery("select RefreshIntervalMinutes from Settings", 1, true, settingsTable);
            databaseQuery("select Workstations.WS_ID, Workstations.WS_Computer_Name, Workstations.WS_Description, Workstations.RefreshIntervalMinutes, Workstations.InactiveDate as WorkstationsInactiveDate, Workstations.DisplayLocation, WorkstationEvents.WS_EV_ID, WorkstationEvents.Inactive as WorkstationEventsInactive, Workstations.LastActivity, Workstations.Inactive as WorkstationsInactive, Events.EV_ID, Events.EV_Description, Events.MessageText, Events.Frequency, Events.HourlyAtMinute, Events.WeeklyDayOfWeek, Events.MonthlyDay, Events.TimeToRun, Events.PersistMinutes, Events.PersistMinutesAfterAck, Events.Inactive as EventsInactive from Workstations inner join WorkstationEvents on Workstations.WS_ID = WorkstationEvents.WS_ID inner join Events on WorkstationEvents.EV_ID = Events.EV_ID where WS_Computer_Name = '" + Environment.MachineName + "' order by Workstations.WS_Description", 1, true, table);
            if (k <= 0)
            {
                uef.Show();
            }
            if (table.Rows.Count > 0)
            {
                initializer();
            }
            else
            {
                tmrUpdate.Stop();
                tmrInitializer.Start();
            }
        }
        private void TmrInitializer_Tick(object sender, EventArgs e)
        {
            databaseQuery("select Workstations.WS_ID, Workstations.WS_Computer_Name, Workstations.WS_Description, Workstations.RefreshIntervalMinutes, Workstations.InactiveDate as WorkstationsInactiveDate, Workstations.DisplayLocation, WorkstationEvents.WS_EV_ID, WorkstationEvents.Inactive as WorkstationEventsInactive, Workstations.LastActivity, Workstations.Inactive as WorkstationsInactive, Events.EV_ID, Events.EV_Description, Events.MessageText, Events.Frequency, Events.HourlyAtMinute, Events.WeeklyDayOfWeek, Events.MonthlyDay, Events.TimeToRun, Events.PersistMinutes, Events.PersistMinutesAfterAck, Events.Inactive as EventsInactive from Workstations inner join WorkstationEvents on Workstations.WS_ID = WorkstationEvents.WS_ID inner join Events on WorkstationEvents.EV_ID = Events.EV_ID where WS_Computer_Name = '" + Environment.MachineName + "' order by Workstations.WS_Description", 1, true, table);
            if (table.Rows.Count > 0)
            {
                initializer();
                tmrInitializer.Stop();
                tmrUpdate.Start();
            }
            else
            {
                tmrInitializer.Stop();
                tmrInitializer.Start();
            }
        }

        private void TmrPersistAfterAck_Tick(object sender, EventArgs e)
        {
            this.Close();
            cmbNames.Items.Clear();
            tmrPersistAfterAck.Stop();
            this.BackColor              = Color.Red;
            this.txtMessage.BackColor   = Color.Red;
            this.cmbNames.Visible       = true;
            this.btnAcknow.Visible      = true;
            this.label1.Visible         = true;
            cmbNames.SelectedIndex      = -1;
            isImmediate                 = false;
        }

        private void TmrPersist_Tick(object sender, EventArgs e)
        {
            this.Close();
            tmrPersist.Stop();
            queryList.RemoveAt(0);
            cmbNames.Items.Clear();
            isImmediate             = false;
            cmbNames.SelectedIndex  = -1;
        }
        public DayOfWeek numberToWeek(int i)
        {
            if (i == 1)
            {
                return DayOfWeek.Sunday;
            }
            else if (i == 2)
            {
                return DayOfWeek.Monday;
            }
            else if (i == 3)
            {
                return DayOfWeek.Tuesday;
            }
            else if (i == 4)
            {
                return DayOfWeek.Wednesday;
            }
            else if (i == 5)
            {
                return DayOfWeek.Thursday;
            }
            else if (i == 6)
            {
                return DayOfWeek.Friday;
            }
            else
            {
                return DayOfWeek.Saturday;
            }
        }
        public static DateTime FirstDateOfWeek(int year, int weekOfYear, DayOfWeek dayOfWeek, int hours, int minutes, int seconds)
        {
            DateTime    jan1            = new DateTime(year, 1, 1);
            int         daysOffset      = dayOfWeek - jan1.DayOfWeek;
            DateTime    firstThursday   = jan1.AddDays(daysOffset);
            var         cal             = CultureInfo.CurrentCulture.Calendar;
            int         firstWeek       = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            var         weekNum         = weekOfYear;
            if (firstWeek == 1)
            {
                weekNum   -= 1;
            }
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(0).AddHours(hours).AddMinutes(minutes).AddSeconds(seconds); 
        }
        public void databaseQuery(string commandText, int num, bool tbl, [Optional] DataTable table)
        {
            DB_Connect(num);
            comm.CommandType    = CommandType.Text;
            comm.CommandText    = commandText;
            comm.Connection     = conn;
            if (tbl)
            {
                if (table.Rows.Count > 0)
                {
                    table.Rows.Clear();
                }
                table.Load(comm.ExecuteReader());
            }
            else
            {
                comm.ExecuteReader();
            }
            DB_Close();
        }
        public void DB_Connect(int num)
        {
            try
            {
                if (num == 1)
                {
                    conn                    = new SqlConnection();
                    conn.ConnectionString   = ("Data Source=comsql;Initial Catalog=WS_Notification;Integrated Security=SSPI;Trusted_Connection=True");
                    conn.Open();
                }
                else if (num == 2)
                {
                    conn                    = new SqlConnection();
                    conn.ConnectionString   = ("Data Source=comsql;Initial Catalog=M1_P1;User ID=M1_Jobs;Password=M1_Jobs_Reader");
                    conn.Open();
                }
            }
            catch (Exception ex)
            {
                DialogResult dr = MessageBox.Show(ex.ToString(), "ERROR: FAILED TO CONNECT TO DATABASE", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Environment.Exit(1);
            }
        }
        public void DB_Close()
        {
            try
            {
                conn.Close();
                conn = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            cmbNames.Visible            = false;
            btnAcknow.Visible           = false;
            label1.Visible              = false;
            this.BackColor              = Color.Green;
            this.txtMessage.BackColor   = Color.Green;
            tmrPersist.Enabled          = false;
            tmrPersist.Stop();
            if (cmbNames.SelectedIndex != -1)
            {
                if (isImmediate)
                {
                    databaseQuery("Update WorkstationEvents set Inactive = 1, InactiveDate = '" + DateTime.Now + "' where EV_ID = '" + immWsEvID + "'", 1, false);
                }
                tmrPersistAfterAck.Enabled  = true;
                this.txtMessage.Text        = this.txtMessage.Text.Trim() + " - Acknowledged";
                this.txtMessage.Refresh();
                tmrPersistAfterAck.Start();
                databaseQuery(queryList[0] + DateTime.Now.ToString() + "', '" + cmbNames.SelectedItem.ToString() + "')", 1, false);
                log.Log("queryList count after updating at " + DateTime.Now.ToLongTimeString() + ": " + queryList.Count, LoggerLogLevel.Info);
                queryList.RemoveAt(0);
                uef.lbEventsLog.Items.Clear();
                databaseQuery("select * from EventLog where WS_ID = '" + table.Rows[0]["WS_ID"].ToString() + "' order by AcknowledgedTime desc", 1, true, eventLogTable);
                foreach (DataRow row in eventLogTable.Rows)
                {
                    uef.lbEventsLog.Items.Add("Log ID: " + row["Log_ID"].ToString() + "\t\tEvent ID: " + row["EV_ID"].ToString() + "\t\tAcknowledged Time: " + row["AcknowledgedTime"].ToString() + "\t\tAcknowledged User: " + row["AcknowledgedUser"].ToString());
                }
                uef.lbEventsLog.Refresh();
            }
            else
            {
                MessageBox.Show("Must Select User!");
            }
            j++;
        }

        //This method generates items for the combo box on the alert forms when the user clicks the combo box
        private void comboBox1_Click(object sender, EventArgs e)
        {
                databaseQuery("SELECT lmeUserID FROM M1_P1.dbo.Employees INNER JOIN M1_P1.dbo.Timecards ON lmeEmployeeID = lmpEmployeeID WHERE lmeTerminationDate Is Null And lmpActive = 1 ORDER BY lmeUserID", 1, true, table2);
                foreach (DataRow r in table2.Rows)
                {
                    cmbNames.Items.Add(r["lmeUserID"].ToString());
                }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        //This method is called once upon startup. It first calls a SQL query, based on the current computer's name, that grabs records from three different tables and loads it into 
        //a datatable. Then, it goes through each row of this datatable and looks at the frequency record; depending on what this is, a new DateTime record is created for the next
        //time a popup is going to be displayed. Each DateTime object is then added to a dictionary object
        public void initializer()
        {
            databaseQuery("select * from EventLog where WS_ID = '" + table.Rows[0]["WS_ID"].ToString() + "' order by AcknowledgedTime desc", 1, true, eventLogTable);
            foreach (DataRow row in eventLogTable.Rows)
            {
                uef.lbEventsLog.Items.Add("Log ID: " + row["Log_ID"].ToString() + "\t\tEvent ID: " + row["EV_ID"].ToString() + "\t\tAcknowledged Time: " + row["AcknowledgedTime"].ToString() + "\t\tAcknowledged User: " + row["AcknowledgedUser"].ToString());
            }
            if (!this.Visible)
            {
                this.notifyIcon1.Visible = true;
            }

            if (int.Parse(table.Rows[0]["RefreshIntervalMinutes"].ToString()) == 0)
            {
                tmrUpdate.Interval = int.Parse(settingsTable.Rows[0]["RefreshIntervalMinutes"].ToString()) * 60000;
            }
            else
            {
                tmrUpdate.Interval = int.Parse(table.Rows[0]["RefreshIntervalMinutes"].ToString()) * 60000;
            }

            if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 1)
            {
                this.Top  = 0;
                this.Left = 0;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 2)
            {
                this.Top  = 0;
                this.Left = Screen.PrimaryScreen.Bounds.Width / 4;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 3)
            {
                this.Top  = 0;
                this.Left = Screen.PrimaryScreen.Bounds.Width - this.Size.Width;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 4)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height / 4;
                this.Left = 0;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 5)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height / 4;
                this.Left = Screen.PrimaryScreen.Bounds.Width / 4;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 6)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height / 4;
                this.Left = Screen.PrimaryScreen.Bounds.Width - this.Size.Width;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 7)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height - this.Size.Height;
                this.Left = 0;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 8)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height - this.Size.Height;
                this.Left = Screen.PrimaryScreen.Bounds.Width / 4;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 9)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height - this.Size.Height;
                this.Left = Screen.PrimaryScreen.Bounds.Width - this.Size.Width;
            }

            foreach (DataRow row in table.Rows)
            {
                if (row["Frequency"].ToString() == "M" && bool.Parse(row["EventsInactive"].ToString()) == false)
                {
                    DateTime newDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, int.Parse(row["MonthlyDay"].ToString()), int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2)));
                    if (DateTime.Compare(newDate, DateTime.Now) > 0 /*&& DateTime.Now.ToShortDateString() == newDate.ToShortDateString()*/)
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), newDate);
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + newDate);
                    }
                    else
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), newDate.AddMonths(1));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + newDate.AddMonths(1));
                    }
                    k++;
                }
                else if (row["Frequency"].ToString() == "W" && bool.Parse(row["EventsInactive"].ToString()) == false)
                {
                    DateTime dw = FirstDateOfWeek(DateTime.Now.Year, (DateTime.Now.DayOfYear / 7), numberToWeek(int.Parse(row["WeeklyDayOfWeek"].ToString())), int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))).AddDays(7);
                    if (DateTime.Compare(dw, DateTime.Now) > 0)
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, dw.Day, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, dw.Day, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                    }
                    else
                    {
                        if (dw.Day + 7 > DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
                        {
                            dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, ((dw.Day + 7) - DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)), int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                            uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, ((dw.Day + 7) - DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)), int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                        }
                        else
                        {
                            dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, dw.Day + 7, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                            uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, dw.Day + 7, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                        }
                    }
                    k++;
                }
                else if (row["Frequency"].ToString() == "D" && bool.Parse(row["EventsInactive"].ToString()) == false)
                {
                    if (TimeSpan.Compare(DateTime.Now.TimeOfDay, TimeSpan.Parse(row["TimeToRun"].ToString())) < 0)// && DateTime.Now.TimeOfDay == TimeSpan.Parse(row["TimeToRun"].ToString()))
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                    }
                    else
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                    }
                    k++;
                }
                else if (row["Frequency"].ToString() == "H" && bool.Parse(row["EventsInactive"].ToString()) == false)
                {
                    if (DateTime.Now.Minute < int.Parse(row["HourlyAtMinute"].ToString()))
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, int.Parse(row["HourlyAtMinute"].ToString()), 0));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, int.Parse(row["HourlyAtMinute"].ToString()), 0));
                    }
                    else
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour + 1, int.Parse(row["HourlyAtMinute"].ToString()), 0));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour + 1, int.Parse(row["HourlyAtMinute"].ToString()), 0));
                    }
                    k++;
                }
            }
            tmrDriver.Enabled = true;
            tmrUpdate.Enabled = true;
            tmrDriver.Start();
            tmrUpdate.Start();
        }

        //This ticks every 0.9 seconds. Every tick, this method is looking to see if the current date/time matches with the records that are stored in the dateDict dictionary object
        //If the current date/time matches the record, it then updates the upcoming events form record to whenever the next time the alert will popup, and then 
        //it looks to see if a form is already visible. If not, it pops up a form with the appropriate message.
        //If a form is already visible, it creates and pops up a new form with the appropriate message
        private void TmrDriver_Tick(object sender, EventArgs e)
        {
            foreach (DataRow row in this.table.Rows)
            {
                if (row["Frequency"].ToString() == "M")
                {
                    if (DateTime.Now.ToShortDateString() == dateDict[int.Parse(row["EV_ID"].ToString())].ToShortDateString() && DateTime.Now.ToLongTimeString() == dateDict[int.Parse(row["EV_ID"].ToString())].ToLongTimeString())
                    {
                        foreach (String str in uef.lbUpcoming.Items)
                        {
                            if (str.Contains(dateDict[int.Parse(row["EV_ID"].ToString())].ToString()))
                            {
                                uef.lbUpcoming.Items.Remove(str);
                                uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + dateDict[int.Parse(row["EV_ID"].ToString())].AddMonths(1));
                                uef.lbUpcoming.Refresh();
                                break;
                            }
                        }
                        dateDict[int.Parse(row["EV_ID"].ToString())] = dateDict[int.Parse(row["EV_ID"].ToString())].AddMonths(1);
                        queryList.Add("insert into EventLog values(" + row["WS_ID"].ToString() + ", " + row["EV_ID"].ToString() + ", '");
                        if (this.Visible)
                        {
                            Form1 frm = new Form1();
                            if (this.Left > 0)
                            {
                                frm.Left = 0;
                            }
                            else
                            {
                                frm.Left = Screen.PrimaryScreen.Bounds.Width - this.Size.Width;
                            }
                            frm.tmrPersistAfterAck.Interval = int.Parse(row["PersistMinutesAfterAck"].ToString()) * 60000;
                            frm.tmrPersist.Interval         = int.Parse(row["PersistMinutes"].ToString()) * 60000;
                            frm.txtMessage.Text             = (row["MessageText"].ToString().Trim());
                            frm.TopMost                     = true;
                            frm.ShowInTaskbar               = false;
                            frm.Show();
                            if (frm.tmrPersist.Interval > 0)
                            {
                                frm.tmrPersist.Enabled      = true;
                                frm.tmrPersist.Start();
                            }
                            currentFormsCounter++;
                        }
                        else
                        {
                            tmrPersistAfterAck.Interval = int.Parse(row["PersistMinutesAfterAck"].ToString()) * 60000;
                            tmrPersist.Interval         = int.Parse(row["PersistMinutes"].ToString()) * 60000;
                            this.txtMessage.Text        = (row["MessageText"].ToString().Trim());
                            this.TopMost                = true;
                            this.ShowInTaskbar          = false;
                            this.Show();
                            if (this.tmrPersist.Interval > 0)
                            {
                                this.tmrPersist.Enabled = true;
                                this.tmrPersist.Start();
                            }
                            currentFormsCounter++;
                        }
                    }
                }
                else if (row["Frequency"].ToString() == "W")
                {
                    if (DateTime.Now.DayOfWeek == dateDict[int.Parse(row["EV_ID"].ToString())].DayOfWeek && DateTime.Now.ToLongTimeString() == dateDict[int.Parse(row["EV_ID"].ToString())].ToLongTimeString())
                    {
                        foreach (String str in uef.lbUpcoming.Items)
                        {
                            if (str.Contains(dateDict[int.Parse(row["EV_ID"].ToString())].ToString()))
                            {
                                uef.lbUpcoming.Items.Remove(str);
                                uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + dateDict[int.Parse(row["EV_ID"].ToString())].AddDays(7));
                                uef.lbUpcoming.Refresh();
                                break;
                            }
                        }
                        dateDict[int.Parse(row["EV_ID"].ToString())] = dateDict[int.Parse(row["EV_ID"].ToString())].AddDays(7);
                        queryList.Add("insert into EventLog values(" + row["WS_ID"].ToString() + ", " + row["EV_ID"].ToString() + ", '");
                        if (this.Visible && this.txtMessage.Text != row["MessageText"].ToString().Trim())
                        {
                            Form1 frm = new Form1();
                            if (this.Top > 0)
                            {
                                frm.Top = 0;
                            }
                            else
                            {
                                frm.Top = Screen.PrimaryScreen.Bounds.Height - this.Size.Height;
                            }
                            if (this.Left > 0)
                            {
                                frm.Left = 0;
                            }
                            else
                            {
                                frm.Left = Screen.PrimaryScreen.Bounds.Width - this.Size.Width;
                            }
                            frm.tmrPersistAfterAck.Interval = int.Parse(row["PersistMinutesAfterAck"].ToString()) * 60000;
                            frm.tmrPersist.Interval         = int.Parse(row["PersistMinutes"].ToString()) * 60000;
                            frm.txtMessage.Text             = (row["MessageText"].ToString().Trim());
                            frm.TopMost                     = true;
                            frm.ShowInTaskbar               = false;
                            frm.Show();
                            if (frm.tmrPersist.Interval > 0)
                            {
                                frm.tmrPersist.Enabled      = true;
                                frm.tmrPersist.Start();
                            }
                            currentFormsCounter++;
                        }
                        else
                        {
                            tmrPersistAfterAck.Interval = int.Parse(row["PersistMinutesAfterAck"].ToString()) * 60000;
                            tmrPersist.Interval         = int.Parse(row["PersistMinutes"].ToString()) * 60000;
                            this.txtMessage.Text        = (row["MessageText"].ToString().Trim());
                            this.TopMost                = true;
                            this.ShowInTaskbar          = false;
                            this.Show();
                            if (this.tmrPersist.Interval > 0)
                            {
                                this.tmrPersist.Enabled = true;
                                this.tmrPersist.Start();
                            }
                            currentFormsCounter++;
                        }
                    }
                }
                else if (row["Frequency"].ToString() == "D")
                {
                    if (DateTime.Now.ToLongTimeString() == dateDict[int.Parse(row["EV_ID"].ToString())].ToLongTimeString() && DateTime.Now.Day == dateDict[int.Parse(row["EV_ID"].ToString())].Day)
                    {
                        foreach (String str in uef.lbUpcoming.Items)
                        {
                            if (str.Contains(dateDict[int.Parse(row["EV_ID"].ToString())].ToString()))
                            {
                                uef.lbUpcoming.Items.Remove(str);
                                uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + dateDict[int.Parse(row["EV_ID"].ToString())].AddDays(1));
                                uef.lbUpcoming.Refresh();
                                break;
                            }
                        }
                        dateDict[int.Parse(row["EV_ID"].ToString())] = dateDict[int.Parse(row["EV_ID"].ToString())].AddDays(1);
                        queryList.Add("insert into EventLog values(" + row["WS_ID"].ToString() + ", " + row["EV_ID"].ToString() + ", '");
                        if (this.Visible)
                        {
                            Form1 frm = new Form1();
                            if (this.Top > 0)
                            {
                                frm.Top = 0;
                            }
                            else
                            {
                                frm.Top = Screen.PrimaryScreen.Bounds.Height - this.Size.Height;
                            }
                            frm.tmrPersistAfterAck.Interval = int.Parse(row["PersistMinutesAfterAck"].ToString()) * 60000;
                            frm.tmrPersist.Interval         = int.Parse(row["PersistMinutes"].ToString()) * 60000;
                            frm.txtMessage.Text             = (row["MessageText"].ToString().Trim());
                            frm.TopMost                     = true;
                            frm.ShowInTaskbar               = false;
                            frm.Show();
                            if (frm.tmrPersist.Interval > 0)
                            {
                                frm.tmrPersist.Enabled      = true;
                                frm.tmrPersist.Start();
                            }
                            currentFormsCounter++;
                        }
                        else
                        {
                            tmrPersistAfterAck.Interval = int.Parse(row["PersistMinutesAfterAck"].ToString()) * 60000;
                            tmrPersist.Interval         = int.Parse(row["PersistMinutes"].ToString()) * 60000;
                            this.txtMessage.Text        = (row["MessageText"].ToString().Trim());
                            this.TopMost                = true;
                            this.ShowInTaskbar          = false;
                            this.Show();
                            if (this.tmrPersist.Interval > 0)
                            {
                                this.tmrPersist.Enabled = true;
                                this.tmrPersist.Start();
                            }
                            currentFormsCounter++;
                        }
                    }
                }
                else if (row["Frequency"].ToString() == "H")
                {
                    if (DateTime.Now.ToLongTimeString() == dateDict[int.Parse(row["EV_ID"].ToString())].ToLongTimeString())
                    {
                        foreach (String str in uef.lbUpcoming.Items)
                        {
                            if (str.Contains(dateDict[int.Parse(row["EV_ID"].ToString())].ToString()))
                            {
                                uef.lbUpcoming.Items.Remove(str);
                                uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + dateDict[int.Parse(row["EV_ID"].ToString())].AddHours(1));
                                uef.lbUpcoming.Refresh();
                                break;
                            }
                        }
                        dateDict[int.Parse(row["EV_ID"].ToString())] = dateDict[int.Parse(row["EV_ID"].ToString())].AddHours(1);
                        queryList.Add("insert into EventLog values(" + row["WS_ID"].ToString() + ", " + row["EV_ID"].ToString() + ", '");
                        if (this.Visible)
                        {
                            Form1 frm = new Form1();
                            if (this.Top > 0)
                            {
                                frm.Top = 0;
                            }
                            else
                            {
                                frm.Top = Screen.PrimaryScreen.Bounds.Height - this.Size.Height;
                            }
                            if (this.Left > 0)
                            {
                                frm.Left = 0;
                            }
                            else
                            {
                                frm.Left = Screen.PrimaryScreen.Bounds.Width - this.Size.Width;
                            }
                            frm.tmrPersistAfterAck.Interval = int.Parse(row["PersistMinutesAfterAck"].ToString()) * 60000;
                            frm.tmrPersist.Interval         = int.Parse(row["PersistMinutes"].ToString()) * 60000;
                            frm.txtMessage.Text             = (row["MessageText"].ToString().Trim());
                            frm.TopMost                     = true;
                            frm.ShowInTaskbar               = false;
                            frm.Show();
                            if (frm.tmrPersist.Interval > 0)
                            {
                                frm.tmrPersist.Enabled      = true;
                                frm.tmrPersist.Start();
                            }
                            currentFormsCounter++;
                            
                        }
                        else
                        {
                            tmrPersistAfterAck.Interval = int.Parse(row["PersistMinutesAfterAck"].ToString()) * 60000;
                            tmrPersist.Interval         = int.Parse(row["PersistMinutes"].ToString()) * 60000;
                            this.txtMessage.Text        = (row["MessageText"].ToString().Trim());
                            this.TopMost                = true;
                            this.ShowInTaskbar          = false;
                            this.Show();
                            if (this.tmrPersist.Interval > 0)
                            {
                                this.tmrPersist.Enabled = true;
                                this.tmrPersist.Start();
                            }
                            currentFormsCounter++;
                        }
                        foreach (string s in queryList)
                        {
                            log.Log("Query List Contents at " + DateTime.Now.ToShortTimeString() + ": " + s, LoggerLogLevel.Info);
                        }
                    }
                }
                else if (row["Frequency"].ToString() == "I" && isImmediate == false && bool.Parse(row["WorkstationEventsInactive"].ToString()) == false)
                {
                    queryList.Add("insert into EventLog values(" + row["WS_ID"].ToString() + ", " + row["EV_ID"].ToString() + ", '");
                    isImmediate = true;
                    immWsEvID = int.Parse(row["EV_ID"].ToString());
                    if (this.Visible)
                    {
                        Form1 frm = new Form1();
                        if (!(this.Top == Screen.PrimaryScreen.Bounds.Height / 4))
                        {
                            frm.Top = Screen.PrimaryScreen.Bounds.Height / 4;
                        }
                        if (!(this.Left == Screen.PrimaryScreen.Bounds.Width / 4))
                        {
                            frm.Left = Screen.PrimaryScreen.Bounds.Width / 4;
                        }
                        frm.tmrPersistAfterAck.Interval = int.Parse(row["PersistMinutesAfterAck"].ToString()) * 60000;
                        frm.tmrPersist.Interval         = int.Parse(row["PersistMinutes"].ToString()) * 60000;
                        frm.txtMessage.Text             = (row["MessageText"].ToString().Trim());
                        frm.TopMost                     = true;
                        frm.ShowInTaskbar               = false;
                        frm.Show();
                        if (frm.tmrPersist.Interval > 0)
                        {
                            frm.tmrPersist.Enabled      = true;
                            frm.tmrPersist.Start();
                        }
                        currentFormsCounter++;
                    }
                    else
                    {
                        tmrPersistAfterAck.Interval = int.Parse(row["PersistMinutesAfterAck"].ToString()) * 60000;
                        tmrPersist.Interval         = int.Parse(row["PersistMinutes"].ToString()) * 60000;
                        this.txtMessage.Text        = (row["MessageText"].ToString().Trim());
                        this.TopMost                = true;
                        this.ShowInTaskbar          = false;
                        this.Show();
                        if (this.tmrPersist.Interval > 0)
                        {
                            this.tmrPersist.Enabled = true;
                            this.tmrPersist.Start();
                        }
                        currentFormsCounter++;
                    }
                }
            }
        }

        //The interval for this method is set based on the current workstation's Workstation RefreshInterval record
        //This is used for checking to see if there are any new records to display for the workstation
        //Everytime the timer ticks, the workstation's LastActivity record is updated to the current date/time
        private void TmrUpdate_Tick(object sender, EventArgs e)
        {
            databaseQuery("select Workstations.WS_ID, Workstations.WS_Computer_Name, Workstations.WS_Description, Workstations.RefreshIntervalMinutes, Workstations.InactiveDate as WorkstationsInactiveDate, Workstations.DisplayLocation, WorkstationEvents.WS_EV_ID, WorkstationEvents.Inactive as WorkstationEventsInactive, Workstations.LastActivity, Workstations.Inactive as WorkstationsInactive, Events.EV_ID, Events.EV_Description, Events.MessageText, Events.Frequency, Events.HourlyAtMinute, Events.WeeklyDayOfWeek, Events.MonthlyDay, Events.TimeToRun, Events.PersistMinutes, Events.PersistMinutesAfterAck, Events.Inactive as EventsInactive from Workstations inner join WorkstationEvents on Workstations.WS_ID = WorkstationEvents.WS_ID inner join Events on WorkstationEvents.EV_ID = Events.EV_ID where WS_Computer_Name = '" + Environment.MachineName + "' order by Workstations.WS_Description", 1, true, table);
            databaseQuery("select * from EventLog where WS_ID = '" + table.Rows[0]["WS_ID"].ToString() + "' order by AcknowledgedTime desc", 1, true, eventLogTable);
            dateDict.Clear();
            uef.lbUpcoming.Items.Clear();
            uef.lbEventsLog.Items.Clear();
            foreach (DataRow row in eventLogTable.Rows)
            {
                uef.lbEventsLog.Items.Add("Log ID: " + row["Log_ID"].ToString() + "\t\tEvent ID: " + row["EV_ID"].ToString() + "\t\tAcknowledged Time: " + row["AcknowledgedTime"].ToString() + "\t\tAcknowledged User: " + row["AcknowledgedUser"].ToString());
            }
            foreach (DataRow row in table.Rows)
            {
                if (row["Frequency"].ToString() == "M" && bool.Parse(row["EventsInactive"].ToString()) == false)
                {
                    DateTime newDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, int.Parse(row["MonthlyDay"].ToString()), int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2)));
                    if (DateTime.Compare(newDate, DateTime.Now) > 0 && DateTime.Now.ToShortDateString() == newDate.ToShortDateString())
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), newDate);
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + newDate);
                    }
                    else
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), newDate.AddMonths(1));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + newDate.AddMonths(1));
                    }
                    k++;
                }
                else if (row["Frequency"].ToString() == "W" && bool.Parse(row["EventsInactive"].ToString()) == false)
                {
                    DateTime dw = FirstDateOfWeek(DateTime.Now.Year, (DateTime.Now.DayOfYear / 7), numberToWeek(int.Parse(row["WeeklyDayOfWeek"].ToString())), int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))).AddDays(7);
                    if (DateTime.Compare(dw, DateTime.Now) > 0)
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, dw.Day, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, dw.Day, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                    }
                    else
                    {
                        if (dw.Day + 7 > DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
                        {
                            dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, (dw.Day + 7) - DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                            uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, (dw.Day + 7) - DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                        }
                        else
                        {
                            dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, dw.Day + 7, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                            uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, dw.Day + 7, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                        }
                    }
                    k++;
                }
                else if (row["Frequency"].ToString() == "D" && bool.Parse(row["EventsInactive"].ToString()) == false)
                {
                    if (TimeSpan.Compare(DateTime.Now.TimeOfDay, TimeSpan.Parse(row["TimeToRun"].ToString())) < 0)// && DateTime.Now.TimeOfDay == TimeSpan.Parse(row["TimeToRun"].ToString()))
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                    }
                    else
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, int.Parse(row["TimeToRun"].ToString().Substring(0, 2)), int.Parse(row["TimeToRun"].ToString().Substring(3, 2)), int.Parse(row["TimeToRun"].ToString().Substring(6, 2))));
                    }
                    k++;
                }
                else if (row["Frequency"].ToString() == "H" && bool.Parse(row["EventsInactive"].ToString()) == false)
                {
                    if (DateTime.Now.Minute < int.Parse(row["HourlyAtMinute"].ToString()))
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, int.Parse(row["HourlyAtMinute"].ToString()), 0));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, int.Parse(row["HourlyAtMinute"].ToString()), 0));
                    }
                    else
                    {
                        dateDict.Add(int.Parse(row["EV_ID"].ToString()), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour + 1, int.Parse(row["HourlyAtMinute"].ToString()), 0));
                        uef.lbUpcoming.Items.Add("Name:" + row["EV_Description"].ToString() + "\t\t\t  Frequency: " + row["Frequency"].ToString() + "\t\t\t\t  Next Notification: " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour + 1, int.Parse(row["HourlyAtMinute"].ToString()), 0));
                    }
                    k++;
                }
            }
            if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 1)
            {
                this.Top  = 0;
                this.Left = 0;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 2)
            {
                this.Top  = 0;
                this.Left = Screen.PrimaryScreen.Bounds.Width / 4;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 3)
            {
                this.Top  = 0;
                this.Left = Screen.PrimaryScreen.Bounds.Width - this.Size.Width;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 4)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height / 4;
                this.Left = 0;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 5)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height / 4;
                this.Left = Screen.PrimaryScreen.Bounds.Width / 4;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 6)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height / 4;
                this.Left = Screen.PrimaryScreen.Bounds.Width - this.Size.Width;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 7)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height - this.Size.Height;
                this.Left = 0;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 8)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height - this.Size.Height;
                this.Left = Screen.PrimaryScreen.Bounds.Width / 4;
            }
            else if (int.Parse(table.Rows[0]["DisplayLocation"].ToString()) == 9)
            {
                this.Top  = Screen.PrimaryScreen.Bounds.Height - this.Size.Height;
                this.Left = Screen.PrimaryScreen.Bounds.Width - this.Size.Width;
            }
            foreach (DataRow row in table.Rows)
            {
                if (row["WorkstationsInactive"].ToString() == "True")
                {
                    databaseQuery("update Workstations set Inactive = " + 0 + " where Ws_Computer_Name = '" + Environment.MachineName + "'", 1, false);
                }
            }
            if (int.Parse(table.Rows[0]["RefreshIntervalMinutes"].ToString()) == 0)
            {
                tmrUpdate.Interval = int.Parse(settingsTable.Rows[0]["RefreshIntervalMinutes"].ToString()) * 60000;
            }
            else
            {
                tmrUpdate.Interval = int.Parse(table.Rows[0]["RefreshIntervalMinutes"].ToString()) * 60000;
            }
            databaseQuery("update Workstations set LastActivity = '" + DateTime.Now.ToString() + "' where WS_Computer_Name = '" + Environment.MachineName + "'", 1, false);
        }
    }
}
