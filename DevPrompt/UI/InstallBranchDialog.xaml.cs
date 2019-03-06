﻿using System.Windows;

namespace DevPrompt.UI
{
    internal partial class InstallBranchDialog : Window
    {
        public InstallBranchDialogVM ViewModel { get; }

        public InstallBranchDialog()
        {
            this.ViewModel = new InstallBranchDialogVM(this, "d16.0");

            this.InitializeComponent();
        }

        public string BranchName
        {
            get
            {
                return this.ViewModel.Name;
            }
        }

        private void OnClickOk(object sender, RoutedEventArgs args)
        {
            this.DialogResult = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            this.editControl.Focus();
            this.editControl.SelectAll();
        }
    }
}
