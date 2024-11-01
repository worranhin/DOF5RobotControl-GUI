﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DOF5RobotControl_GUI
{
    internal class JogHandler
    {
        private CancellationTokenSource cancelJoggingSource;
        private CancellationToken cancelJoggingToken;


        public JogHandler()
        {
            cancelJoggingSource = new CancellationTokenSource();
            cancelJoggingToken = cancelJoggingSource.Token;
        }

        public void StartJogging(D5RControl.Joints joints)
        {
            cancelJoggingSource = new();  // initialize CancelSource
            cancelJoggingToken = cancelJoggingSource.Token;

            Task.Run(() =>
            {
                while (!cancelJoggingToken.IsCancellationRequested)
                {
                    int result = D5RControl.JointsMoveRelative(joints);

                    if (result != 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Jog error in JogHandler.");
                        });
                        break;
                    }

                    Thread.Sleep(20);
                }
            });
        }

        public void TestStartJogging()
        {
            cancelJoggingSource = new();
            cancelJoggingToken = cancelJoggingSource.Token;

            Task.Run(() =>
            {
                while (!cancelJoggingToken.IsCancellationRequested)
                {
                    Debug.WriteLine("Is jogging...");
                    Thread.Sleep(500);
                }
            }, cancelJoggingToken);
        }

        public void StopJogging()
        {
            cancelJoggingSource?.Cancel();
        }
    }
}
