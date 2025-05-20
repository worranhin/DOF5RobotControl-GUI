using DOF5RobotControl_GUI.Model;

namespace UnitTest
{
    public class RoboticStateTests
    {
        [Fact]
        public void JointSpace_Set_NoSideEffect()
        {
            var state = new RoboticState();
            var joint = new JointSpace() { R1 = 1 };

            state.JointSpace = joint;
            joint.R1 = 2;

            Assert.Equal(1, state.JointSpace.R1);
        }

        [Fact]
        public void JointSpace_Set_TaskSpaceChanged()
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
        public void JointSpace_ChangeProperty_TaskSpaceChanged()
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

        [Fact]
        public void TaskSpace_Set_NoSideEffect()
        {
            var state = new RoboticState();
            var pose = new TaskSpace() { Px = 1 };

            state.TaskSpace = pose;
            pose.Px = 2;

            Assert.Equal(1, state.TaskSpace.Px);
        }

        [Fact]
        public void TaskSpace_Set_JointSpaceChanged()
        {
            var state = new RoboticState();
            var pose = new TaskSpace() { Px = 1 };
            var expectedJoint = KineHelper.Inverse(pose);

            state.TaskSpace = pose;

            Assert.Equal(expectedJoint.R1, state.JointSpace.R1);
            Assert.Equal(expectedJoint.P2, state.JointSpace.P2);
            Assert.Equal(expectedJoint.P3, state.JointSpace.P3);
            Assert.Equal(expectedJoint.P4, state.JointSpace.P4);
            Assert.Equal(expectedJoint.R5, state.JointSpace.R5);
        }

        [Fact]
        public void TaskSpace_ChangeProperty_JointSpaceChanged()
        {
            var state = new RoboticState();

            state.TaskSpace.Px = 1;
            var expectedJoint = KineHelper.Inverse(state.TaskSpace);

            Assert.Equal(expectedJoint.R1, state.JointSpace.R1);
            Assert.Equal(expectedJoint.P2, state.JointSpace.P2);
            Assert.Equal(expectedJoint.P3, state.JointSpace.P3);
            Assert.Equal(expectedJoint.P4, state.JointSpace.P4);
            Assert.Equal(expectedJoint.R5, state.JointSpace.R5);
        }
    }
}
