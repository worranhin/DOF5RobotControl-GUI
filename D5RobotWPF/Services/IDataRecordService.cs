using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public interface IDataRecordService
    {
        /// <summary>
        /// Indicate the record is started or not.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Do initialization and prepare for record.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the record, save datas.
        /// </summary>
        [Obsolete("Use StopAsync when possible.")]
        void Stop();

        /// <summary>
        /// Stop the record and save datas in async way.
        /// </summary>
        /// <returns></returns>
        public Task StopAsync();

        /// <summary>
        /// Do one record with current and target joints values.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        public void Record(JointSpace current, JointSpace target);

        /// <summary>
        /// Do one record with joints values and images.
        /// </summary>
        /// <param name="currentJoints"></param>
        /// <param name="deltaJoints"></param>
        /// <param name="topFrame"></param>
        /// <param name="bottomFrame"></param>
        void Record(JointSpace currentJoints, JointSpace deltaJoints, CamFrame topFrame, CamFrame bottomFrame);
    }
}