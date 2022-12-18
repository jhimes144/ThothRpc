using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;
using ThothRpc.Models.Dto;
using ThothRpc.Optimizer;

namespace ThothRpc.Tests.Unit
{
    [TestFixture]
    public class PacketAnalyzerTests
    {
        Func<MethodCallDto> _methodCallFactory = () => new MethodCallDto();
        Func<MethodResponseDto> _methodResponseFactory = () => new MethodResponseDto();

        IInternalThothOptimizer _noOptimizer = Mock.Of<IInternalThothOptimizer>(o => o.IsOptimized == false);
        byte[] _randomData = new byte[100];

        [SetUp]
        public void Setup()
        {
            var rn = new Random();
            rn.NextBytes(_randomData);
        }

        [Test]
        public void MethodCallSerializes_NoArgs()
        {
            var analyzer = new PacketAnalyzer(_noOptimizer, _methodCallFactory, _methodResponseFactory);

            var methodCallDto = new MethodCallDto
            {
                CallId = 2,
                ClassTarget = "Target",
                Method = "TargetMethod"
            };

            var bytes = analyzer.SerializePacket(methodCallDto);
            var cMethodCallDto = (MethodCallDto)analyzer.DeserializePacket(bytes);

            assertMethodCallEqual(methodCallDto, cMethodCallDto);
        }

        [Test]
        public void MethodCallSerializes_Args()
        {
            var analyzer = new PacketAnalyzer(_noOptimizer, _methodCallFactory, _methodResponseFactory);

            var methodCallDto = new MethodCallDto
            {
                CallId = 2,
                ClassTarget = "Target",
                Method = "TargetMethod",
                ArgumentsData = new List<ReadOnlyMemory<byte>>
                {
                    (byte[])_randomData.Clone(),
                    (byte[])_randomData.Clone(),
                    (byte[])_randomData.Clone()
                }
            };

            var bytes = analyzer.SerializePacket(methodCallDto);
            var cMethodCallDto = (MethodCallDto)analyzer.DeserializePacket(bytes);

            assertMethodCallEqual(methodCallDto, cMethodCallDto);
        }

        [Test]
        public void MethodCallSerializes_NoCallId()
        {
            var analyzer = new PacketAnalyzer(_noOptimizer, _methodCallFactory, _methodResponseFactory);

            var methodCallDto = new MethodCallDto
            {
                CallId = null,
                ClassTarget = "Target",
                Method = "TargetMethod"
            };

            var bytes = analyzer.SerializePacket(methodCallDto);
            var cMethodCallDto = (MethodCallDto)analyzer.DeserializePacket(bytes);

            assertMethodCallEqual(methodCallDto, cMethodCallDto);
        }

        [Test]
        public void MethodCallSerializes_Optimized()
        {
            var analyzer = new PacketAnalyzer(getMockOptimizer(), _methodCallFactory, _methodResponseFactory);

            var methodCallDto = new MethodCallDto
            {
                CallId = 2,
                ClassTarget = "Target",
                Method = "TargetMethod"
            };

            var bytes = analyzer.SerializePacket(methodCallDto);
            var cMethodCallDto = (MethodCallDto)analyzer.DeserializePacket(bytes);

            assertMethodCallEqual(methodCallDto, cMethodCallDto);
        }

        [Test]
        public void MethodCallSerializes_Optimized_NoCallId()
        {
            var analyzer = new PacketAnalyzer(getMockOptimizer(), _methodCallFactory, _methodResponseFactory);

            var methodCallDto = new MethodCallDto
            {
                CallId = null,
                ClassTarget = "Target",
                Method = "TargetMethod"
            };

            var bytes = analyzer.SerializePacket(methodCallDto);
            var cMethodCallDto = (MethodCallDto)analyzer.DeserializePacket(bytes);

            assertMethodCallEqual(methodCallDto, cMethodCallDto);
        }

        [Test]
        public void MethodResponseSerializes()
        {
            var analyzer = new PacketAnalyzer(_noOptimizer, _methodCallFactory, _methodResponseFactory);

            var methodResponseDto = new MethodResponseDto
            {
                ResultData = new ReadOnlyMemory<byte>(_randomData),
                CallId = 2
            };

            var bytes = analyzer.SerializePacket(methodResponseDto);
            var cMethodResponseDto = (MethodResponseDto)analyzer.DeserializePacket(bytes);

            assertMethodResponseEqual(methodResponseDto, cMethodResponseDto);
        }

        [Test]
        public void MethodResponseSerializes_NoResult()
        {
            var analyzer = new PacketAnalyzer(_noOptimizer, _methodCallFactory, _methodResponseFactory);

            var methodResponseDto = new MethodResponseDto
            {
                CallId = 2
            };

            var bytes = analyzer.SerializePacket(methodResponseDto);
            var cMethodResponseDto = (MethodResponseDto)analyzer.DeserializePacket(bytes);

            assertMethodResponseEqual(methodResponseDto, cMethodResponseDto);
        }

        [Test]
        public void MethodResponseSerializes_InvalidCall()
        {
            var analyzer = new PacketAnalyzer(_noOptimizer, _methodCallFactory, _methodResponseFactory);

            var methodResponseDto = new MethodResponseDto
            {
                CallId = 2,
                Exception = new Exceptions.ExceptionContainer
                {
                    Message = "Error",
                    Type = Exceptions.ExceptionType.InvalidCall
                }
            };

            var bytes = analyzer.SerializePacket(methodResponseDto);
            var cMethodResponseDto = (MethodResponseDto)analyzer.DeserializePacket(bytes);

            assertMethodResponseEqual(methodResponseDto, cMethodResponseDto);
        }

        [Test]
        public void MethodResponseSerializes_CallFailed()
        {
            var analyzer = new PacketAnalyzer(_noOptimizer, _methodCallFactory, _methodResponseFactory);

            var methodResponseDto = new MethodResponseDto
            {
                CallId = 2,
                Exception = new Exceptions.ExceptionContainer
                {
                    Message = "Error",
                    Type = Exceptions.ExceptionType.CallFailed
                }
            };

            var bytes = analyzer.SerializePacket(methodResponseDto);
            var cMethodResponseDto = (MethodResponseDto)analyzer.DeserializePacket(bytes);

            assertMethodResponseEqual(methodResponseDto, cMethodResponseDto);
        }

        static IInternalThothOptimizer getMockOptimizer()
        {
            var optimizer = new Mock<IInternalThothOptimizer>();

            optimizer.Setup(opt => opt.IsOptimized).Returns(true);
            optimizer.Setup(opt => opt.GetRecFromId(3)).Returns(new MethodTargetOptRec("Target", "TargetMethod"));
            optimizer.Setup(opt => opt.GetIdFromTargetMethod("Target", "TargetMethod")).Returns(3);

            return optimizer.Object;
        }

        static void assertMethodResponseEqual(MethodResponseDto a, MethodResponseDto b)
        {
            Assert.Multiple(() =>
            {
                Assert.That(a.CallId, Is.EqualTo(b.CallId));
                Assert.That(a.Exception?.Message, Is.EqualTo(b.Exception?.Message));
                Assert.That(a.Exception?.Type, Is.EqualTo(b.Exception?.Type));
                Assert.That(a.ResultData.GetValueOrDefault().ToArray(),
                    Is.EquivalentTo(b.ResultData.GetValueOrDefault().ToArray()));
            });
        }

        static void assertMethodCallEqual(MethodCallDto a, MethodCallDto b)
        {
            Assert.Multiple(() =>
            {
                Assert.That(a.CallId, Is.EqualTo(b.CallId));
                Assert.That(a.ClassTarget, Is.EqualTo(b.ClassTarget));
                Assert.That(a.Method, Is.EqualTo(b.Method));
                Assert.That(a.ArgumentsData, Has.Count.EqualTo(b.ArgumentsData.Count));
                
                for (int i = 0; i < a.ArgumentsData.Count; i++)
                {
                    var aArg = a.ArgumentsData[i];
                    var bArg = b.ArgumentsData[i];

                    Assert.That(aArg.ToArray(),
                        Is.EquivalentTo(bArg.ToArray()));
                }
            });
        }
    }
}
