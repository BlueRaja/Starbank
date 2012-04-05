namespace StarBank
{
    partial class PlayerNumberForm
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
            if(disposing && (components != null))
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
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lblPlayerNumber = new System.Windows.Forms.Label();
            this.txtPlayerNumber = new System.Windows.Forms.TextBox();
            this.txtAuthorNumber = new System.Windows.Forms.TextBox();
            this.lblAuthorNumber = new System.Windows.Forms.Label();
            this.lblError = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(466, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "Please enter the player and author numbers for this bank file";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::StarBank.Properties.Resources.PlayerNumber;
            this.pictureBox1.Location = new System.Drawing.Point(16, 34);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(595, 159);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(13, 205);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(561, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "Note that the file-name must also be the same as the original in order for the si" +
                "gnature to match";
            // 
            // lblPlayerNumber
            // 
            this.lblPlayerNumber.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPlayerNumber.Location = new System.Drawing.Point(67, 234);
            this.lblPlayerNumber.Name = "lblPlayerNumber";
            this.lblPlayerNumber.Size = new System.Drawing.Size(136, 17);
            this.lblPlayerNumber.TabIndex = 3;
            this.lblPlayerNumber.Text = "Player Number";
            this.lblPlayerNumber.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPlayerNumber
            // 
            this.txtPlayerNumber.Location = new System.Drawing.Point(209, 234);
            this.txtPlayerNumber.Name = "txtPlayerNumber";
            this.txtPlayerNumber.Size = new System.Drawing.Size(178, 20);
            this.txtPlayerNumber.TabIndex = 4;
            this.txtPlayerNumber.Text = "1-S2-1-";
            this.txtPlayerNumber.TextChanged += new System.EventHandler(this.ValidateTextChanged);
            // 
            // txtAuthorNumber
            // 
            this.txtAuthorNumber.Location = new System.Drawing.Point(209, 261);
            this.txtAuthorNumber.Name = "txtAuthorNumber";
            this.txtAuthorNumber.Size = new System.Drawing.Size(178, 20);
            this.txtAuthorNumber.TabIndex = 5;
            this.txtAuthorNumber.Text = "1-S2-1-";
            this.txtAuthorNumber.TextChanged += new System.EventHandler(this.ValidateTextChanged);
            // 
            // lblAuthorNumber
            // 
            this.lblAuthorNumber.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAuthorNumber.Location = new System.Drawing.Point(70, 261);
            this.lblAuthorNumber.Name = "lblAuthorNumber";
            this.lblAuthorNumber.Size = new System.Drawing.Size(133, 20);
            this.lblAuthorNumber.TabIndex = 6;
            this.lblAuthorNumber.Text = "Author Number";
            this.lblAuthorNumber.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblError
            // 
            this.lblError.AutoSize = true;
            this.lblError.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblError.ForeColor = System.Drawing.Color.Red;
            this.lblError.Location = new System.Drawing.Point(106, 290);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(161, 16);
            this.lblError.TabIndex = 7;
            this.lblError.Text = "Invalid player number!";
            this.lblError.Visible = false;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(536, 293);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Enabled = false;
            this.btnOk.Location = new System.Drawing.Point(455, 293);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 9;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // PlayerNumberForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(624, 323);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.lblAuthorNumber);
            this.Controls.Add(this.txtAuthorNumber);
            this.Controls.Add(this.txtPlayerNumber);
            this.Controls.Add(this.lblPlayerNumber);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PlayerNumberForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Signature information";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblPlayerNumber;
        private System.Windows.Forms.TextBox txtPlayerNumber;
        private System.Windows.Forms.TextBox txtAuthorNumber;
        private System.Windows.Forms.Label lblAuthorNumber;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
    }
}