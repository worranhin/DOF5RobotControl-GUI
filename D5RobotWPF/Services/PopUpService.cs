﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DOF5RobotControl_GUI.Services
{
    public class PopUpService : IPopUpService
    {
        public void Show(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(text);
            });
        }

        public void Show(string text, string title)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(text, title);
            });
        }
    }
}
