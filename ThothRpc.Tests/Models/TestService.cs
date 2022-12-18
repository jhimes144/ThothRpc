using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Attributes;

namespace ThothRpc.Tests.Models
{
    internal class TestService
    {
        public bool MethodCalled { get; private set; }

        [ThothMethod]
        public void MethodNoReturnNoParams()
        {
            MethodCalled = true;
        }

        [ThothMethod]
        public string GetFavColor(string favColor, bool newColor)
        {
            MethodCalled = true;
            return favColor;
        }

        [ThothMethod]
        public async Task AsyncMethodNoReturn()
        {
            await Task.Delay(30);
            MethodCalled = true;
        }

        [ThothMethod]
        public async Task<string> AsyncMethodReturnsStr()
        {
            await Task.Delay(30);
            MethodCalled = true;
            return "This is the result as string";
        }

        [ThothMethod]
        public IPeerInfo PeerInfoInMethod(IPeerInfo peerInfo, string additonalArg)
        {
            return peerInfo;
        }

        public void NotAThothMethod()
        {

        }
    }
}
