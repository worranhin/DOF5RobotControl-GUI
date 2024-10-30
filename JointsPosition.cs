using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI
{
    internal class JointsPositon
    {
        

        public int R1 { get; set; } = 0;
        public int P2 { get; set; } = 0;
        public int P3 { get; set; } = 0;
        public int P4 { get; set; } = 0;
        public int R5 { get; set; } = 0;

        public JointsPositon(int r1, int p2, int p3, int p4, int r5)
        {
            R1 = r1;
            P2 = p2;
            P3 = p3;
            P4 = p4;
            R5 = r5;
        }

        public D5RControl.Joints ToD5RJoints()
        {
            D5RControl.Joints j = new(this.R1, this.P2, this.P3, this.P4, this.R5);
            return j;
        }

        // 尝试使用 INotifyChanged

        //private int _r1;
        //private int _p2;
        //private int _p3;
        //private int _p4;
        //private int _r5;

        //public int R1
        //{
        //    get { return _r1; }
        //    set
        //    {
        //        if (_r1 != value)
        //        {
        //            _r1 = value;
        //            OnpropertyChanged();
        //        }
        //    }
        //}
        //public int P2
        //{
        //    get { return _p2; }
        //    set
        //    {
        //        if (_p2 != value)
        //        {
        //            _p2 = value; OnpropertyChanged();
        //        }
        //    }
        //}
        //public int P3
        //{
        //    get { return _p3; }
        //    set
        //    {
        //        if (_p3 != value)
        //        {
        //            _p3 = value; OnpropertyChanged();
        //        }
        //    }
        //}
        //public int P4
        //{
        //    get { return _p4; }
        //    set
        //    {
        //        if (_p4 != value)
        //        {
        //            _p4 = value; OnpropertyChanged();
        //        }
        //    }
        //}
        //public int R5
        //{
        //    get { return _r5; }
        //    set
        //    {
        //        if (_r5 != value)
        //        {
        //            _r5 = value; OnpropertyChanged();
        //        }
        //    }
        //}

        //public event PropertyChangedEventHandler? PropertyChanged;

        //protected void OnpropertyChanged([CallerMemberName] string? propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    };
}
