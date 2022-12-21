using ThothRpc.Attributes;

namespace ThothRpc.Tests.TestHelpers
{
    public interface ITestServerService
    {
        [ThothMethod]
        void BasicParamsNoReturn(string firstArg, bool secondArg);

        [ThothMethod]
        int BasicParamsReturnsValueType(string firstArg);

        [ThothMethod]
        string BasicParamsReturnRefType(DateTime firstArg);

        [ThothMethod]
        void NoParamsNoReturn();

        [ThothMethod]
        Task NoReturnAsync(string value);

        [ThothMethod]
        Task<string> ReturnAsync(string value);

        [ThothMethod]
        PayloadObj PayloadTest(PayloadObj payload);
    }
}