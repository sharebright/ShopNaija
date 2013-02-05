namespace HandleCipher
{
    partial class Form1
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
            this.TextToDecrypt = new System.Windows.Forms.TextBox();
            this.BitLyResult = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // TextToDecrypt
            // 
            this.TextToDecrypt.Location = new System.Drawing.Point(4, 2);
            this.TextToDecrypt.Name = "TextToDecrypt";
            this.TextToDecrypt.Size = new System.Drawing.Size(256, 20);
            this.TextToDecrypt.TabIndex = 1;
            this.TextToDecrypt.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.EnterKeyPress);
            // 
            // BitLyResult
            // 
            this.BitLyResult.Location = new System.Drawing.Point(4, 24);
            this.BitLyResult.Name = "BitLyResult";
            this.BitLyResult.ReadOnly = true;
            this.BitLyResult.Size = new System.Drawing.Size(256, 20);
            this.BitLyResult.TabIndex = 3;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(265, 50);
            this.Controls.Add(this.BitLyResult);
            this.Controls.Add(this.TextToDecrypt);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Location = new System.Drawing.Point(1090, 670);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(265, 50);
            this.MinimumSize = new System.Drawing.Size(265, 50);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Product Identifier";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TextToDecrypt;
        private System.Windows.Forms.TextBox BitLyResult;
    }
}

