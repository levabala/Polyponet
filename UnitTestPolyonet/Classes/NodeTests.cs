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

            //verifying 
            byte[] token = Encoding.ASCII.GetBytes("testToken");
            if (!Node.verifyData(n1.publicRSA, token, n1.verifyMyself(token)))
                Assert.Fail();
            
            //add as trusted
            n1.requestTrust(n2);
            Assert.IsTrue(n1.trustedNodes.ContainsKey(n2.deviceId) && n2.trustedNodes.ContainsKey(n1.deviceId));

            //data sending 
            byte[] rawData = new byte[] { 0, 12, 48, 33, 13, 100 };
            byte[] hash = Node.getHashAlgorithm().ComputeHash(rawData);
            int byteIndex1 = 1;
            int byteIndex2 = 3;
            byte[] chunk1 = rawData.Take(byteIndex1).ToArray();
            byte[] chunk2 = rawData.Skip(byteIndex1).Take(byteIndex2-byteIndex1).ToArray();
            byte[] chunk3 = rawData.Skip(byteIndex2).Take(rawData.Length - byteIndex2 - 1).ToArray();

            DataChunk d1 = n1.generateDataChunk(rawData);
            DataChunk d2 = n1.generateDataChunk(rawData, hash, 0, byteIndex1);
            DataChunk d3 = n1.generateDataChunk(rawData, hash, byteIndex1, byteIndex2);
            DataChunk d4 = n1.generateDataChunk(rawData, hash, chunk3, byteIndex2, rawData.Length-1);

            Node n3 = new Node();
            Assert.IsTrue(n1.sendToDirect(n2, d1) && n1.sendToDirect(n2, d2));
            Assert.AreEqual(d1, n2.chunks[d1.hashOrigin]);
            Assert.AreEqual(d2, n2.chunks[d2.hashOrigin]);

            Assert.IsFalse(n1.sendToDirect(n3, d1));            
        }                
    }
}