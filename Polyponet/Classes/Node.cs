using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Polyponet.Classes
{        
    public class Node
    {
        public static readonly int KEY_SIZE = 2048;
        public static readonly bool USE_AOEP = false;
        public static readonly string HASH_ALGORITHM_NAME = "SHA256";
        public static HashAlgorithm getHashAlgorithm() { return new SHA256CryptoServiceProvider(); }

        public byte[] deviceId = Guid.NewGuid().ToByteArray();
        public RSAParameters publicRSA;
        public bool online = true;

        //temporary public

        public RSAParameters privateRSA;
        public Dictionary<byte[], Node> trustedNodes = new Dictionary<byte[], Node>();
        public Dictionary<byte[], Node> familiarNodes = new Dictionary<byte[], Node>();
        public Dictionary<byte[], Node> knownNodes = new Dictionary<byte[], Node>();
        public Dictionary<byte[], List<DataChunk>> chunks = new Dictionary<byte[], List<DataChunk>>();

        //local variables 
        RSACryptoServiceProvider RSAProvider = new RSACryptoServiceProvider(KEY_SIZE);
        HashAlgorithm HashProvider = new SHA256CryptoServiceProvider();

        public Node()
        {            
            publicRSA = RSAProvider.ExportParameters(false);
            privateRSA = RSAProvider.ExportParameters(true);
        }

        public Node(RSAParameters publicRSA, RSAParameters privateRSA)
        {
            this.publicRSA = publicRSA;
            this.privateRSA = privateRSA;
            RSAProvider.ImportParameters(privateRSA);
        }

        public bool requestTrust(Node n)
        {
            addAsTrusted(n);
            return n.giveTrust(this);            
        }

        public bool giveTrust(Node n)
        {
            if (!online)
                return false;

            addAsTrusted(n);
            return true;
        }

        private void addAsTrusted(Node n)
        {
            if (!trustedNodes.ContainsKey(n.deviceId))
                trustedNodes.Add(n.deviceId, n);            
        }

        public byte[] verifyMyself(byte[] token)
        {
            return RSAProvider.SignData(token, HashProvider);
        }

        public bool acceptMessage(Node origin, Node transponder, DataChunk data)
        {
            if (!online || !verifyData(origin.publicRSA, data))
                return false;

            putChunk(data);
            return true;
        }

        private void putChunk(DataChunk chunk)
        {
            if (!chunks.ContainsKey(chunk.hashOrigin))
                chunks.Add(chunk.hashOrigin, new List<DataChunk> { chunk });
            else
                if (!chunks[chunk.hashOrigin].Contains(chunk)) chunks[chunk.hashOrigin].Add(chunk);
        }

        public bool sendToDirect(Node n, DataChunk data)
        {
            if (!trustedNodes.ContainsKey(n.deviceId))
                return false;

            return n.acceptMessage(this, this, data);
        }

        public bool sendByShare(Node origin, Node addressee, DataChunk data, double safeKeepingRate)
        {
            return false;
        }

        public bool sendTo(Node n, DataChunk data, Action<byte[]> storingAccepted)
        {
            if (!sendToDirect(n, data))
            {

            }

            return false;
        }

        public DataChunk generateDataChunk(byte[] data)
        {
            byte[] hash = HashProvider.ComputeHash(data);
            byte[] sign = RSAProvider.SignHash(hash, HASH_ALGORITHM_NAME);
            return new DataChunk(data, hash, sign);
        }

        public DataChunk generateDataChunk(byte[] dataOrigin, int startByte, int endByte)
        {
            byte[] data = dataOrigin.Skip(startByte).Take(endByte - startByte).ToArray();
            byte[] hash = HashProvider.ComputeHash(data);
            byte[] hashOrigin = HashProvider.ComputeHash(dataOrigin);
            byte[] sign = RSAProvider.SignHash(hash, HASH_ALGORITHM_NAME);
            return new DataChunk(data, hashOrigin, hash, sign, startByte, endByte);
        }

        public DataChunk generateDataChunk(byte[] dataOrigin, byte[] hashOrigin, int startByte, int endByte)
        {
            byte[] data = dataOrigin.Skip(startByte).Take(endByte - startByte).ToArray();
            byte[] hash = HashProvider.ComputeHash(data);            
            byte[] sign = RSAProvider.SignHash(hash, HASH_ALGORITHM_NAME);
            return new DataChunk(data, hashOrigin, hash, sign, startByte, endByte);
        }

        public DataChunk generateDataChunk(
            byte[] dataOrigin, byte[] hashOrigin, byte[] data, int startByte, int endByte)
        {            
            byte[] hash = HashProvider.ComputeHash(data);
            byte[] sign = RSAProvider.SignHash(hash, HASH_ALGORITHM_NAME);
            return new DataChunk(data, hashOrigin, hash, sign, startByte, endByte);
        }

        public static bool verifyData(RSAParameters publicRSA, byte[] data, byte[] sign)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(KEY_SIZE);
            csp.ImportParameters(publicRSA);

            return csp.VerifyData(data, getHashAlgorithm(), sign);
        }

        public static bool verifyData(RSAParameters publicRSA, byte[] data, byte[] hash, byte[] sign)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(KEY_SIZE);
            csp.ImportParameters(publicRSA);

            HashAlgorithm hashProvider = getHashAlgorithm();
            byte[] computedHash = hashProvider.ComputeHash(data);

            if (computedHash != hash)
                return false;

            return csp.VerifyHash(hash, HASH_ALGORITHM_NAME, sign);
        }

        public static bool verifyData(RSAParameters publicRSA, DataChunk chunk)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(KEY_SIZE);
            csp.ImportParameters(publicRSA);

            HashAlgorithm hashProvider = getHashAlgorithm();
            /*byte[] computedHash = HashProvider.ComputeHash(chunk.data);

            if (computedHash != chunk.hash)
                return false;*/

            return csp.VerifyData(chunk.data, hashProvider, chunk.sign);
        }
    }

    public struct DataChunk
    {
        public byte[] data, hashOrigin, hash, sign;
        public int startByte, endByte; 
        
        public DataChunk(byte[] data, byte[] hashOrigin, byte[] hash, byte[] sign, int startByte, int endByte)
        {
            this.data = data;
            this.hashOrigin = hashOrigin;
            this.hash = hash;
            this.sign = sign;
            this.startByte = startByte;
            this.endByte = endByte;
        }

        public DataChunk(byte[] data, byte[] hash, byte[] sign)
        {
            this.data = data;
            this.hash = hashOrigin = hash;
            this.sign = sign;
            startByte = 0;
            endByte = data.Length-1;
        }
    }
}
