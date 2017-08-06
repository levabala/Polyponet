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
        public void ChunksMergingTest()
        {
            Node n1 = new Node();
            byte[] data = new byte[] { 9, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4 };
            DataChunk d1 = n1.generateDataChunk(data, 2, 9);
            DataChunk d2 = n1.generateDataChunk(data, 0, 3);
            DataChunk d3 = n1.generateDataChunk(data, 10, 11);
            DataChunk d4 = n1.generateDataChunk(data, 16, 17);
            DataChunk d5 = n1.generateDataChunk(data, 17, 19);

            List<DataChunk> chunks = new List<DataChunk>() { d1, d2, d3, d4, d5 };

            byte[] data2 = n1.combineChunks(chunks);
        }

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
            byte[] rawData1 = new byte[] { 0, 12, 48, 33, 13, 100 };
            byte[] rawData2 = new byte[] { 1, 1, 1, 0, 0, 0 };
            byte[] hash1 = Node.getHashAlgorithm().ComputeHash(rawData1);
            byte[] hash2 = Node.getHashAlgorithm().ComputeHash(rawData2);
            int byteIndex1 = 1;
            int byteIndex2 = 3;
            byte[] chunk1 = rawData1.Take(byteIndex1).ToArray();
            byte[] chunk2 = rawData1.Skip(byteIndex1).Take(byteIndex2-byteIndex1).ToArray();
            byte[] chunk3 = rawData1.Skip(byteIndex2).Take(rawData1.Length - byteIndex2 - 1).ToArray();

            //without any encryption
            {
                DataChunk d1 = n1.generateDataChunk(rawData2);
                DataChunk d2 = n1.generateDataChunk(rawData1, hash1, 0, byteIndex1);
                DataChunk d3 = n1.generateDataChunk(rawData1, hash1, byteIndex1, byteIndex2);
                DataChunk d4 = n1.generateDataChunk(rawData1, hash1, chunk3, byteIndex2, rawData1.Length - 1);

                Node n3 = new Node();
                Assert.IsTrue(n1.sendToDirect(n2, d1) && n1.sendToDirect(n2, d2) && n1.sendToDirect(n2, d3));

                List<DataChunk> chunks = n2.chunks[hash2];
                Assert.AreEqual(d1, chunks[0]);//n2.chunks[hash2][0]);
                Assert.AreEqual(d2, n2.chunks[hash1][0]);
                Assert.AreEqual(d3, n2.chunks[hash1][1]);
                Assert.AreNotEqual(d4, n2.chunks[hash1][1]);

                Assert.IsFalse(n1.sendToDirect(n3, d1));
            }

            n1.resetChunksStorage();
            n2.resetChunksStorage();            

            //now ENCRYPT
            {
                DataChunk d1 = n1.generateDataChunk(rawData2, true);
                DataChunk d2 = n1.generateDataChunk(rawData1, hash1, 0, byteIndex1, true);
                DataChunk d3 = n1.generateDataChunk(rawData1, hash1, byteIndex1, byteIndex2, true);
                DataChunk d4 = n1.generateDataChunk(rawData1, hash1, chunk3, byteIndex2, rawData1.Length - 1, true);

                Node n3 = new Node();
                Assert.IsTrue(n1.sendToDirect(n2, d1) && n1.sendToDirect(n2, d2) && n1.sendToDirect(n2, d3));

                List<DataChunk> chunks = n2.chunks[hash2];
                Assert.AreEqual(d1, chunks[0]);//n2.chunks[hash2][0]);
                Assert.AreEqual(d2, n2.chunks[hash1][0]);
                Assert.AreEqual(d3, n2.chunks[hash1][1]);
                Assert.AreNotEqual(d4, n2.chunks[hash1][1]);

                Assert.IsFalse(n1.sendToDirect(n3, d1));

                //chunks getting 
                //n3.requestChunk(n2, hash1);
            }
        }                
    }
}