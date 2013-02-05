using System;
using System.Diagnostics;
using System.Windows.Forms;
using ShopifyHandle;

namespace HandleCipher
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Decrypt_Click(object sender, EventArgs e)
        {
            var locator = HandleManager.Decrypt(TextToDecrypt.Text);
            BitLyResult.Text = locator;
            Process.Start(new ProcessStartInfo("chrome.exe", locator));
        }

        private void EnterKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                Decrypt_Click(default(object), default(EventArgs));
            }
        }
    }
}
