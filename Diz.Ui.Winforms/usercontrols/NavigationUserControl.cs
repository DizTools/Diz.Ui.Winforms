﻿using System.ComponentModel;
using Diz.Controllers.controllers;
using Diz.Controllers.interfaces;
using Diz.Core.util;

namespace Diz.Ui.Winforms.usercontrols
{
    public partial class NavigationUserControl : UserControl
    {
        private IDizDocument document;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IDizDocument Document
        {
            get => document;
            set
            {
                document = value;
                navigationEntryBindingSource.DataSource = Document?.NavigationHistory;

                if (navigationEntryBindingSource.DataSource != null)
                {
                    navigationEntryBindingSource.ListChanged += NavigationEntryBindingSourceOnListChanged;
                    navigationEntryBindingSource.PositionChanged += NavigationEntryBindingSourceOnPositionChanged;
                }
            }
        }

        private void NavigationEntryBindingSourceOnPositionChanged(object sender, EventArgs e)
        {
            
        }

        private void NavigationEntryBindingSourceOnListChanged(object sender, ListChangedEventArgs e)
        {
            if (navigationEntryBindingSource.Count == 0)
                return;
            
            SelectDataGridRow(navigationEntryBindingSource.Count - 1);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ISnesNavigation SnesNavigation { get; set; }

        public NavigationUserControl()
        {
            InitializeComponent();

            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
        }

        public int SelectedIndex => navigationEntryBindingSource.Position; // dataGridView1.SelectedRows[0].Index;

        public void Navigate(bool forwardDirection, int overshootAmount = 0)
        {
            if (navigationEntryBindingSource == null || navigationEntryBindingSource.Count == 0)
                return;
            
            var navigationEntryToUse = 
                Util.ClampIndex(SelectedIndex + (forwardDirection ? 1 : -1),
                navigationEntryBindingSource.Count);

            NavigateToEntry(navigationEntryToUse, overshootAmount);
            SelectDataGridRow(navigationEntryToUse);
        }

        private void NavigateToEntry(int indexToUse, int overshootAmount = 0)
        {
            NavigateToEntry(GetNavigationEntry(indexToUse), overshootAmount);
        }

        private void NavigateToEntry(NavigationEntry navigationEntry, int overshootAmount = 0)
        {
            var newSnesAddress = navigationEntry?.SnesOffset ?? -1;
            if (newSnesAddress == -1)
                return;
            
            var pcOffset = Document.Project.Data.ConvertSnesToPc(newSnesAddress);
            if (pcOffset == -1) 
                return;
            
            SnesNavigation.SelectOffsetWithOvershoot(pcOffset, overshootAmount);
        }

        private NavigationEntry GetNavigationEntry(int index)
        {
            if (index < 0 || index >= navigationEntryBindingSource.Count)
                return null;
            
            return (NavigationEntry) navigationEntryBindingSource[index];
        }

        private void SelectDataGridRow(int index)
        {
            if (index < 0 || index >= navigationEntryBindingSource.Count)
                return;
            
            navigationEntryBindingSource.Position = index;
        }

        private void navigationEntryBindingSource_CurrentChanged(object sender, System.EventArgs e) => 
            NavigateToCurrentNavigationEntry();

        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e) => 
            NavigateToCurrentNavigationEntry();

        private void NavigateToCurrentNavigationEntry()
        {
            if (navigationEntryBindingSource != null)
                NavigateToEntry(navigationEntryBindingSource.Position);
        }

        private void btnBack_Click(object sender, EventArgs e) => Navigate(false);
        private void btnForward_Click(object sender, EventArgs e) => Navigate(true);
        private void btnClearHistory_Click(object sender, EventArgs e) => navigationEntryBindingSource?.Clear();
    }
}
