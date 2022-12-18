using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;
using ThothRpc.Exceptions;
using ThothRpc.MessagePack;
using ThothRpc.Models.Dto;
using ThothRpc.Optimizer;
using ThothRpc.Tests.Models;
using ThothRpc.Tests.TestHelpers;

namespace ThothRpc.Tests.Unit
{
    [TestFixture]
    public class ServerHubTests
    {
        MockServerClient _serverMock;
        ServerHub _defaultHub;
        IServerDelegator _serverDelegator;
        TestService _testService;
        PeerInfo _nominalPeerInfo;
        PacketAnalyzer _packetAnalyzer;
        SerializeObject _serializeObject;
        DeserializeObject _deserializeObject;
        IInternalThothOptimizer _noOptimizer = Mock.Of<IInternalThothOptimizer>(o => o.IsOptimized == false);

        [SetUp]
        public void SetUp()
        {
            _serverMock = new MockServerClient();
            _defaultHub = ServerHubBuilder.BuildServer()
                .UseMessagePack()
                .UseTransport(_serverMock)
                .Build();

            _serializeObject = _defaultHub.Config.ObjectSerializer;
            _deserializeObject = _defaultHub.Config.ObjectDeserializer;

            _packetAnalyzer = new PacketAnalyzer(_noOptimizer, () => new MethodCallDto(), () => new MethodResponseDto());

            _serverDelegator = _defaultHub;
            _testService = new TestService();

            _defaultHub.Register(_testService);

            _nominalPeerInfo = new PeerInfo
            {
                PeerId = 1,
                RemoteEndpoint = IPEndPoint.Parse("127.0.0.1:40"),
                UnderlyingConnection = new object()
            };
        }

        [TearDown]
        public void TearDown()
        {
            _defaultHub.Dispose();
        }

        //[Test]
        //public async Task Called_InjectsPeerInfo()
        //{
        //    await _serverDelegator.OnObjectReceivedAsync(_nominalPeerInfo, new MethodCallDto
        //    {
        //        CallId = 1,
        //        ClassTarget = nameof(TestService),
        //        Method = nameof(TestService.PeerInfoInMethod),
        //        ArgumentsData = new List<object> { "sauce" }
        //    });

        //    _serverMock.Verify(s => s.SendObj(1, DeliveryMode.ReliableOrdered,
        //        It.Is<MethodResponseDto>(m => m.CallId == 1 && m.Result == _nominalPeerInfo)));
        //}

        [Test]
        public async Task Invokes_WithExpression()
        {
            //_serverDelegator.
            //var returnVal = await _defaultHub.InvokeClientAsync<string, TestService>(1, t => t.GetFavColor("Red", true));

            //var methodCallProxy = MethodCallProxy.DeserializeFrom(_serverMock.DataSent, _packetAnalyzer,
            //    _deserializeObject, typeof(string), typeof(bool));

            //Assert.That(_serverMock.DataSentDeliveryMode, Is.EqualTo(DeliveryMode.ReliableOrdered));
        }



        //[Test]
        //public async Task Called_ParametersAndReturned()
        //{
        //    await _serverDelegator.OnObjectReceivedAsync(_nominalPeerInfo, new MethodCallDto
        //    {
        //        CallId = 1,
        //        ClassTarget = nameof(TestService),
        //        Method = "GetFavColor",
        //        ArgumentsData = new List<object> { "Blue", true }
        //    });

        //    _serverMock.Verify(s => s.SendObj(1, DeliveryMode.ReliableOrdered,
        //        It.Is<MethodResponseDto>(m => m.CallId == 1 && m.Result.GetType() == typeof(string))));
        //}

        //[Test]
        //public async Task Called_NoParameters()
        //{
        //    await _serverDelegator.OnObjectReceivedAsync(_nominalPeerInfo, new MethodCallDto
        //    {
        //        CallId = 1,
        //        ClassTarget = nameof(TestService),
        //        Method = "MethodNoReturnNoParams"
        //    });

        //    _serverMock.Verify(s => s.SendObj(1, DeliveryMode.ReliableOrdered,
        //        It.Is<MethodResponseDto>(m => m.CallId == 1 && m.Result == null)));

        //    Assert.That(_testService.MethodCalled, Is.True);
        //}

        //[Test]
        //public async Task Called_Async_Result()
        //{
        //    await _serverDelegator.OnObjectReceivedAsync(_nominalPeerInfo, new MethodCallDto
        //    {
        //        CallId = 1,
        //        ClassTarget = nameof(TestService),
        //        Method = nameof(TestService.AsyncMethodReturnsStr)
        //    });

        //    _serverMock.Verify(s => s.SendObj(1, DeliveryMode.ReliableOrdered,
        //        It.Is<MethodResponseDto>(m => m.CallId == 1 && m.Result.GetType() == typeof(string))));

        //    Assert.That(_testService.MethodCalled, Is.True);
        //}

        //[Test]
        //public async Task Called_Async_No_Result()
        //{
        //    await _serverDelegator.OnObjectReceivedAsync(_nominalPeerInfo, new MethodCallDto
        //    {
        //        CallId = 1,
        //        ClassTarget = nameof(TestService),
        //        Method = "AsyncMethodNoReturn"
        //    });

        //    _serverMock.Verify(s => s.SendObj(1, DeliveryMode.ReliableOrdered,
        //        It.Is<MethodResponseDto>(m => m.CallId == 1 && m.Result == null)));

        //    Assert.That(_testService.MethodCalled, Is.True);
        //}

        //[Test]
        //public async Task Called_ThrowsInvalidCall_NonExistentMethod()
        //{
        //    await _serverDelegator.OnObjectReceivedAsync(_nominalPeerInfo, new MethodCallDto
        //    {
        //        CallId = 1,
        //        ClassTarget = nameof(TestService),
        //        Method = "IDontExist"
        //    });

        //    _serverMock.Verify(s => s.SendObj(1, DeliveryMode.ReliableOrdered,
        //        It.Is<MethodResponseDto>(m => m.Exception.Type == ExceptionType.InvalidCall)));
        //}

        //[Test]
        //public async Task Called_ThrowsInvalidCall_WrongParameterCount()
        //{
        //    await _serverDelegator.OnObjectReceivedAsync(_nominalPeerInfo, new MethodCallDto
        //    {
        //        CallId = 1,
        //        ClassTarget = nameof(TestService),
        //        Method = "GetFavColor",
        //        ArgumentsData = new List<object> { "Blue", true, "Cat" }
        //    });

        //    _serverMock.Verify(s => s.SendObj(1, DeliveryMode.ReliableOrdered,
        //        It.Is<MethodResponseDto>(m => m.Exception.Type == ExceptionType.InvalidCall)));
        //}

        //[Test]
        //public async Task Called_ThrowsInvalidCall_WrongParameterType()
        //{
        //    await _serverDelegator.OnObjectReceivedAsync(_nominalPeerInfo, new MethodCallDto
        //    {
        //        CallId = 1,
        //        ClassTarget = nameof(TestService),
        //        Method = "GetFavColor",
        //        ArgumentsData = new List<object> { true, true, "Cat" }
        //    });

        //    _serverMock.Verify(s => s.SendObj(1, DeliveryMode.ReliableOrdered,
        //        It.Is<MethodResponseDto>(m => m.Exception.Type == ExceptionType.InvalidCall)));
        //}

        //[Test]
        //public async Task Called_ThrowsInvalidCall_InvalidTarget()
        //{
        //    await _serverDelegator.OnObjectReceivedAsync(_nominalPeerInfo, new MethodCallDto
        //    {
        //        CallId = 1,
        //        ClassTarget = "BadTarget",
        //        Method = "GetFavColor",
        //        ArgumentsData = new List<object> { "Blue", true }
        //    });

        //    _serverMock.Verify(s => s.SendObj(1, DeliveryMode.ReliableOrdered,
        //        It.Is<MethodResponseDto>(m => m.Exception.Type == ExceptionType.InvalidCall)));
        //}
    }
}
