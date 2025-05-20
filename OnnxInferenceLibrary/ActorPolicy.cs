using System.Collections.ObjectModel;

namespace OnnxInferenceLibrary
{
    public class ActorPolicy : OnnxInferenceBase
    {
        const float scale = 0.01F;
        readonly long[] InputShape = [1, 7];
        readonly ReadOnlyCollection<float> min = Array.AsReadOnly([-0.2F, -0.002F, -0.002F, -0.002F, -0.01F]);
        readonly ReadOnlyCollection<float> max = Array.AsReadOnly([0.2F, 0.002F, 0.002F, 0.002F, 0.01F]);

        public ActorPolicy() : base("policy_v0.3.onnx")
        {
        }

        /// <summary>
        /// 依据策略网络的输出，返回动作空间
        /// </summary>
        /// <param name="obs">观测空间，夹钳与钳口的距离，shape: (1, 7), [x, y, z] + 四元数</param>
        /// <returns>动作空间（5个关节的相对位移）</returns>
        public float[] Step(float[] obs)
        {          
            var inferResult = Inference(obs, InputShape);
            float[] processResult = new float[inferResult.Length];
            
            for (int i = 0; i < inferResult.Length; i++)
            {
                float value = inferResult[i] * scale;
                processResult[i] = Math.Clamp(value, min[i], max[i]);
            }

            return processResult;
        }
    }
}
