using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KeyPassSystray {
    public partial class editorForm : Form {

        public delegate void ItemEdited(Entry entry);
        public event ItemEdited OnItemEdit;

        public string Key;

        public editorForm() {
            InitializeComponent();
        }

        public void FillFormFields(Entry data) {
            categoryTextBox.Text = data.Category;
            titleTextBox.Text = data.Title;
            userNameTextBox.Text = data.UserName;
            passwordTextBox.Text = data.Password;
            urlTextBox.Text = data.Url;
            Key = data.Key;
        }

        private void saveButton_Click(object sender, EventArgs e) {
            OnItemEdit(
              new Entry {
                  Category = categoryTextBox.Text,
                  Title = titleTextBox.Text,
                  UserName = userNameTextBox.Text,
                  Password = passwordTextBox.Text,
                  Url = urlTextBox.Text,
                  Key = Key
              }
            );
        }
    }
}
