﻿using System.ComponentModel;
using Diz.Controllers.controllers;
using Diz.Controllers.interfaces;

namespace Diz.Ui.Winforms.dialogs;

public partial class NavigationForm : Form
{
    private IDizDocument document;
    private ISnesNavigation snesNavigation;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IDizDocument Document
    {
        get => document;
        set
        {
            document = value;
            navigationCtrl.Document = Document;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ISnesNavigation SnesNavigation
    {
        get => snesNavigation;
        set
        {
            snesNavigation = value;
            navigationCtrl.SnesNavigation = snesNavigation;
        }
    }

    public NavigationForm()
    {
        InitializeComponent();
        FormClosing += Navigation_Closing;
    }
        
    private void Navigation_Closing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) 
            return;
            
        e.Cancel = true;
        Hide();
    }

    private void Navigation_Load(object sender, EventArgs e)
    {
            
    }

    public void Navigate(bool forwardDirection, int overshootAmount = 0)
    {
        navigationCtrl.Navigate(forwardDirection, overshootAmount);
    }

    private void navigationCtrl_Load(object sender, EventArgs e)
    {

    }
}