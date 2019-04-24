using System;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace AlgoSimple.Perfee.Tests
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _output;

        public UnitTest1(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Test1()
        {
            Perfee.Configuration.AddLogger(_output.WriteLine);

            var random = new Random();

            for (var i = 0; i < 100; i++)
            {
                var g = $"g{random.Next(1, 5)}";
                using (Perfee.MeasureGroup(g))
                {
                    Thread.Sleep(random.Next(100, 500));
                }
            }

            var logs = Perfee.GetLogs();
            Assert.NotEmpty(logs);
        }
    }
}
