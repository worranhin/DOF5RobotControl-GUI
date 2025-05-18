using DOF5RobotControl_GUI.Model;

namespace UnitTest
{
    public class RoboticStateTests
    {
        [Fact]
        public void AssignJoint_NoSideEffect()
        {
            var state = new RoboticState();
            var joint = new JointSpace() { R1 = 1 };

            state.JointSpace = joint;
            joint.R1 = 2;

            Assert.Equal(1, state.JointSpace.R1);
        }

        [Fact]
        public void AssignJoint_TaskSpaceChanged()
        {
            var state = new RoboticState();
            var joint = new JointSpace() { P2 = 1 };
            var expectedPose = KineHelper.Forward(joint);

            state.JointSpace = joint;

            Assert.Equal(expectedPose.Px, state.TaskSpace.Px);
            Assert.Equal(expectedPose.Py, state.TaskSpace.Py);
            Assert.Equal(expectedPose.Pz, state.TaskSpace.Pz);
            Assert.Equal(expectedPose.Ry, state.TaskSpace.Ry);
            Assert.Equal(expectedPose.Rz, state.TaskSpace.Rz);
        }

        [Fact]
        public void JointChange_TaskSpaceChanged()
        {
            var state = new RoboticState();

            state.JointSpace.R1 = 1;
            var expectedPose = KineHelper.Forward(state.JointSpace);

            Assert.Equal(expectedPose.Px, state.TaskSpace.Px);
            Assert.Equal(expectedPose.Py, state.TaskSpace.Py);
            Assert.Equal(expectedPose.Pz, state.TaskSpace.Pz);
            Assert.Equal(expectedPose.Ry, state.TaskSpace.Ry);
            Assert.Equal(expectedPose.Rz, state.TaskSpace.Rz);
        }
    }
}
