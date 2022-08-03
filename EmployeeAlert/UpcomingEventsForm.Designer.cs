
namespace AlertFormV2
{
    partial class UpcomingEventsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpcomingEventsForm));
            this.lbUpcoming = new System.Windows.Forms.ListBox();
            this.lbEventsLog = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lbUpcoming
            // 
            this.lbUpcoming.FormattingEnabled = true;
            this.lbUpcoming.Location = new System.Drawing.Point(4, 39);
            this.lbUpcoming.Name = "lbUpcoming";
            this.lbUpcoming.Size = new System.Drawing.Size(794, 173);
            this.lbUpcoming.TabIndex = 0;
            // 
            // lbEventsLog
            // 
            this.lbEventsLog.FormattingEnabled = true;
            this.lbEventsLog.Location = new System.Drawing.Point(4, 251);
            this.lbEventsLog.Name = "lbEventsLog";
            this.lbEventsLog.Size = new System.Drawing.Size(794, 199);
            this.lbEventsLog.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(344, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Upcoming Alerts";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(334, 225);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Acknowledged Alerts";
            // 
            // UpcomingEventsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lbEventsLog);
            this.Controls.Add(this.lbUpcoming);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "UpcomingEventsForm";
            this.Text = "Upcoming Alerts / Acknowledged Alerts";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UpcomingEventsForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.ListBox lbUpcoming;
        public System.Windows.Forms.ListBox lbEventsLog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}