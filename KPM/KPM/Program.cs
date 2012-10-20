/*
 * Creates a systray icon with a menu of all groups and items in keepass db
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using KeePass.DataExchange;
using System.Net;
using System.Text;

namespace KeyPassSystray {

    public class SysTrayAccess : Form {

        //todo: add auto menu load when db changes

        const string MenuSeparator = "-";

        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;

        Dictionary<string, string> localPwdHash;
        Dictionary<string, string> localURLHash;

        string path = @"[:\keepass\";
        string dbPath;
        string masterPath;

        [STAThread]
        public static void Main() {
            Application.Run(new SysTrayAccess());
        }

        public SysTrayAccess() {
            trayMenu = new ContextMenu();
            loadMenu();

            // Create a tray icon.
            const string TrayIconToolTip = "KeepPass SysTray Access";
            trayIcon = new NotifyIcon();
            trayIcon.Text = TrayIconToolTip;
            trayIcon.Icon = new Icon(SystemIcons.Shield, 40, 40);

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
        }

        protected override void OnLoad(EventArgs e) {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e) {
            Application.Exit();
        }

        private void onAddNewItem(object sender, EventArgs e) {
            
        }
        private void OnEditItemClick(object sender, EventArgs e) {
            var editor = new editorForm();
            editor.OnItemEdit += new editorForm.ItemEdited(editor_OnItemEdit);
            var mnu = (MenuItem)sender;
            var pwd = localPwdHash[mnu.Name];
            var url = localURLHash[mnu.Name];
            var items = mnu.Name.Split(MenuSeparator.ToCharArray());
            editor.FillFormFields(
                new Entry {
                    Category = items[0],
                    Title = items[1],
                    UserName = items[2],
                    Password = pwd,
                    Url = url
                }
            );
            editor.ShowDialog();
        }
        private void editor_OnItemEdit(Entry e) {
            // create kdb manager
            
            // find entry for item
            
            // update
            
            // set entry    
        }
        protected override void Dispose(bool isDisposing) {
            if (isDisposing) {
                // Release the icon resource.
                trayIcon.Dispose();
                TrayIconRefresher.RefreshTaskbarNotificationArea();
            }

            base.Dispose(isDisposing);
        }

        private bool setDriveLetter() {
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            for (int i = 0; i < letters.Length; i++) {
                if (Directory.Exists(path.Replace('[', letters[i]))) {
                    path = path.Replace('[', letters[i]);
                    return true;
                }
            }
            return false;
        }

        private void loadMenu() {
            if (setDriveLetter()) {

                dbPath = path + "passwords.kdbx";    // drive letter calculated
                masterPath = path + "master";

                // init local hashes
                localPwdHash = new Dictionary<string, string>();
                localURLHash = new Dictionary<string, string>();
                 
                string masterpw = File.ReadAllText(masterPath);
                var ioConnInfo = new IOConnectionInfo { Path = dbPath };
                var compKey = new CompositeKey();
                compKey.AddUserKey(new KcpPassword(masterpw));

                var db = new KeePassLib.PwDatabase();
                db.Open(ioConnInfo, compKey, null);

                foreach (var i in db.RootGroup.GetGroups(false)) {
                    var currMenu = new MenuItem(i.Name);
                    trayMenu.MenuItems.Add(currMenu);
                    loadSubMenuItems(i, currMenu);
                }

                db.Close();

                if (watcher == null) {
                    watchForDbChanges();
                }

            } else {
                const string DbFileNotFound = "Database file not found!";
                trayMenu.MenuItems.Add(DbFileNotFound);
            }

            //trayMenu.MenuItems.Add(MenuSeparator);
            //const string AddItem = "Add New Entry";
            //trayMenu.MenuItems.Add(AddItem, onAddNewItem);

            trayMenu.MenuItems.Add(MenuSeparator);
            const string Exit = "Exit";
            trayMenu.MenuItems.Add(Exit, OnExit);
        }

        private void loadSubMenuItems(PwGroup group, MenuItem currMenu) {

            const string UserName = "UserName";
            const string Title = "Title";
            const string URL = "URL";
            const string Password = "Password";
            const string CopyPwdCaption = "Copy Password";
            const string CopyUrlPwdCaption = "Launch URL && Copy Password";
            const string EditItemCaption = "Edit this item";

            uint grpCount = 0, entryCount = 0;
            group.GetCounts(false, out grpCount, out entryCount);
            if (entryCount > 0) {
                foreach (var j in group.GetEntries(false)) {

                    var titleMenu = new MenuItem(j.Strings.ReadSafe(Title));
                    currMenu.MenuItems.Add(titleMenu);

                    titleMenu.MenuItems.Add(new MenuItem(j.Strings.ReadSafe(UserName)));
                    var name = j.ParentGroup.Name + MenuSeparator + j.Strings.ReadSafe(Title) + MenuSeparator + j.Strings.ReadSafe(UserName);
                    var pwMnu = new MenuItem(CopyPwdCaption, onPwdMenuClick);
                    pwMnu.Name = name;
                    titleMenu.MenuItems.Add(pwMnu);
                    var pwlMnu = new MenuItem(CopyUrlPwdCaption, onUrlPwdMenuClick);
                    pwlMnu.Name = name;
                    titleMenu.MenuItems.Add(pwlMnu);

                    currMenu.MenuItems.Add(new MenuItem(MenuSeparator));

                    var editMnu = new MenuItem(EditItemCaption, OnEditItemClick);
                    editMnu.Name = name;
                    titleMenu.MenuItems.Add(editMnu);

                    try {
                        localPwdHash.Add(name, j.Strings.ReadSafe(Password));
                        localURLHash.Add(name, j.Strings.ReadSafe(URL));
                    } catch (Exception e) {
                        MessageBox.Show("Dupe found: " + name + MenuSeparator + j.Strings.ReadSafe(URL));
                    }

                }
            }
            // are there any group children
            if (grpCount > 0) {
                foreach (var j in group.Groups) {
                    var newGroupMenu = new MenuItem(j.Name);
                    currMenu.MenuItems.Add(newGroupMenu);
                    loadSubMenuItems(j, newGroupMenu);
                }
            }
        }

        private void onPwdMenuClick(object sender, EventArgs e) {
            copyToClipboard(sender, false);
        }

        private void onUrlPwdMenuClick(object sender, EventArgs e) {
            copyToClipboard(sender, true);
        }

        private void copyToClipboard(object sender, bool launchUrl) {
            Clipboard.Clear();
            var mnu = (MenuItem)sender;
            Clipboard.SetText(localPwdHash[mnu.Name]);
            if (launchUrl) {
                System.Diagnostics.Process.Start(localURLHash[mnu.Name]);
            }
        }

        FileSystemWatcher watcher;
        private void watchForDbChanges() {

            // Create a new FileSystemWatcher and set its properties.
            watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(dbPath);

            /* Watch for changes in LastWrite times */
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            // Only watch db files.
            watcher.Filter = "*.*";

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);

            // Begin watching.
            watcher.EnableRaisingEvents = true;

        }

        private void OnChanged(object source, FileSystemEventArgs e) {
            System.Threading.Thread.Sleep(3000);
            trayMenu.MenuItems.Clear();
            loadMenu();
        }

        private void InitializeComponent() {
            this.SuspendLayout();
            // 
            // SysTrayAccess
            // 
            this.ClientSize = new System.Drawing.Size(104, 0);
            this.Name = "SysTrayAccess";
            this.ResumeLayout(false);
        }

    }
}

