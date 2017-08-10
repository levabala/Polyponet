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
        public void ChunksCreateTest()
        {
            byte[] rawData = {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 0, 0, 0,
                1, 1, 1, 1,
                2, 2, 2, 2 };

            Random rnd = new Random();
            byte[] bigRawData = new byte[2048];
            rnd.NextBytes(bigRawData);

            Node n = new Node();
            DataInstance dataInstance = n.generateDataInstance(rawData);

            DataInstance dataInstanceEncrypted = n.encryptData(dataInstance, 2);
            List<DataChunk> chunks = n.generateDataChunks(dataInstanceEncrypted, 3);
            //now we have some encrypted&signed chunks

            DataChunk mergedChunk = n.combineChunks(chunks);
            DataInstance? dataInstance2 = n.chunkToInstance(mergedChunk);
            if (!dataInstance2.HasValue)
                Assert.Fail();

            DataInstance decryptedData = n.decryptData(dataInstance2.Value);
            Assert.IsTrue(
                decryptedData.data.SequenceEqual(dataInstance.data) && 
                decryptedData.hash.SequenceEqual(dataInstance.hash) &&
                decryptedData.sign.SequenceEqual(dataInstance.sign));
        }

        [TestMethod()]
        public void ChunksTest()
        {
            Node originNode = new Node();
            List<Node> friendlyNodes = new List<Node>();
            for (int i = 0; i < 10; i++)
            {
                Node n = new Node();
                n.giveTrust(originNode);
                friendlyNodes.Add(n);
            }


        }                
    }
}