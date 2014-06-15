﻿using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace VisualCompiler.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for CompilerOptionsWindow.xaml
    /// </summary>
    public partial class CompilerOptionsWindow
    {
        public CompilerOptionsViewModel ViewModel
        {
            get { return (CompilerOptionsViewModel) DataContext; }
        }
        public CompilerOptionsWindow()
        {
            InitializeComponent();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            ViewModel.Accepted = true;
            var result = (ListBox.SelectedItems.Cast<object>()
                .Select(selectedItem => selectedItem.ToString()))
                .ToList();
            ViewModel.Capabilities = result;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            ViewModel.Accepted = false;
            Close();
        }
    }
}