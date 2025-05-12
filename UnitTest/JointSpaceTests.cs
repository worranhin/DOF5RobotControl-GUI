using D5R;
using DOF5RobotControl_GUI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    public class JointSpaceTests
    {
        [Fact]
        public void TestD5RJoint()
        {
            var jointSpace = new JointSpace()
            {
                R1 = -10,
                P2 = 5,
                P3 = 10,
                P4 = -5,
                R5 = 10
            };

            var controlJoint = jointSpace.ToD5RJoints();
            var expectJoint = new Joints()
            {
                R1 = 35000,
                P2 = -5_000_000,
                P3 = 10_000_000,
                P4 = -5_000_000,
                R5 = 35000
            };

            Assert.Equal(expectJoint.R1, controlJoint.R1);
            Assert.Equal(expectJoint.R5, controlJoint.R5);
            Assert.Equal(expectJoint.P2, controlJoint.P2);
            Assert.Equal(expectJoint.P3, controlJoint.P3);
            Assert.Equal(expectJoint.P4, controlJoint.P4);

        }
    }
}
