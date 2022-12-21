using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;
using ThothRpc.Exceptions;
using ThothRpc.MessagePack;
using ThothRpc.Tests.TestHelpers;

namespace ThothRpc.Tests.Unit
{
    [TestFixture(true)]
    [TestFixture(false)]
    public class ClientToServerTests
    {
        bool _local;

        ServerHub _serverHub;
        ClientHub _clientHub;

        Mock<ITestClientService> _clientServiceMock;
        Mock<ITestServerService> _serverServiceMock;

        public ClientToServerTests(bool isLocal) 
        { 
            _local = isLocal;
        }

        [SetUp]
        public void Setup()
        {
            var server = new TestServer();
            var client = new TestClient();

            server.AddClient(client);

            _serverHub = ServerHubBuilder.BuildServer()
                .UseTransport(server)
                .WithConfiguration(c => c.RequestTimeout = TimeSpan.FromSeconds(1))
                .UseMessagePack()
                .Build();

            var clientBuilder = ClientHubBuilder.BuildClient()
                .WithConfiguration(c => c.RequestTimeout = TimeSpan.FromSeconds(1))
                .UseMessagePack();

            if (_local)
            {
                clientBuilder.UseLocalServer(_serverHub);
            }
            else
            {
                clientBuilder.UseTransport(client);
            }

            _clientHub = clientBuilder.Build();

            _clientServiceMock = new Mock<ITestClientService>();
            _serverServiceMock = new Mock<ITestServerService>();

            _clientHub.RegisterAs(_clientServiceMock.Object);
            _serverHub.RegisterAs(_serverServiceMock.Object);
        }

        [Test]
        public async Task Client_To_Server_NoParams_NoReturn()
        {
            await _clientHub.InvokeServerAsync<ITestServerService>(s => s.NoParamsNoReturn());
            _serverServiceMock.Verify(s => s.NoParamsNoReturn());
        }

        [Test]
        public async Task Client_To_Server_BasicParams_NoReturn()
        {
            await _clientHub.InvokeServerAsync<ITestServerService>
                (s => s.BasicParamsNoReturn("hello", true));

            _serverServiceMock.Verify(s => s.BasicParamsNoReturn("hello", true));
        }

        [Test]
        public async Task Client_To_Server_BasicParams_Returns()
        {
            _serverServiceMock.Setup(s => s.BasicParamsReturnsValueType("hello"))
                .Returns(5);

            var value = await _clientHub.InvokeServerAsync<ITestServerService, int>
                (s => s.BasicParamsReturnsValueType("hello"));

            _serverServiceMock.Verify(s => s.BasicParamsReturnsValueType("hello"));
            Assert.That(value, Is.EqualTo(5));
        }

        [Test]
        public async Task Client_To_Server_BasicParams_Returns_Null_ValueType()
        {
            _serverServiceMock.Setup(s => s.BasicParamsReturnsValueType("hello"))
                .Returns(null);

            var value = await _clientHub.InvokeServerAsync<ITestServerService, int>
                (s => s.BasicParamsReturnsValueType("hello"));

            _serverServiceMock.Verify(s => s.BasicParamsReturnsValueType("hello"));
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public async Task Client_To_Server_BasicParams_Returns_Null_ReferenceType()
        {
            var now = DateTime.UtcNow;
            _serverServiceMock.Setup(s => s.BasicParamsReturnRefType(now))
                .Returns(() => null);

            var value = await _clientHub.InvokeServerAsync<ITestServerService, string>
                (s => s.BasicParamsReturnRefType(now));

            _serverServiceMock.Verify(s => s.BasicParamsReturnRefType(now));
            Assert.That(value, Is.EqualTo(null));
        }

        [Test]
        public async Task Client_To_Server_BasicParams_WithNull_NoReturn()
        {
            await _clientHub.InvokeServerAsync<ITestServerService>
                (s => s.BasicParamsNoReturn(null, true));

            _serverServiceMock.Verify(s => s.BasicParamsNoReturn(null, true));
        }

        [Test]
        public async Task Client_To_Server_Async_Return()
        {
            _serverServiceMock.Setup(s => s.ReturnAsync("hello"))
                .ReturnsAsync("hello");

            var value = await _clientHub.InvokeServerAsync<string>
                (typeof(ITestServerService).FullName, nameof(ITestServerService.ReturnAsync), "hello");

            _serverServiceMock.Verify(s => s.ReturnAsync("hello"));
            Assert.That(value, Is.EqualTo("hello"));
        }

        [Test]
        public async Task Client_To_Server_Async_NoReturn()
        {
            _serverServiceMock.Setup(s => s.NoReturnAsync("hello"));

            var value = await _clientHub.InvokeServerAsync<string>
                (typeof(ITestServerService).FullName, nameof(ITestServerService.NoReturnAsync), "hello");

            _serverServiceMock.Verify(s => s.NoReturnAsync("hello"));
            Assert.That(value, Is.EqualTo(null));
        }

        [Test]
        public async Task Client_To_Server_Payload()
        {
            _serverServiceMock.Setup(s => s.PayloadTest(It.IsAny<PayloadObj>()))
                .Returns(new PayloadObj { Age = 3, Name = "Bob" });

            var payloadObj = new PayloadObj
            {
                Age = 3,
                Name = "Tim"
            };

            var value = await _clientHub.InvokeServerAsync<ITestServerService, PayloadObj>
                (s => s.PayloadTest(payloadObj));

            _serverServiceMock.Verify(s => s.PayloadTest(It.IsAny<PayloadObj>()));
            Assert.That(value.Age, Is.EqualTo(3));
            Assert.That(value.Name, Is.EqualTo("Bob"));
        }

        [Test]
        public void Client_To_Server_Timesout()
        {
            _serverServiceMock.Setup(s => s.ReturnAsync("hello"))
                .ReturnsAsync("hello", TimeSpan.FromSeconds(1.2d));

            AsyncTestDelegate testDelegate = async () => await _clientHub.InvokeServerAsync<string>
                (typeof(ITestServerService).FullName, nameof(ITestServerService.ReturnAsync), "hello");

            Assert.That(testDelegate, Throws.InstanceOf<TimeoutException>());
            _serverServiceMock.Verify(s => s.ReturnAsync("hello"));
        }

        [Test]
        public void Client_To_Server_InvalidCall_WrongParamCount()
        {
            AsyncTestDelegate testDelegate = async () => await _clientHub.InvokeServerAsync<string>
                (typeof(ITestServerService).FullName, nameof(ITestServerService.ReturnAsync), "hello", "hello");

            Assert.That(testDelegate, Throws.InstanceOf<InvalidCallException>());
            _serverServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Client_To_Server_InvalidCall_MissingMethod()
        {
            AsyncTestDelegate testDelegate = async () => await _clientHub.InvokeServerAsync<string>
                (typeof(ITestServerService).FullName, "badMethodName", "hello", "hello");

            Assert.That(testDelegate, Throws.InstanceOf<InvalidCallException>());
            _serverServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Client_To_Server_InvalidCall_WrongParamType()
        {
            AsyncTestDelegate testDelegate = async () => await _clientHub.InvokeServerAsync<string>
                (typeof(ITestServerService).FullName, nameof(ITestServerService.ReturnAsync), true);

            Assert.That(testDelegate, Throws.InstanceOf<Exception>());
            _serverServiceMock.VerifyNoOtherCalls();
        }
    }
}
