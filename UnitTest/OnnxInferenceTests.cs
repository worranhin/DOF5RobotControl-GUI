using Xunit.Abstractions;
using OnnxInferenceLibrary;
using System.Diagnostics;

namespace UnitTest
{
    public class OnnxInferenceTests(ITestOutputHelper helper)
    {
        [Fact]
        public void TestInference()
        {
            var input = new float[] { 1.0F, 2.0F, 3.0F, 4.0F, 5.0F, 6.0F, 7.0F };

            using var actorPolicy = new ActorPolicy();

            double t_mean = 0;
            for (int i = 0; i < 1000; i++)
            {
                var sw = Stopwatch.StartNew();
                var result = actorPolicy.Step(input);
                var t = sw.ElapsedMilliseconds;
                t_mean = t_mean + (t - t_mean) / (i + 1);
                Assert.Equal(5, result.Length);
            }

            helper.WriteLine($"Inference time: {t_mean} ms");
        }
    }
}
