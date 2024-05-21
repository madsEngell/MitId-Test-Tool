namespace MitIdAuthenticatorApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.individDropDown = new System.Windows.Forms.ComboBox();
            this.idInput = new System.Windows.Forms.TextBox();
            this.consoleOutput = new System.Windows.Forms.TextBox();
            this.sendApprovalButton = new System.Windows.Forms.Button();
            this.sendAllApprovalsButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.addIdButtons = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // individDropDown
            // 
            this.individDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.individDropDown.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.individDropDown.FormattingEnabled = true;
            this.individDropDown.ItemHeight = 32;
            this.individDropDown.Location = new System.Drawing.Point(12, 12);
            this.individDropDown.Name = "individDropDown";
            this.individDropDown.Size = new System.Drawing.Size(260, 40);
            this.individDropDown.TabIndex = 0;
            this.individDropDown.SelectedIndexChanged += new System.EventHandler(this.individDropDown_SelectedIndexChanged);
            // 
            // idInput
            // 
            this.idInput.Location = new System.Drawing.Point(6, 22);
            this.idInput.Name = "idInput";
            this.idInput.Size = new System.Drawing.Size(248, 23);
            this.idInput.TabIndex = 3;
            this.idInput.TextChanged += new System.EventHandler(this.idInput_TextChanged);
            this.idInput.KeyUp += new System.Windows.Forms.KeyEventHandler(this.idInput_KeyUp);
            // 
            // consoleOutput
            // 
            this.consoleOutput.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.consoleOutput.Location = new System.Drawing.Point(12, 266);
            this.consoleOutput.Multiline = true;
            this.consoleOutput.Name = "consoleOutput";
            this.consoleOutput.ReadOnly = true;
            this.consoleOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.consoleOutput.Size = new System.Drawing.Size(260, 163);
            this.consoleOutput.TabIndex = 2;
            this.consoleOutput.TabStop = false;
            this.consoleOutput.TextChanged += new System.EventHandler(this.consoleOutput_TextChanged);
            // 
            // sendApprovalButton
            // 
            this.sendApprovalButton.AccessibleName = "Hover";
            this.sendApprovalButton.Location = new System.Drawing.Point(12, 58);
            this.sendApprovalButton.Name = "sendApprovalButton";
            this.sendApprovalButton.Size = new System.Drawing.Size(260, 45);
            this.sendApprovalButton.TabIndex = 1;
            this.sendApprovalButton.Text = "Godkend (space)";
            this.sendApprovalButton.UseVisualStyleBackColor = true;
            this.sendApprovalButton.Click += new System.EventHandler(this.sendApprovalButton_Click);
            // 
            // sendAllApprovalsButton
            // 
            this.sendAllApprovalsButton.Location = new System.Drawing.Point(12, 109);
            this.sendAllApprovalsButton.Name = "sendAllApprovalsButton";
            this.sendAllApprovalsButton.Size = new System.Drawing.Size(260, 45);
            this.sendAllApprovalsButton.TabIndex = 2;
            this.sendAllApprovalsButton.Text = "Godkend &alle (½)";
            this.sendAllApprovalsButton.UseVisualStyleBackColor = true;
            this.sendAllApprovalsButton.Click += new System.EventHandler(this.sendAllApprovalsButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.addIdButtons);
            this.groupBox1.Controls.Add(this.idInput);
            this.groupBox1.Location = new System.Drawing.Point(12, 160);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(260, 100);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Tilføj eller opret MitID bruger";
            // 
            // addIdButtons
            // 
            this.addIdButtons.Enabled = false;
            this.addIdButtons.Location = new System.Drawing.Point(6, 51);
            this.addIdButtons.Name = "addIdButtons";
            this.addIdButtons.Size = new System.Drawing.Size(248, 43);
            this.addIdButtons.TabIndex = 4;
            this.addIdButtons.Text = "Tilføj (enter)";
            this.addIdButtons.UseVisualStyleBackColor = true;
            this.addIdButtons.Click += new System.EventHandler(this.addIdButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 441);
            this.Controls.Add(this.individDropDown);
            this.Controls.Add(this.sendApprovalButton);
            this.Controls.Add(this.sendAllApprovalsButton);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.consoleOutput);
            this.KeyPreview = true;
            this.Name = "Form1";
            this.Text = "MitID Authenticator app";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ComboBox individDropDown;
        private TextBox idInput;
        private TextBox consoleOutput;
        private Button sendApprovalButton;
        private Button sendAllApprovalsButton;
        private GroupBox groupBox1;
        private Button addIdButtons;
    }
}