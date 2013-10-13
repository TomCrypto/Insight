namespace Sample
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.RenderPanel = new System.Windows.Forms.Panel();
            this.ControlPanel = new System.Windows.Forms.Panel();
            this.AnimationGroupBox = new System.Windows.Forms.GroupBox();
            this.AnimationList = new System.Windows.Forms.ComboBox();
            this.ApertureGroupBox = new System.Windows.Forms.GroupBox();
            this.SpectralTermsList = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SpectralMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.NewWavelengthMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.SepMenu = new System.Windows.Forms.ToolStripSeparator();
            this.RemoveWavelengthMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.SepMenu2 = new System.Windows.Forms.ToolStripSeparator();
            this.ResetWavelengthsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.InfoLabel3 = new System.Windows.Forms.Label();
            this.DistanceBar = new System.Windows.Forms.TrackBar();
            this.GeneralGroupBox = new System.Windows.Forms.GroupBox();
            this.InfoLabel2 = new System.Windows.Forms.Label();
            this.InfoLabel1 = new System.Windows.Forms.Label();
            this.SpeedBar = new System.Windows.Forms.TrackBar();
            this.ExposureBar = new System.Windows.Forms.TrackBar();
            this.ViewGroupBox = new System.Windows.Forms.GroupBox();
            this.ConvolvedFrameBtn = new System.Windows.Forms.RadioButton();
            this.FrameRadioBtn = new System.Windows.Forms.RadioButton();
            this.FilterRadioBtn = new System.Windows.Forms.RadioButton();
            this.ApertureRadioBtn = new System.Windows.Forms.RadioButton();
            this.LoadApertureBtn = new System.Windows.Forms.Button();
            this.OpenFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.ToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.RenderTimer = new System.Windows.Forms.Timer(this.components);
            this.ColorDialog = new System.Windows.Forms.ColorDialog();
            this.ControlPanel.SuspendLayout();
            this.AnimationGroupBox.SuspendLayout();
            this.ApertureGroupBox.SuspendLayout();
            this.SpectralMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DistanceBar)).BeginInit();
            this.GeneralGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ExposureBar)).BeginInit();
            this.ViewGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // RenderPanel
            // 
            this.RenderPanel.BackColor = System.Drawing.SystemColors.Control;
            this.RenderPanel.Location = new System.Drawing.Point(0, 0);
            this.RenderPanel.Name = "RenderPanel";
            this.RenderPanel.Size = new System.Drawing.Size(600, 600);
            this.RenderPanel.TabIndex = 0;
            this.RenderPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.RenderPanel_Paint);
            // 
            // ControlPanel
            // 
            this.ControlPanel.BackColor = System.Drawing.SystemColors.Control;
            this.ControlPanel.Controls.Add(this.AnimationGroupBox);
            this.ControlPanel.Controls.Add(this.ApertureGroupBox);
            this.ControlPanel.Controls.Add(this.GeneralGroupBox);
            this.ControlPanel.Controls.Add(this.ViewGroupBox);
            this.ControlPanel.Controls.Add(this.LoadApertureBtn);
            this.ControlPanel.Location = new System.Drawing.Point(600, 0);
            this.ControlPanel.Name = "ControlPanel";
            this.ControlPanel.Size = new System.Drawing.Size(200, 600);
            this.ControlPanel.TabIndex = 1;
            // 
            // AnimationGroupBox
            // 
            this.AnimationGroupBox.Controls.Add(this.AnimationList);
            this.AnimationGroupBox.Location = new System.Drawing.Point(3, 544);
            this.AnimationGroupBox.Name = "AnimationGroupBox";
            this.AnimationGroupBox.Size = new System.Drawing.Size(192, 50);
            this.AnimationGroupBox.TabIndex = 7;
            this.AnimationGroupBox.TabStop = false;
            this.AnimationGroupBox.Text = "Animation Selection";
            // 
            // AnimationList
            // 
            this.AnimationList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.AnimationList.FormattingEnabled = true;
            this.AnimationList.Items.AddRange(new object[] {
            "No movement",
            "Occlusion example",
            "Two lights interacting",
            "Stationary light source",
            "Spectral lighting example",
            "Multiple occlusion grating"});
            this.AnimationList.Location = new System.Drawing.Point(8, 20);
            this.AnimationList.Name = "AnimationList";
            this.AnimationList.Size = new System.Drawing.Size(176, 21);
            this.AnimationList.TabIndex = 0;
            this.ToolTip.SetToolTip(this.AnimationList, "Different animations to experiment with");
            this.AnimationList.SelectedIndexChanged += new System.EventHandler(this.AnimationList_SelectedIndexChanged);
            // 
            // ApertureGroupBox
            // 
            this.ApertureGroupBox.Controls.Add(this.SpectralTermsList);
            this.ApertureGroupBox.Controls.Add(this.InfoLabel3);
            this.ApertureGroupBox.Controls.Add(this.DistanceBar);
            this.ApertureGroupBox.Location = new System.Drawing.Point(3, 161);
            this.ApertureGroupBox.Name = "ApertureGroupBox";
            this.ApertureGroupBox.Size = new System.Drawing.Size(192, 266);
            this.ApertureGroupBox.TabIndex = 6;
            this.ApertureGroupBox.TabStop = false;
            this.ApertureGroupBox.Text = "Aperture Definition";
            // 
            // SpectralTermsList
            // 
            this.SpectralTermsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.SpectralTermsList.ContextMenuStrip = this.SpectralMenu;
            this.SpectralTermsList.FullRowSelect = true;
            this.SpectralTermsList.Location = new System.Drawing.Point(10, 24);
            this.SpectralTermsList.MultiSelect = false;
            this.SpectralTermsList.Name = "SpectralTermsList";
            this.SpectralTermsList.Size = new System.Drawing.Size(170, 186);
            this.SpectralTermsList.TabIndex = 8;
            this.ToolTip.SetToolTip(this.SpectralTermsList, "Wavelengths being diffracted");
            this.SpectralTermsList.UseCompatibleStateImageBehavior = false;
            this.SpectralTermsList.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Wavelength";
            this.columnHeader1.Width = 72;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Color";
            this.columnHeader2.Width = 77;
            // 
            // SpectralMenu
            // 
            this.SpectralMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewWavelengthMenu,
            this.SepMenu,
            this.RemoveWavelengthMenu,
            this.SepMenu2,
            this.ResetWavelengthsMenu});
            this.SpectralMenu.Name = "SpectralMenu";
            this.SpectralMenu.Size = new System.Drawing.Size(186, 82);
            this.SpectralMenu.Opening += new System.ComponentModel.CancelEventHandler(this.SpectralMenu_Opening);
            // 
            // NewWavelengthMenu
            // 
            this.NewWavelengthMenu.Name = "NewWavelengthMenu";
            this.NewWavelengthMenu.Size = new System.Drawing.Size(185, 22);
            this.NewWavelengthMenu.Text = "Add new wavelength";
            this.NewWavelengthMenu.Click += new System.EventHandler(this.NewWavelengthMenu_Click);
            // 
            // SepMenu
            // 
            this.SepMenu.Name = "SepMenu";
            this.SepMenu.Size = new System.Drawing.Size(182, 6);
            // 
            // RemoveWavelengthMenu
            // 
            this.RemoveWavelengthMenu.Name = "RemoveWavelengthMenu";
            this.RemoveWavelengthMenu.Size = new System.Drawing.Size(185, 22);
            this.RemoveWavelengthMenu.Text = "Remove selected";
            this.RemoveWavelengthMenu.Click += new System.EventHandler(this.RemoveWavelengthMenu_Click);
            // 
            // SepMenu2
            // 
            this.SepMenu2.Name = "SepMenu2";
            this.SepMenu2.Size = new System.Drawing.Size(182, 6);
            // 
            // ResetWavelengthsMenu
            // 
            this.ResetWavelengthsMenu.Name = "ResetWavelengthsMenu";
            this.ResetWavelengthsMenu.Size = new System.Drawing.Size(185, 22);
            this.ResetWavelengthsMenu.Text = "Reset to defaults";
            this.ResetWavelengthsMenu.Click += new System.EventHandler(this.ResetWavelengthsMenu_Click);
            // 
            // InfoLabel3
            // 
            this.InfoLabel3.AutoSize = true;
            this.InfoLabel3.Location = new System.Drawing.Point(40, 219);
            this.InfoLabel3.Name = "InfoLabel3";
            this.InfoLabel3.Size = new System.Drawing.Size(136, 13);
            this.InfoLabel3.TabIndex = 7;
            this.InfoLabel3.Text = "Observation plane distance";
            this.InfoLabel3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // DistanceBar
            // 
            this.DistanceBar.AutoSize = false;
            this.DistanceBar.Location = new System.Drawing.Point(10, 235);
            this.DistanceBar.Maximum = 1000;
            this.DistanceBar.Minimum = 500;
            this.DistanceBar.Name = "DistanceBar";
            this.DistanceBar.Size = new System.Drawing.Size(171, 23);
            this.DistanceBar.TabIndex = 6;
            this.DistanceBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.ToolTip.SetToolTip(this.DistanceBar, "Distance between the aperture and the observation plane");
            this.DistanceBar.Value = 500;
            this.DistanceBar.Scroll += new System.EventHandler(this.DistanceBar_Scroll);
            // 
            // GeneralGroupBox
            // 
            this.GeneralGroupBox.Controls.Add(this.InfoLabel2);
            this.GeneralGroupBox.Controls.Add(this.InfoLabel1);
            this.GeneralGroupBox.Controls.Add(this.SpeedBar);
            this.GeneralGroupBox.Controls.Add(this.ExposureBar);
            this.GeneralGroupBox.Location = new System.Drawing.Point(3, 433);
            this.GeneralGroupBox.Name = "GeneralGroupBox";
            this.GeneralGroupBox.Size = new System.Drawing.Size(192, 105);
            this.GeneralGroupBox.TabIndex = 5;
            this.GeneralGroupBox.TabStop = false;
            this.GeneralGroupBox.Text = "General Options";
            // 
            // InfoLabel2
            // 
            this.InfoLabel2.AutoSize = true;
            this.InfoLabel2.Location = new System.Drawing.Point(91, 58);
            this.InfoLabel2.Name = "InfoLabel2";
            this.InfoLabel2.Size = new System.Drawing.Size(85, 13);
            this.InfoLabel2.TabIndex = 3;
            this.InfoLabel2.Text = "Animation speed";
            this.InfoLabel2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // InfoLabel1
            // 
            this.InfoLabel1.AutoSize = true;
            this.InfoLabel1.Location = new System.Drawing.Point(100, 16);
            this.InfoLabel1.Name = "InfoLabel1";
            this.InfoLabel1.Size = new System.Drawing.Size(76, 13);
            this.InfoLabel1.TabIndex = 2;
            this.InfoLabel1.Text = "Exposure level";
            this.InfoLabel1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // SpeedBar
            // 
            this.SpeedBar.AutoSize = false;
            this.SpeedBar.Location = new System.Drawing.Point(10, 74);
            this.SpeedBar.Maximum = 100;
            this.SpeedBar.Name = "SpeedBar";
            this.SpeedBar.Size = new System.Drawing.Size(171, 23);
            this.SpeedBar.TabIndex = 1;
            this.SpeedBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.ToolTip.SetToolTip(this.SpeedBar, "Speed of the frame animation");
            this.SpeedBar.Scroll += new System.EventHandler(this.SpeedBar_Scroll);
            // 
            // ExposureBar
            // 
            this.ExposureBar.AutoSize = false;
            this.ExposureBar.Location = new System.Drawing.Point(10, 32);
            this.ExposureBar.Maximum = 2100;
            this.ExposureBar.Name = "ExposureBar";
            this.ExposureBar.Size = new System.Drawing.Size(172, 23);
            this.ExposureBar.TabIndex = 0;
            this.ExposureBar.TickFrequency = 0;
            this.ExposureBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.ToolTip.SetToolTip(this.ExposureBar, "HDR exposure (exponential scale)");
            this.ExposureBar.Scroll += new System.EventHandler(this.ExposureBar_Scroll);
            // 
            // ViewGroupBox
            // 
            this.ViewGroupBox.Controls.Add(this.ConvolvedFrameBtn);
            this.ViewGroupBox.Controls.Add(this.FrameRadioBtn);
            this.ViewGroupBox.Controls.Add(this.FilterRadioBtn);
            this.ViewGroupBox.Controls.Add(this.ApertureRadioBtn);
            this.ViewGroupBox.Location = new System.Drawing.Point(3, 44);
            this.ViewGroupBox.Name = "ViewGroupBox";
            this.ViewGroupBox.Size = new System.Drawing.Size(192, 112);
            this.ViewGroupBox.TabIndex = 3;
            this.ViewGroupBox.TabStop = false;
            this.ViewGroupBox.Text = "View";
            // 
            // ConvolvedFrameBtn
            // 
            this.ConvolvedFrameBtn.AutoSize = true;
            this.ConvolvedFrameBtn.Location = new System.Drawing.Point(10, 88);
            this.ConvolvedFrameBtn.Name = "ConvolvedFrameBtn";
            this.ConvolvedFrameBtn.Size = new System.Drawing.Size(108, 17);
            this.ConvolvedFrameBtn.TabIndex = 3;
            this.ConvolvedFrameBtn.TabStop = true;
            this.ConvolvedFrameBtn.Text = "Convolved Frame";
            this.ToolTip.SetToolTip(this.ConvolvedFrameBtn, "The frame, with diffraction effects");
            this.ConvolvedFrameBtn.UseVisualStyleBackColor = true;
            this.ConvolvedFrameBtn.CheckedChanged += new System.EventHandler(this.ConvolvedFrameBtn_CheckedChanged);
            // 
            // FrameRadioBtn
            // 
            this.FrameRadioBtn.AutoSize = true;
            this.FrameRadioBtn.Location = new System.Drawing.Point(10, 65);
            this.FrameRadioBtn.Name = "FrameRadioBtn";
            this.FrameRadioBtn.Size = new System.Drawing.Size(141, 17);
            this.FrameRadioBtn.TabIndex = 2;
            this.FrameRadioBtn.TabStop = true;
            this.FrameRadioBtn.Text = "Original Frame Animation";
            this.ToolTip.SetToolTip(this.FrameRadioBtn, "The frame, without diffraction effects");
            this.FrameRadioBtn.UseVisualStyleBackColor = true;
            this.FrameRadioBtn.CheckedChanged += new System.EventHandler(this.FrameRadioBtn_CheckedChanged);
            // 
            // FilterRadioBtn
            // 
            this.FilterRadioBtn.AutoSize = true;
            this.FilterRadioBtn.Location = new System.Drawing.Point(10, 42);
            this.FilterRadioBtn.Name = "FilterRadioBtn";
            this.FilterRadioBtn.Size = new System.Drawing.Size(149, 17);
            this.FilterRadioBtn.TabIndex = 1;
            this.FilterRadioBtn.TabStop = true;
            this.FilterRadioBtn.Text = "Aperture Convolution Filter";
            this.ToolTip.SetToolTip(this.FilterRadioBtn, "Distribution of diffracted light");
            this.FilterRadioBtn.UseVisualStyleBackColor = true;
            this.FilterRadioBtn.CheckedChanged += new System.EventHandler(this.FilterRadioBtn_CheckedChanged);
            // 
            // ApertureRadioBtn
            // 
            this.ApertureRadioBtn.AutoSize = true;
            this.ApertureRadioBtn.Checked = true;
            this.ApertureRadioBtn.Location = new System.Drawing.Point(10, 19);
            this.ApertureRadioBtn.Name = "ApertureRadioBtn";
            this.ApertureRadioBtn.Size = new System.Drawing.Size(173, 17);
            this.ApertureRadioBtn.TabIndex = 0;
            this.ApertureRadioBtn.TabStop = true;
            this.ApertureRadioBtn.Text = "Aperture Transmission Function";
            this.ToolTip.SetToolTip(this.ApertureRadioBtn, "Transmittance at each pixel");
            this.ApertureRadioBtn.UseVisualStyleBackColor = true;
            this.ApertureRadioBtn.CheckedChanged += new System.EventHandler(this.ApertureRadioBtn_CheckedChanged);
            // 
            // LoadApertureBtn
            // 
            this.LoadApertureBtn.Location = new System.Drawing.Point(3, 3);
            this.LoadApertureBtn.Name = "LoadApertureBtn";
            this.LoadApertureBtn.Size = new System.Drawing.Size(194, 35);
            this.LoadApertureBtn.TabIndex = 0;
            this.LoadApertureBtn.Text = "Load Aperture";
            this.ToolTip.SetToolTip(this.LoadApertureBtn, "Select a new aperture (as an image file)");
            this.LoadApertureBtn.UseVisualStyleBackColor = true;
            this.LoadApertureBtn.Click += new System.EventHandler(this.LoadApertureBtn_Click);
            // 
            // OpenFileDlg
            // 
            this.OpenFileDlg.DefaultExt = "png";
            this.OpenFileDlg.Filter = "PNG (Portable Network Graphics)|*.png|All files|*.*";
            this.OpenFileDlg.Title = "Load Aperture";
            // 
            // RenderTimer
            // 
            this.RenderTimer.Enabled = true;
            this.RenderTimer.Interval = 1;
            this.RenderTimer.Tick += new System.EventHandler(this.RenderTimer_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.ControlPanel);
            this.Controls.Add(this.RenderPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Fraunhofer Diffraction Simulation";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.ControlPanel.ResumeLayout(false);
            this.AnimationGroupBox.ResumeLayout(false);
            this.ApertureGroupBox.ResumeLayout(false);
            this.ApertureGroupBox.PerformLayout();
            this.SpectralMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.DistanceBar)).EndInit();
            this.GeneralGroupBox.ResumeLayout(false);
            this.GeneralGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ExposureBar)).EndInit();
            this.ViewGroupBox.ResumeLayout(false);
            this.ViewGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel RenderPanel;
        private System.Windows.Forms.Panel ControlPanel;
        private System.Windows.Forms.Button LoadApertureBtn;
        private System.Windows.Forms.OpenFileDialog OpenFileDlg;
        private System.Windows.Forms.GroupBox ViewGroupBox;
        private System.Windows.Forms.RadioButton ApertureRadioBtn;
        private System.Windows.Forms.RadioButton FilterRadioBtn;
        private System.Windows.Forms.ToolTip ToolTip;
        private System.Windows.Forms.Timer RenderTimer;
        private System.Windows.Forms.GroupBox GeneralGroupBox;
        private System.Windows.Forms.TrackBar ExposureBar;
        private System.Windows.Forms.RadioButton FrameRadioBtn;
        private System.Windows.Forms.TrackBar SpeedBar;
        private System.Windows.Forms.RadioButton ConvolvedFrameBtn;
        private System.Windows.Forms.Label InfoLabel1;
        private System.Windows.Forms.Label InfoLabel2;
        private System.Windows.Forms.GroupBox ApertureGroupBox;
        private System.Windows.Forms.GroupBox AnimationGroupBox;
        private System.Windows.Forms.ComboBox AnimationList;
        private System.Windows.Forms.Label InfoLabel3;
        private System.Windows.Forms.TrackBar DistanceBar;
        private System.Windows.Forms.ListView SpectralTermsList;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ContextMenuStrip SpectralMenu;
        private System.Windows.Forms.ToolStripMenuItem NewWavelengthMenu;
        private System.Windows.Forms.ToolStripSeparator SepMenu;
        private System.Windows.Forms.ToolStripMenuItem RemoveWavelengthMenu;
        private System.Windows.Forms.ColorDialog ColorDialog;
        private System.Windows.Forms.ToolStripSeparator SepMenu2;
        private System.Windows.Forms.ToolStripMenuItem ResetWavelengthsMenu;
    }
}

