namespace hlsmx
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) { components.Dispose(); }
            base.Dispose(disposing);
        }

private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.menuSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOptions = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOpenLogs = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.tabs = new hlsmx.ThemedTabControl();
            this.tabServers = new System.Windows.Forms.TabPage();
            this.serverList = new System.Windows.Forms.ListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colIp = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPort = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colExe = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPriority = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colProcess = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colNetwork = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colMap = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPlayers = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colRestarts = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPid = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colWindow = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colConsole = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colCpu = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colParams = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUptime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colLastRestart = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.headerMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tabDisabled = new System.Windows.Forms.TabPage();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.logBox = new System.Windows.Forms.TextBox();
            this.serverMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuPauseMonitoring = new System.Windows.Forms.ToolStripMenuItem();
            this.menuResetRestartCount = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDisableServer = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuEditServer = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSchedules = new System.Windows.Forms.ToolStripMenuItem();
            this.menuNewServer = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDuplicateServer = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDeleteServer = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.menuMoveUp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuMoveDown = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuShowServer = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHideServer = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.menuRestartServer = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCloseServer = new System.Windows.Forms.ToolStripMenuItem();
            this.checkTimer = new System.Windows.Forms.Timer(this.components);
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuTrayShow = new System.Windows.Forms.ToolStripMenuItem();
            this.menuTrayExit = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenu.SuspendLayout();
            this.tabs.SuspendLayout();
            this.tabServers.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.serverMenu.SuspendLayout();
            this.trayMenu.SuspendLayout();
            this.SuspendLayout();
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuSettings,
            this.menuHelp});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.Size = new System.Drawing.Size(1008, 25);
            this.mainMenu.TabIndex = 0;
            this.mainMenu.Text = "mainMenu";
            this.menuSettings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuOpenLogs,
            this.menuOptions,
            this.menuSeparator4,
            this.menuExit});
            this.menuSettings.Name = "menuSettings";
            this.menuSettings.Size = new System.Drawing.Size(59, 21);
            this.menuSettings.Text = "HLS&MX";
            this.menuOptions.Name = "menuOptions";
            this.menuOptions.Size = new System.Drawing.Size(200, 22);
            this.menuOptions.Text = "&Options";
            this.menuOptions.Click += new System.EventHandler(this.menuOptions_Click);
            this.menuOpenLogs.Name = "menuOpenLogs";
            this.menuOpenLogs.Size = new System.Drawing.Size(200, 22);
            this.menuOpenLogs.Text = "Open &Logs";
            this.menuOpenLogs.Click += new System.EventHandler(this.menuOpenLogs_Click);
            this.menuSeparator4.Name = "menuSeparator4";
            this.menuSeparator4.Size = new System.Drawing.Size(197, 6);
            this.menuExit.Name = "menuExit";
            this.menuExit.Size = new System.Drawing.Size(200, 22);
            this.menuExit.Text = "E&xit";
            this.menuExit.Click += new System.EventHandler(this.menuExit_Click);
            this.menuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAbout});
            this.menuHelp.Name = "menuHelp";
            this.menuHelp.Size = new System.Drawing.Size(61, 21);
            this.menuHelp.Text = "&Help";
            this.menuAbout.Name = "menuAbout";
            this.menuAbout.Size = new System.Drawing.Size(153, 22);
            this.menuAbout.Text = "&About...";
            this.menuAbout.Click += new System.EventHandler(this.menuAbout_Click);
            this.tabs.Controls.Add(this.tabServers);
            this.tabs.Controls.Add(this.tabDisabled);
            this.tabs.Controls.Add(this.tabLog);
            this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabs.Location = new System.Drawing.Point(0, 25);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(1008, 296);
            this.tabs.TabIndex = 1;
            this.tabs.SelectedIndexChanged += new System.EventHandler(this.tabs_SelectedIndexChanged);
            this.tabServers.Controls.Add(this.serverList);
            this.tabServers.Location = new System.Drawing.Point(4, 22);
            this.tabServers.Name = "tabServers";
            this.tabServers.Padding = new System.Windows.Forms.Padding(3);
            this.tabServers.Size = new System.Drawing.Size(1000, 270);
            this.tabServers.TabIndex = 0;
            this.tabServers.Text = "Servers";
            this.tabServers.UseVisualStyleBackColor = true;
            this.serverList.AllowColumnReorder = true;
            this.serverList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colIp,
            this.colPort,
            this.colExe,
            this.colPriority,
            this.colProcess,
            this.colNetwork,
            this.colMap,
            this.colPlayers,
            this.colRestarts,
            this.colPid,
            this.colWindow,
            this.colConsole,
            this.colCpu,
            this.colParams,
            this.colUptime,
            this.colLastRestart});
            this.serverList.ContextMenuStrip = this.headerMenu;
            this.serverList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.serverList.FullRowSelect = true;
            this.serverList.HideSelection = false;
            this.serverList.Location = new System.Drawing.Point(3, 3);
            this.serverList.Name = "serverList";
            this.serverList.Size = new System.Drawing.Size(994, 264);
            this.serverList.TabIndex = 1;
            this.serverList.UseCompatibleStateImageBehavior = false;
            this.serverList.View = System.Windows.Forms.View.Details;
            this.serverList.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.serverList_ColumnClick);
            this.serverList.ColumnWidthChanged += new System.Windows.Forms.ColumnWidthChangedEventHandler(this.serverList_ColumnWidthChanged);
            this.serverList.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.serverList_ColumnWidthChanging);
            this.serverList.DoubleClick += new System.EventHandler(this.serverList_DoubleClick);
            this.serverList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.serverList_KeyDown);
            this.serverList.MouseUp += new System.Windows.Forms.MouseEventHandler(this.serverList_MouseUp);
            this.colName.Text = "Name";
            this.colName.Width = 90;
            this.colIp.Text = "IP";
            this.colIp.Width = 100;
            this.colPort.Text = "Port";
            this.colPort.Width = 50;
            this.colExe.Text = "Executable";
            this.colExe.Width = 220;
            this.colPriority.Text = "Priority";
            this.colPriority.Width = 70;
            this.colProcess.Text = "Process";
            this.colProcess.Width = 90;
            this.colNetwork.Text = "Network";
            this.colNetwork.Width = 90;
            this.colMap.Text = "Map";
            this.colMap.Width = 110;
            this.colPlayers.Text = "Players";
            this.colPlayers.Width = 60;
            this.colRestarts.Text = "Restarts";
            this.colRestarts.Width = 60;
            this.colPid.Text = "PID";
            this.colPid.Width = 60;
            this.colWindow.Text = "Window Handle";
            this.colWindow.Width = 90;
            this.colConsole.Text = "Console";
            this.colConsole.Width = 60;
            this.colCpu.Text = "CPU";
            this.colCpu.Width = 90;
            this.colParams.Text = "Launch Params";
            this.colParams.Width = 200;
            this.colUptime.Text = "Uptime";
            this.colUptime.Width = 70;
            this.colLastRestart.Text = "Last Restart";
            this.colLastRestart.Width = 110;
            this.headerMenu.Name = "headerMenu";
            this.headerMenu.Size = new System.Drawing.Size(61, 4);
            this.headerMenu.Opening += new System.ComponentModel.CancelEventHandler(this.headerMenu_Opening);
            this.tabDisabled.Location = new System.Drawing.Point(4, 22);
            this.tabDisabled.Name = "tabDisabled";
            this.tabDisabled.Padding = new System.Windows.Forms.Padding(3);
            this.tabDisabled.Size = new System.Drawing.Size(1000, 270);
            this.tabDisabled.TabIndex = 1;
            this.tabDisabled.Text = "Inactive";
            this.tabDisabled.UseVisualStyleBackColor = true;
            this.tabLog.Controls.Add(this.logBox);
            this.tabLog.Location = new System.Drawing.Point(4, 22);
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Size = new System.Drawing.Size(1000, 270);
            this.tabLog.TabIndex = 2;
            this.tabLog.Text = "Log";
            this.tabLog.UseVisualStyleBackColor = true;
            this.logBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logBox.Location = new System.Drawing.Point(3, 3);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logBox.Size = new System.Drawing.Size(994, 264);
            this.logBox.TabIndex = 1;
            this.serverMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuPauseMonitoring,
            this.menuResetRestartCount,
            this.menuDisableServer,
            this.menuSeparator1,
            this.menuEditServer,
            this.menuSchedules,
            this.menuNewServer,
            this.menuDuplicateServer,
            this.menuDeleteServer,
            this.menuSeparator5,
            this.menuMoveUp,
            this.menuMoveDown,
            this.menuSeparator2,
            this.menuShowServer,
            this.menuHideServer,
            this.menuSeparator3,
            this.menuRestartServer,
            this.menuCloseServer});
            this.serverMenu.Name = "serverMenu";
            this.serverMenu.Size = new System.Drawing.Size(220, 320);
            this.menuPauseMonitoring.Name = "menuPauseMonitoring";
            this.menuPauseMonitoring.ShortcutKeyDisplayString = "Space";
            this.menuPauseMonitoring.Size = new System.Drawing.Size(219, 22);
            this.menuPauseMonitoring.Text = "&Pause Monitoring";
            this.menuPauseMonitoring.Click += new System.EventHandler(this.menuPauseMonitoring_Click);
            this.menuResetRestartCount.Name = "menuResetRestartCount";
            this.menuResetRestartCount.Size = new System.Drawing.Size(219, 22);
            this.menuResetRestartCount.Text = "&Zero Restart Count";
            this.menuResetRestartCount.Click += new System.EventHandler(this.menuResetRestartCount_Click);
            this.menuDisableServer.Name = "menuDisableServer";
            this.menuDisableServer.Size = new System.Drawing.Size(219, 22);
            this.menuDisableServer.Text = "Deac&tivate";
            this.menuDisableServer.Click += new System.EventHandler(this.menuDisableServer_Click);
            this.menuSeparator1.Name = "menuSeparator1";
            this.menuSeparator1.Size = new System.Drawing.Size(216, 6);
            this.menuEditServer.Name = "menuEditServer";
            this.menuEditServer.ShortcutKeyDisplayString = "Enter";
            this.menuEditServer.Size = new System.Drawing.Size(219, 22);
            this.menuEditServer.Text = "&Edit Server";
            this.menuEditServer.Click += new System.EventHandler(this.menuEditServer_Click);
            this.menuSchedules.Name = "menuSchedules";
            this.menuSchedules.Size = new System.Drawing.Size(219, 22);
            this.menuSchedules.Text = "Sche&dules";
            this.menuSchedules.Click += new System.EventHandler(this.menuSchedules_Click);
            this.menuNewServer.Name = "menuNewServer";
            this.menuNewServer.Size = new System.Drawing.Size(219, 22);
            this.menuNewServer.Text = "&New Server";
            this.menuNewServer.Click += new System.EventHandler(this.menuNewServer_Click);
            this.menuDuplicateServer.Name = "menuDuplicateServer";
            this.menuDuplicateServer.ShortcutKeyDisplayString = "Ctrl+D";
            this.menuDuplicateServer.Size = new System.Drawing.Size(219, 22);
            this.menuDuplicateServer.Text = "D&uplicate Server";
            this.menuDuplicateServer.Click += new System.EventHandler(this.menuDuplicateServer_Click);
            this.menuDeleteServer.Name = "menuDeleteServer";
            this.menuDeleteServer.ShortcutKeyDisplayString = "Del";
            this.menuDeleteServer.Size = new System.Drawing.Size(219, 22);
            this.menuDeleteServer.Text = "&Delete Server";
            this.menuDeleteServer.Click += new System.EventHandler(this.menuDeleteServer_Click);
            this.menuSeparator5.Name = "menuSeparator5";
            this.menuSeparator5.Size = new System.Drawing.Size(216, 6);
            this.menuMoveUp.Name = "menuMoveUp";
            this.menuMoveUp.ShortcutKeyDisplayString = "Ctrl+Up";
            this.menuMoveUp.Size = new System.Drawing.Size(219, 22);
            this.menuMoveUp.Text = "Move &Up";
            this.menuMoveUp.Click += new System.EventHandler(this.menuMoveUp_Click);
            this.menuMoveDown.Name = "menuMoveDown";
            this.menuMoveDown.ShortcutKeyDisplayString = "Ctrl+Down";
            this.menuMoveDown.Size = new System.Drawing.Size(219, 22);
            this.menuMoveDown.Text = "Move Do&wn";
            this.menuMoveDown.Click += new System.EventHandler(this.menuMoveDown_Click);
            this.menuSeparator2.Name = "menuSeparator2";
            this.menuSeparator2.Size = new System.Drawing.Size(216, 6);
            this.menuShowServer.Name = "menuShowServer";
            this.menuShowServer.Size = new System.Drawing.Size(219, 22);
            this.menuShowServer.Text = "&Show Console";
            this.menuShowServer.Click += new System.EventHandler(this.menuShowServer_Click);
            this.menuHideServer.Name = "menuHideServer";
            this.menuHideServer.Size = new System.Drawing.Size(219, 22);
            this.menuHideServer.Text = "&Hide Console";
            this.menuHideServer.Click += new System.EventHandler(this.menuHideServer_Click);
            this.menuSeparator3.Name = "menuSeparator3";
            this.menuSeparator3.Size = new System.Drawing.Size(216, 6);
            this.menuRestartServer.Name = "menuRestartServer";
            this.menuRestartServer.Size = new System.Drawing.Size(219, 22);
            this.menuRestartServer.Text = "&Restart Server";
            this.menuRestartServer.Click += new System.EventHandler(this.menuRestartServer_Click);
            this.menuCloseServer.Name = "menuCloseServer";
            this.menuCloseServer.Size = new System.Drawing.Size(219, 22);
            this.menuCloseServer.Text = "&Close Server";
            this.menuCloseServer.Click += new System.EventHandler(this.menuCloseServer_Click);
            this.checkTimer.Interval = 4000;
            this.checkTimer.Tick += new System.EventHandler(this.checkTimer_Tick);
            this.trayIcon.ContextMenuStrip = this.trayMenu;
            this.trayIcon.Text = "HLSMX";
            this.trayIcon.Visible = true;
            this.trayIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.trayIcon_MouseClick);
            this.trayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.trayIcon_MouseClick);
            this.trayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuTrayShow,
            this.menuTrayExit});
            this.trayMenu.Name = "trayMenu";
            this.trayMenu.Size = new System.Drawing.Size(108, 48);
            this.menuTrayShow.Name = "menuTrayShow";
            this.menuTrayShow.Size = new System.Drawing.Size(107, 22);
            this.menuTrayShow.Text = "&Show";
            this.menuTrayShow.Click += new System.EventHandler(this.menuTrayShow_Click);
            this.menuTrayExit.Name = "menuTrayExit";
            this.menuTrayExit.Size = new System.Drawing.Size(107, 22);
            this.menuTrayExit.Text = "E&xit";
            this.menuTrayExit.Click += new System.EventHandler(this.menuExit_Click);
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 321);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.mainMenu);
            this.MainMenuStrip = this.mainMenu;
            this.Name = "MainForm";
            this.Text = "Half-Life Server Monitor X";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.tabs.ResumeLayout(false);
            this.tabServers.ResumeLayout(false);
            this.tabLog.ResumeLayout(false);
            this.tabLog.PerformLayout();
            this.serverMenu.ResumeLayout(false);
            this.trayMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem menuSettings;
        private System.Windows.Forms.ToolStripMenuItem menuOptions;
        private System.Windows.Forms.ToolStripMenuItem menuOpenLogs;
        private System.Windows.Forms.ToolStripSeparator menuSeparator4;
        private System.Windows.Forms.ToolStripMenuItem menuExit;
        private System.Windows.Forms.ToolStripMenuItem menuHelp;
        private System.Windows.Forms.ToolStripMenuItem menuAbout;
        private hlsmx.ThemedTabControl tabs;
        private System.Windows.Forms.TabPage tabServers;
        private System.Windows.Forms.TabPage tabDisabled;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.ListView serverList;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colIp;
        private System.Windows.Forms.ColumnHeader colPort;
        private System.Windows.Forms.ColumnHeader colExe;
        private System.Windows.Forms.ColumnHeader colPriority;
        private System.Windows.Forms.ColumnHeader colProcess;
        private System.Windows.Forms.ColumnHeader colNetwork;
        private System.Windows.Forms.ColumnHeader colMap;
        private System.Windows.Forms.ColumnHeader colPlayers;
        private System.Windows.Forms.ColumnHeader colRestarts;
        private System.Windows.Forms.ColumnHeader colPid;
        private System.Windows.Forms.ColumnHeader colWindow;
        private System.Windows.Forms.ColumnHeader colConsole;
        private System.Windows.Forms.ColumnHeader colCpu;
        private System.Windows.Forms.ColumnHeader colParams;
        private System.Windows.Forms.ColumnHeader colUptime;
        private System.Windows.Forms.ColumnHeader colLastRestart;
        private System.Windows.Forms.ContextMenuStrip serverMenu;
        private System.Windows.Forms.ToolStripMenuItem menuPauseMonitoring;
        private System.Windows.Forms.ToolStripMenuItem menuResetRestartCount;
        private System.Windows.Forms.ToolStripMenuItem menuDisableServer;
        private System.Windows.Forms.ToolStripSeparator menuSeparator1;
        private System.Windows.Forms.ToolStripMenuItem menuEditServer;
        private System.Windows.Forms.ToolStripMenuItem menuSchedules;
        private System.Windows.Forms.ToolStripMenuItem menuNewServer;
        private System.Windows.Forms.ToolStripMenuItem menuDuplicateServer;
        private System.Windows.Forms.ToolStripMenuItem menuDeleteServer;
        private System.Windows.Forms.ToolStripSeparator menuSeparator5;
        private System.Windows.Forms.ToolStripMenuItem menuMoveUp;
        private System.Windows.Forms.ToolStripMenuItem menuMoveDown;
        private System.Windows.Forms.ToolStripSeparator menuSeparator2;
        private System.Windows.Forms.ToolStripMenuItem menuShowServer;
        private System.Windows.Forms.ToolStripMenuItem menuHideServer;
        private System.Windows.Forms.ToolStripSeparator menuSeparator3;
        private System.Windows.Forms.ToolStripMenuItem menuRestartServer;
        private System.Windows.Forms.ToolStripMenuItem menuCloseServer;
        private System.Windows.Forms.ContextMenuStrip headerMenu;
        private System.Windows.Forms.Timer checkTimer;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;
        private System.Windows.Forms.ToolStripMenuItem menuTrayShow;
        private System.Windows.Forms.ToolStripMenuItem menuTrayExit;
    }
}
