using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DOF5RobotControl_GUI.ViewModel
{
    internal partial class ManualControlViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _speedMode;

        [ObservableProperty]
        private bool _gamepadConnected = false;

        [ObservableProperty]
        private bool _topCameraConnected = false;
        [ObservableProperty]
        private ImageSource? _topImageSource;
        public readonly Mutex TopImgSrcMutex = new();

        [ObservableProperty]
        private bool _bottomCameraConnected = false;
        [ObservableProperty]
        private ImageSource? _bottomImageSource;
        public readonly Mutex BottomImgSrcMutex = new();
    }
}
