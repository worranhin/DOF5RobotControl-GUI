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
        /// Copy values of another TaskSpace to this
        /// </summary>
        /// <param name="taskSpace"></param>
        /// <returns>this instance</returns>
        public TaskSpace Copy(TaskSpace taskSpace)
        {
            Px = taskSpace.Px;
            Py = taskSpace.Py;
            Pz = taskSpace.Pz;
            Ry = taskSpace.Ry;
            Rz = taskSpace.Rz;

            return this;
        }

        /// <summary>
        /// Return a new TaskSpace with same values of this
        /// </summary>
        /// <returns>An instance clone.</returns>
        public TaskSpace Clone()
        {
            TaskSpace taskSpace = new();
            taskSpace.Copy(this);
            return taskSpace;
        }
    }
}
