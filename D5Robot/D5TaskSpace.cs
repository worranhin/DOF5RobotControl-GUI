namespace D5Robot
{
    public class D5TaskSpace
    {
        public double Px;
        public double Py;
        public double Pz;
        public double Ry;
        public double Rz;

        public override string ToString()
        {
            string str = base.ToString() + $"\tPx:{Px} Py:{Py} Pz:{Pz} Ry:{Ry} Rz:{Rz}";
            return str;
        }

        public static double Distance(D5TaskSpace a, D5TaskSpace b)
        {
            double dx = a.Px - b.Px;
            double dy = a.Py - b.Py;
            double dz = a.Pz - b.Pz;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
