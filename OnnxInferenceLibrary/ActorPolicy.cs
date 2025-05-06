using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnnxInferenceLibrary
{
    public class ActorPolicy : OnnxInferenceBase
    {
        readonly long[] InputShape = [1, 7];

        public ActorPolicy(): base("policy.onnx")
        {
        }

        /// <summary>
        /// 依据策略网络的输出，返回动作空间
        /// </summary>
        /// <param name="obs">观测空间，夹钳与钳口的距离，shape: (1, 7), [x, y, z] + 四元数</param>
        /// <returns>动作空间</returns>
        public ReadOnlySpan<float> Step(float[] obs)
        {
            return Inference(obs, InputShape);
        }
    }
}
