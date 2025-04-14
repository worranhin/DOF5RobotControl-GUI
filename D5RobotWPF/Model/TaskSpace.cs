using CommunityToolkit.Mvvm.ComponentModel;

namespace DOF5RobotControl_GUI.Model
{
    public partial class TaskSpace : ObservableObject
    {
        [ObservableProperty]
        private double _px;
        [ObservableProperty]
        private double _py;
        [ObservableProperty]
        private double _pz;
        [ObservableProperty]
        private double _ry;
        [ObservableProperty]
        private double _rz;

        public override string ToString()
        {
            string str = base.ToString() + $"\tPx:{Px} Py:{Py} Pz:{Pz} Ry:{Ry} Rz:{Rz}";
            return str;
        }

        public static double Distance(TaskSpace a, TaskSpace b)
        {
            double dx = a.Px - b.Px;
            double dy = a.Py - b.Py;
            double dz = a.Pz - b.Pz;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Return a new TaskSpace object with same value as this one.
        /// </summary>
        /// <returns></returns>
        public TaskSpace Clone()
        {
            return new TaskSpace() { Px = Px, Py = Py, Pz = Pz, Ry = Ry, Rz = Rz };
        }

        /// <summary>
        /// Copy the input Taskspace to this
        /// </summary>
        /// <param name="space">Taskspace to copy from</param>
        /// <returns>return this Taskspace</returns>
        public TaskSpace Copy(TaskSpace space)
        {
            Px = space.Px;
            Py = space.Py;
            Pz = space.Pz;
            Ry = space.Ry;
            Rz = space.Rz;

            return this;
        }
    }
}
