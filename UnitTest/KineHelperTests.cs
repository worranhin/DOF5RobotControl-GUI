using DOF5RobotControl_GUI.Model;

namespace UnitTest
{
    public class KineHelperTests
    {
        [Fact]
        public void Forward_Inverse_ExpectEqual()
        {
            var joint = new JointSpace() { P2 = 1 };

            var pose = KineHelper.Forward(joint);
            var joint2 = KineHelper.Inverse(pose);

            Assert.Equal(joint.R1, joint2.R1);
            Assert.Equal(joint.P2, joint2.P2);
            Assert.Equal(joint.P3, joint2.P3);
            Assert.Equal(joint.P4, joint2.P4);
            Assert.Equal(joint.R5, joint2.R5);
        }

        [Fact]
        public void Inverse_Forward_ExpectEqual()
        {
            var pose = new TaskSpace() { Px = 1 };

            var joint = KineHelper.Inverse(pose);
            var pose2 = KineHelper.Forward(joint);

            Assert.Equal(pose.Px, pose2.Px);
            Assert.Equal(pose.Py, pose2.Py);
            Assert.Equal(pose.Pz, pose2.Pz);
            Assert.Equal(pose.Ry, pose2.Ry);
            Assert.Equal(pose.Rz, pose2.Rz);
        }

        [Fact]
        public void Forward_StubR1_ExpectedPose()
        {
            var joint = new JointSpace();
            var originPose = KineHelper.Forward(joint);
            var expectedPose = originPose.Clone();

            joint.R1 += 90;
            expectedPose.Px = -originPose.Py;
            expectedPose.Py = originPose.Px;
            expectedPose.Rz += 90;

            var pose = KineHelper.Forward(joint);

            Assert.Equal(expectedPose.Px, pose.Px);
            Assert.Equal(expectedPose.Py, pose.Py);
            Assert.Equal(expectedPose.Pz, pose.Pz);
            Assert.Equal(expectedPose.Ry, pose.Ry);
            Assert.Equal(expectedPose.Rz, pose.Rz);
        }

        [Fact]
        public void Forward_StubP2_ExpectedPose()
        {
            var joint = new JointSpace();
            var originPose = KineHelper.Forward(joint);
            var expectedPose = originPose.Clone();

            joint.P2 += 1;
            expectedPose.Py -= 1;

            var pose = KineHelper.Forward(joint);

            Assert.Equal(expectedPose.Px, pose.Px);
            Assert.Equal(expectedPose.Py, pose.Py);
            Assert.Equal(expectedPose.Pz, pose.Pz);
            Assert.Equal(expectedPose.Ry, pose.Ry);
            Assert.Equal(expectedPose.Rz, pose.Rz);
        }

        [Fact]
        public void Forward_StubP3_ExpectedPose()
        {
            var joint = new JointSpace();
            var originPose = KineHelper.Forward(joint);
            var expectedPose = originPose.Clone();

            joint.P3 += 1;
            expectedPose.Px += 1;

            var pose = KineHelper.Forward(joint);

            Assert.Equal(expectedPose.Px, pose.Px);
            Assert.Equal(expectedPose.Py, pose.Py);
            Assert.Equal(expectedPose.Pz, pose.Pz);
            Assert.Equal(expectedPose.Ry, pose.Ry);
            Assert.Equal(expectedPose.Rz, pose.Rz);
        }

        [Fact]
        public void Forward_StubP4_ExpectedPose()
        {
            var joint = new JointSpace();
            var expectedPose = KineHelper.Forward(joint);

            joint.P4 += 1;
            expectedPose.Pz -= 1;

            var pose = KineHelper.Forward(joint);

            Assert.Equal(expectedPose.Px, pose.Px);
            Assert.Equal(expectedPose.Py, pose.Py);
            Assert.Equal(expectedPose.Pz, pose.Pz);
            Assert.Equal(expectedPose.Ry, pose.Ry);
            Assert.Equal(expectedPose.Rz, pose.Rz);
        }

        [Fact]
        public void Forward_StubR5_ExpectedPose()
        {
            var joint = new JointSpace();
            var originPose = KineHelper.Forward(joint);
            var expectedPose = KineHelper.Forward(joint);

            joint.R5 += 90;
            //expectedPose.Px = 0;  // 这个的预期变化暂时未知
            //expectedPose.Pz += originPose.Px;
            expectedPose.Ry -= 90;

            var pose = KineHelper.Forward(joint);

            //Assert.Equal(expectedPose.Px, pose.Px);
            Assert.Equal(expectedPose.Py, pose.Py);
            //Assert.Equal(expectedPose.Pz, pose.Pz);
            Assert.Equal(expectedPose.Ry, pose.Ry);
            Assert.Equal(expectedPose.Rz, pose.Rz);
        }

        [Fact]
        public void InverseDifferential_StubPose_ExpectedJoint()
        {
            var currentJoint = new JointSpace();
            var currentPose = KineHelper.Forward(currentJoint);
            var diffPose = new TaskSpace() { Px = 1 };
            var expectedJoint = new JointSpace() { P3 = 1 };

            var diffJoint = KineHelper.InverseDifferential(diffPose, currentPose);

            Assert.Equal(expectedJoint.R1, diffJoint.R1);
            Assert.Equal(expectedJoint.P2, diffJoint.P2);
            Assert.Equal(expectedJoint.P3, diffJoint.P3);
            Assert.Equal(expectedJoint.P4, diffJoint.P4);
            Assert.Equal(expectedJoint.R5, diffJoint.R5);
        }
    }
}
