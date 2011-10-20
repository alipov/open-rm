namespace OpenRm.Agent.CustomControls
{
    partial class NotifyIconWrapper
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.OpenRmNotifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.OpenRmContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.startAgentMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopAgentMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.quitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenRmContextMenuStrip.SuspendLayout();
            // 
            // OpenRmNotifyIcon
            // 
            this.OpenRmNotifyIcon.ContextMenuStrip = this.OpenRmContextMenuStrip;
            this.OpenRmNotifyIcon.Text = "OpenRM";
            this.OpenRmNotifyIcon.Visible = true;
            // 
            // OpenRmContextMenuStrip
            // 
            this.OpenRmContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startAgentMenuItem,
            this.stopAgentMenuItem,
            this.toolStripSeparator1,
            this.settingsMenuItem,
            this.toolStripSeparator,
            this.quitMenuItem});
            this.OpenRmContextMenuStrip.Name = "OpenRmContextMenuStrip";
            this.OpenRmContextMenuStrip.Size = new System.Drawing.Size(184, 104);
            // 
            // startAgentMenuItem
            // 
            this.startAgentMenuItem.Name = "startAgentMenuItem";
            this.startAgentMenuItem.Size = new System.Drawing.Size(183, 22);
            this.startAgentMenuItem.Text = "Start OpenRm Agent";
            // 
            // stopAgentMenuItem
            // 
            this.stopAgentMenuItem.Enabled = false;
            this.stopAgentMenuItem.Name = "stopAgentMenuItem";
            this.stopAgentMenuItem.Size = new System.Drawing.Size(183, 22);
            this.stopAgentMenuItem.Text = "Stop OpenRm Agent";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(180, 6);
            // 
            // settingsMenuItem
            // 
            this.settingsMenuItem.Name = "settingsMenuItem";
            this.settingsMenuItem.Size = new System.Drawing.Size(183, 22);
            this.settingsMenuItem.Text = "Settings";
            // 
            // toolStripSeparator
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(180, 6);
            // 
            // quitMenuItem
            // 
            this.quitMenuItem.Name = "quitMenuItem";
            this.quitMenuItem.Size = new System.Drawing.Size(183, 22);
            this.quitMenuItem.Text = "Quit";
            this.OpenRmContextMenuStrip.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon OpenRmNotifyIcon;
        private System.Windows.Forms.ContextMenuStrip OpenRmContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem startAgentMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopAgentMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem settingsMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
        private System.Windows.Forms.ToolStripMenuItem quitMenuItem;
    }
}
