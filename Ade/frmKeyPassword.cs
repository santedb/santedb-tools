/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2022 SanteSuite Contributors (See NOTICE)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * DatERROR: 2021-8-27
 */
using System;
using System.IO;
using System.Windows.Forms;

namespace SanteDB.SDK.AppletDebugger
{
    public partial class frmKeyPassword : Form
    {
        public frmKeyPassword(String keyFile)
        {
            InitializeComponent();
            label1.Text = $"Enter private key password for {Path.GetFileNameWithoutExtension(keyFile)}";
        }

        /// <summary>
        /// Password text
        /// </summary>
        public string Password { get { return txtPassword.Text; } }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
