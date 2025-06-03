using D5R;
using DOF5RobotControl_GUI.Model;

namespace D5Robot
{
    public struct D5State
    {
        public D5JointSpace JointSpace;
        public D5TaskSpace TaskSpace
        {
            readonly get => KineHelper.Forward(JointSpace);
            set => JointSpace = KineHelper.Inverse(value);
        }

        public D5State()
        {
            JointSpace = new();
        }

        public D5State(D5JointSpace joint)
        {
            JointSpace = joint;
        }

        public D5State(double r1, double p2, double p3, double p4, double r5)
        {
            JointSpace = new() { R1 = r1, P2 = p2, P3 = p3, P4 = p4, R5 = r5 };
        }

        public static D5State operator +(D5State left, D5State right)
        {
            D5State result;
            result.JointSpace = left.JointSpace + right.JointSpace;
            return result;
        }

        public static D5State operator -(D5State left, D5State right)
        {
            D5State result = left;
            result.JointSpace = left.JointSpace - right.JointSpace;
            return result;
        }
    }
}
