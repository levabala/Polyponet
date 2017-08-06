using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polyponet.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Polyponet.Classes.Tests
{
    [TestClass()]
    public class NodeTests
    {
        [TestMethod()]
        public void NodeInstanceTest()
        {
            Node n1 = new Node();
            Node n2 = new Node();

            byte[] token = Encoding.ASCII.GetBytes("testToken");
            if (!checkSign(token, n1.verifyMyself(token), n1.publicRSA))
                Assert.Fail();
            
            n1.requestTrust(n2);

            Assert.IsTrue(n1.trustedNodes.ContainsKey(n2.deviceId) && n2.trustedNodes.ContainsKey(n1.deviceId));
        }        

        private bool checkSign(byte[] token, byte[] sign, RSAParameters publicRSA)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(Node.KEY_SIZE);
            csp.ImportParameters(publicRSA);
            
            return csp.VerifyData(token, new SHA256CryptoServiceProvider(), sign);
        }
    }
}