using Opc.UaFx.Server;
using Opc.UaFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DOF5RobotControl_GUI.ViewModel;

namespace DOF5RobotControl_GUI.Model
{
    internal class D5RobotOpcNodeManager : OpcNodeManager
    {
        private OpcDataVariableNode<double>? r1_Node;
        private OpcDataVariableNode<double>? p2_Node;
        private OpcDataVariableNode<double>? p3_Node;
        private OpcDataVariableNode<double>? p4_Node;
        private OpcDataVariableNode<double>? r5_Node;
        //private OpcMethodNode method;
        private MainViewModel _viewModel;

        public D5RobotOpcNodeManager(MainViewModel vm)
        : base("dof5")
        {
            _viewModel = vm;
        }

        //[return: OpcArgument("是否成功")]//说明参数
        //private bool runMotor()//运行
        //{
        //    try
        //    {
        //        _mainWindow.SomeOtherMethod(12);
        //    }
        //    catch //异常处理，警告窗口
        //    {
        //        MessageBox.Show("CALL FAIL!");
        //        return false;
        //    }
        //    return true;
        //}
        private bool OpcBtn_1([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(1);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_2([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(2);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_3([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(3);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_4([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(4);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_5([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(5);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_6([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(6);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_7([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(7);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_8([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(8);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_9([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(9);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_10([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(10);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_11([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(11);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        [return: OpcArgument("调用情况")]//此处为输出参数，类型为下函数返回的类型
        private bool OpcBtn_12([OpcArgument("运行", Description = "是否运行")] bool change)//内部为输入参数
        {
            try
            {
                _viewModel.OpcMapMethod(12);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_13([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(13);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool OpcBtn_14([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                _viewModel.OpcMapMethod(14);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }


        protected override IEnumerable<IOpcNode> CreateNodes(OpcNodeReferenceCollection references)
        {
            var rootNode = new OpcObjectNode(new OpcName("dof5", this.DefaultNamespaceIndex));
            references.Add(rootNode, OpcObjectTypes.ObjectsFolder);

            r1_Node = new OpcDataVariableNode<double>(rootNode, "r1");
            //r1_Node.Value = MainWindow.joints100[0];
            r1_Node.Value = _viewModel.TargetState.JointSpace.R1;
            p2_Node = new OpcDataVariableNode<double>(rootNode, "p2");
            //p2_Node.Value = MainWindow.joints100[1];
            p2_Node.Value = _viewModel.TargetState.JointSpace.P2;
            p3_Node = new OpcDataVariableNode<double>(rootNode, "p3");
            //p3_Node.Value = MainWindow.joints100[2];
            p3_Node.Value = _viewModel.TargetState.JointSpace.P3;
            p4_Node = new OpcDataVariableNode<double>(rootNode, "p4");
            //p4_Node.Value = MainWindow.joints100[3];
            p4_Node.Value = _viewModel.TargetState.JointSpace.P4;
            r5_Node = new OpcDataVariableNode<double>(rootNode, "r5");
            //r5_Node.Value = MainWindow.joints100[4];
            r5_Node.Value = _viewModel.TargetState.JointSpace.R5;
            r1_Node.WriteVariableValueCallback = handleWriteR1_NodeCallback;
            r1_Node.ReadVariableValueCallback = handleReadR1_NodeCallback;
            p2_Node.WriteVariableValueCallback = handleWriteP2_NodeCallback;
            p2_Node.ReadVariableValueCallback = handleReadP2_NodeCallback;
            p3_Node.WriteVariableValueCallback = handleWriteP3_NodeCallback;
            p3_Node.ReadVariableValueCallback = handleReadP3_NodeCallback;
            p4_Node.WriteVariableValueCallback = handleWriteP4_NodeCallback;
            p4_Node.ReadVariableValueCallback = handleReadP4_NodeCallback;
            r5_Node.WriteVariableValueCallback = handleWriteR5_NodeCallback;
            r5_Node.ReadVariableValueCallback = handleReadR5_NodeCallback;
            var globalControlNode = new OpcFolderNode(rootNode, "全局控制");
            var refreshNode = new OpcMethodNode(globalControlNode, "刷新", new Func<bool, bool>(this.OpcBtn_1));
            var connectNode = new OpcMethodNode(globalControlNode, "连接", new Func<bool, bool>(this.OpcBtn_2));
            var ZeroPosNode = new OpcMethodNode(globalControlNode, "零点位", new Func<bool, bool>(this.OpcBtn_3));
            var IdlePosNode = new OpcMethodNode(globalControlNode, "待机位", new Func<bool, bool>(this.OpcBtn_4));
            var PreChangeJawPosNode = new OpcMethodNode(globalControlNode, "换夹钳预备位", new Func<bool, bool>(this.OpcBtn_5));
            var ChangeJawPosNode = new OpcMethodNode(globalControlNode, "换夹钳位", new Func<bool, bool>(this.OpcBtn_6));
            var AssemblePos1Node = new OpcMethodNode(globalControlNode, "装配位1", new Func<bool, bool>(this.OpcBtn_7));
            var AssemblePos2Node = new OpcMethodNode(globalControlNode, "装配位2", new Func<bool, bool>(this.OpcBtn_8));
            var AssemblePos3Node = new OpcMethodNode(globalControlNode, "装配位3", new Func<bool, bool>(this.OpcBtn_9));
            var PreFetchRingPosNode = new OpcMethodNode(globalControlNode, "取零件预备位", new Func<bool, bool>(this.OpcBtn_10));
            var FetchRingPosNode = new OpcMethodNode(globalControlNode, "取零件位", new Func<bool, bool>(this.OpcBtn_11));
            var runNode = new OpcMethodNode(globalControlNode, "运行", new Func<bool, bool>(this.OpcBtn_12));
            var StopNode = new OpcMethodNode(globalControlNode, "停止", new Func<bool, bool>(this.OpcBtn_13));
            var SetZeroNode = new OpcMethodNode(globalControlNode, "置零", new Func<bool, bool>(this.OpcBtn_14));


            return new IOpcNode[] { rootNode };

        }


        OpcVariableValue<object> handleWriteR1_NodeCallback(OpcWriteVariableValueContext context, OpcVariableValue<object> value)
        {
            object objectValue = value.Value;
            double nodeValue = (double)objectValue;
            {
                // 安全地更新标签文本
                _viewModel.Dispatcher.Invoke(() =>
                {
                    //_viewModel.JointValueR1.Text = nodeValue.ToString();
                    _viewModel.TargetState.JointSpace.R1 = nodeValue;
                });
            }
            return value;
        }
        OpcVariableValue<object> handleReadR1_NodeCallback(OpcReadVariableValueContext context, OpcVariableValue<object> value)
        {
            double r1_change = _viewModel.TargetState.JointSpace.R1;
            OpcVariableValue<object> r = new OpcVariableValue<object>(r1_change);
            return r;
        }
        OpcVariableValue<object> handleWriteP2_NodeCallback(OpcWriteVariableValueContext context, OpcVariableValue<object> value)
        {
            object objectValue = value.Value;
            double nodeValue = (double)objectValue;
            {
                _viewModel.Dispatcher.Invoke(() =>
                {
                    //_viewModel.JointValueP2.Text = nodeValue.ToString();
                    _viewModel.TargetState.JointSpace.P2 = nodeValue;
                });
            }
            return value;
        }
        OpcVariableValue<object> handleReadP2_NodeCallback(OpcReadVariableValueContext context, OpcVariableValue<object> value)
        {
            double p2_change = _viewModel.TargetState.JointSpace.P2;
            OpcVariableValue<object> r = new OpcVariableValue<object>(p2_change);
            return r;
        }
        OpcVariableValue<object> handleWriteP3_NodeCallback(OpcWriteVariableValueContext context, OpcVariableValue<object> value)
        {
            object objectValue = value.Value;
            double nodeValue = (double)objectValue;
            {
                _viewModel.Dispatcher.Invoke(() =>
                {
                    //_viewModel.JointValueP3.Text = nodeValue.ToString();
                    _viewModel.TargetState.JointSpace.P3 = nodeValue;
                });
            }
            return value;
        }
        OpcVariableValue<object> handleReadP3_NodeCallback(OpcReadVariableValueContext context, OpcVariableValue<object> value)
        {
            double p3_change = _viewModel.TargetState.JointSpace.P3;
            OpcVariableValue<object> r = new OpcVariableValue<object>(p3_change);
            return r;
        }
        OpcVariableValue<object> handleWriteP4_NodeCallback(OpcWriteVariableValueContext context, OpcVariableValue<object> value)
        {
            object objectValue = value.Value;
            double nodeValue = (double)objectValue;
            {
                _viewModel.Dispatcher.Invoke(() =>
                {
                    //_viewModel.JointValueP4.Text = nodeValue.ToString();
                    _viewModel.TargetState.JointSpace.P4 = nodeValue;
                });
            }
            return value;
        }
        OpcVariableValue<object> handleReadP4_NodeCallback(OpcReadVariableValueContext context, OpcVariableValue<object> value)
        {
            double jointValue = _viewModel.TargetState.JointSpace.P4;
            OpcVariableValue<object> r = new OpcVariableValue<object>(jointValue);
            return r;
        }
        OpcVariableValue<object> handleWriteR5_NodeCallback(OpcWriteVariableValueContext context, OpcVariableValue<object> value)
        {
            object objectValue = value.Value;
            double nodeValue = (double)objectValue;
            {
                _viewModel.Dispatcher.Invoke(() =>
                {
                    //_viewModel.JointValueR5.Text = nodeValue.ToString();
                    _viewModel.TargetState.JointSpace.R5 = nodeValue;
                });
            }
            return value;
        }
        OpcVariableValue<object> handleReadR5_NodeCallback(OpcReadVariableValueContext context, OpcVariableValue<object> value)
        {
            double jointValue = _viewModel.TargetState.JointSpace.R5;
            OpcVariableValue<object> r = new OpcVariableValue<object>(jointValue);
            return r;
        }

    }
}
