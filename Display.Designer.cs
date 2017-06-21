namespace DictionaryServer
{
    partial class Display
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
            this.Display1 = new System.Windows.Forms.RichTextBox();
            this.Display2 = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // Display1
            // 
            this.Display1.BackColor = System.Drawing.Color.Black;
            this.Display1.DetectUrls = false;
            this.Display1.Font = new System.Drawing.Font("Courier New", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Display1.ForeColor = System.Drawing.SystemColors.Window;
            this.Display1.Location = new System.Drawing.Point(139, 43);
            this.Display1.Name = "Display1";
            this.Display1.ReadOnly = true;
            this.Display1.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.Display1.Size = new System.Drawing.Size(420, 143);
            this.Display1.TabIndex = 0;
            this.Display1.Text = "";
            this.Display1.WordWrap = false;
            // 
            // Display2
            // 
            this.Display2.BackColor = System.Drawing.Color.Black;
            this.Display2.Font = new System.Drawing.Font("Consolas", 14F);
            this.Display2.ForeColor = System.Drawing.SystemColors.Window;
            this.Display2.Location = new System.Drawing.Point(161, 239);
            this.Display2.Name = "Display2";
            this.Display2.ReadOnly = true;
            this.Display2.Size = new System.Drawing.Size(383, 220);
            this.Display2.TabIndex = 1;
            this.Display2.Text = "";
            this.Display2.WordWrap = false;
            // 
            // Display
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(734, 701);
            this.Controls.Add(this.Display2);
            this.Controls.Add(this.Display1);
            this.Name = "Display";
            this.Text = "Display";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Display_FormClosing);
            this.Resize += new System.EventHandler(this.Display_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.RichTextBox Display1;
        public System.Windows.Forms.RichTextBox Display2;
    }
}