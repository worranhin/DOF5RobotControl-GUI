using Microsoft.ML.OnnxRuntime;
using System.Diagnostics;
using System.Numerics.Tensors;

namespace OnnxInferenceLibrary
{
    public class OnnxInferenceBase : IDisposable
    {
        readonly InferenceSession session;

        private bool disposedValue;

        public OnnxInferenceBase(string modelPath)
        {
            session = new InferenceSession(modelPath);  // create inference session and cache it (creating and loading is expensive)
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    session.Dispose();
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                disposedValue = true;
            }
        }

        ~OnnxInferenceBase()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 执行一次推理
        /// </summary>
        /// <param name="input">模型的输入</param>
        /// <param name="shape">输入的形状</param>
        /// <returns>Span</returns>
        protected ReadOnlySpan<float> Inference(float[] input, long[] shape)
        {
            using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(input, shape);
            var inputs = new Dictionary<string, OrtValue>
            {
                {"obs", inputOrtValue}
            };

            using var runOptions = new RunOptions();

            using var output = session.Run(runOptions, inputs, session.OutputNames);  // 进行推理

            var output_0 = output[0];

            var outputData = output_0.GetTensorDataAsSpan<float>();

            return outputData;
        }
    }
}
