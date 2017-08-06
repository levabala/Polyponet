using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Polyponet.Classes
{
    public class Node
    {
        public static readonly int RSA_KEY_SIZE = 2048;
        public static readonly int AES_KEY_SIZE = 192;
        public static readonly bool USE_AOEP = true;
        public static readonly string HASH_ALGORITHM_NAME = "SHA256";
        public static HashAlgorithm getHashAlgorithm() { return new SHA256CryptoServiceProvider(); }

        public byte[] deviceId = Guid.NewGuid().ToByteArray();
        public RSAParameters publicRSA;
        public bool online = true;

        //temporary public

        public RSAParameters privateRSA;
        public Dictionary<byte[], Node> trustedNodes = new Dictionary<byte[], Node>(new ByteArrayComparer());
        public Dictionary<byte[], Node> familiarNodes = new Dictionary<byte[], Node>(new ByteArrayComparer());
        public Dictionary<byte[], Node> knownNodes = new Dictionary<byte[], Node>(new ByteArrayComparer());
        public Dictionary<byte[], List<DataChunk>> chunks = new Dictionary<byte[], List<DataChunk>>(new ByteArrayComparer());

        class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] left, byte[] right)
            {
                if (left == null || right == null)
                    return left == right;
                if (left.Length != right.Length)
                    return false;
                for (int i = 0; i < left.Length; i++)
                    if (left[i] != right[i])
                        return false;
                return true;
            }
            public int GetHashCode(byte[] key)
            {
                if (key == null)
                    throw new ArgumentNullException("key");
                int sum = 0;
                foreach (byte cur in key)
                    sum += cur;
                return sum;
            }
        }

        //local variables 
        RSACryptoServiceProvider RSAProvider = new RSACryptoServiceProvider(RSA_KEY_SIZE);
        AesCryptoServiceProvider AESProvider = new AesCryptoServiceProvider();
        HashAlgorithm HashProvider = new SHA256CryptoServiceProvider();                

        public Node()
        {
            AESProvider.KeySize = AES_KEY_SIZE;

            publicRSA = RSAProvider.ExportParameters(false);
            privateRSA = RSAProvider.ExportParameters(true);            
        }

        public Node(RSAParameters publicRSA, RSAParameters privateRSA)
        {
            AESProvider.KeySize = AES_KEY_SIZE;

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

        public void resetChunksStorage()
        {            
            chunks = new Dictionary<byte[], List<DataChunk>>(new ByteArrayComparer());
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

        public bool shareFile(DataChunk data, Action<byte[]> storingAccepted)
        {            
            return false;
        }        

        public List<DataChunk> getChunk(byte[] hash)
        {
            if (!online) return null;

            if (!chunks.ContainsKey(hash))
                return new List<DataChunk>();

            return chunks[hash];
        }

        public List<DataChunk> getChunk(byte[] hash, int startByte, int endByte)
        {
            if (!online) return null;

            if (!chunks.ContainsKey(hash))
                return new List<DataChunk>();

            return chunks[hash].Where((chunk) => { return endByte >= chunk.startByte && chunk.endByte >= startByte; }).ToList();
        }

        public bool requestChunk(Node n, byte[] hash)
        {
            List<DataChunk> chunks = n.getChunk(hash);
            if (chunks == null || chunks.Count == 0) return false;

            combineChunks(chunks);            

            return false;
        }

        public byte[] combineChunks(List<DataChunk> chunks)
        {
            int minStartByte = chunks.Min((chunk) => { return chunk.startByte; });
            int maxStartByte = chunks.Max((chunk) => { return chunk.startByte; });
            int minEndByte = chunks.Min((chunk) => { return chunk.endByte; });
            int maxEndByte = chunks.Max((chunk) => { return chunk.endByte; });

            int[] startByteArr = new int[chunks.Count];
            int[] endByteArr = new int[chunks.Count];
            for (int i = 0; i < startByteArr.Length; i++)
                startByteArr[i] = endByteArr[i] = i;

            Array.Sort(startByteArr, (i1, i2) => { return chunks[i1].startByte.CompareTo(chunks[i2].startByte); });
            Array.Sort(endByteArr, (i1, i2) => { return chunks[i1].endByte.CompareTo(chunks[i2].endByte); });

            int maxEndByteFilled = minEndByte;
            byte[] data = new byte[maxEndByte + 1];
            foreach (int index in startByteArr)
            {
                DataChunk chunk = chunks[index];
                chunk.data.CopyTo(data, chunk.startByte);

                if (chunk.endByte > maxEndByteFilled) maxEndByteFilled = chunk.endByte;
            }

            endByteArr.Reverse();
            chunks[endByteArr[0]].data.CopyTo(data, chunks[endByteArr[0]].startByte);
            for (int index = 1; index < chunks.Count && maxEndByteFilled < chunks[index].startByte; index++)
                chunks[index].data.CopyTo(data, chunks[index].startByte);

            return data;
        }

        #region DataChunk generators
        public DataChunk generateDataChunk(byte[] data, bool encrypt = false)
        {
            byte[] hashOrigin = HashProvider.ComputeHash(data);

            DataChunk chunk = new DataChunk(data, null, null);

            if (encrypt)
            {
                encryptDataChunk(ref chunk);                
            }
            else
            {                
                byte[] sign = RSAProvider.SignHash(hashOrigin, HASH_ALGORITHM_NAME);
                chunk.hash = hashOrigin;
                chunk.sign = sign;
            }

            chunk.hashOrigin = hashOrigin;

            return chunk;
        }

        public DataChunk generateDataChunk(byte[] data, byte[] hashOrigin, bool encrypt = false)
        {                        
            DataChunk chunk = new DataChunk(data, null, null);

            if (encrypt)
                encryptDataChunk(ref chunk);
            else
            {                
                byte[] sign = RSAProvider.SignHash(hashOrigin, HASH_ALGORITHM_NAME);                
                chunk.sign = sign;
            }

            chunk.hashOrigin = hashOrigin;

            return chunk;
        }

        public DataChunk generateDataChunk(byte[] dataOrigin, int startByte, int endByte, bool encrypt = false)
        {
            byte[] data = dataOrigin.Skip(startByte).Take(endByte - startByte + 1).ToArray();            
            byte[] hashOrigin = HashProvider.ComputeHash(dataOrigin);            

            DataChunk chunk = new DataChunk(data, hashOrigin, null, null, startByte, endByte);

            if (encrypt)
                encryptDataChunk(ref chunk);
            else
            {
                byte[] hash = HashProvider.ComputeHash(data);
                byte[] sign = RSAProvider.SignHash(hash, HASH_ALGORITHM_NAME);
                chunk.hash = hash;
                chunk.sign = sign;
            }

            return chunk;
        }

        public DataChunk generateDataChunk(
            byte[] dataOrigin, byte[] hashOrigin, int startByte, int endByte, bool encrypt = false)
        {
            byte[] data = dataOrigin.Skip(startByte).Take(endByte - startByte + 1).ToArray();            

            DataChunk chunk = new DataChunk(data, hashOrigin, null, null, startByte, endByte);

            if (encrypt)
                encryptDataChunk(ref chunk);
            else
            {
                byte[] hash = HashProvider.ComputeHash(data);
                byte[] sign = RSAProvider.SignHash(hash, HASH_ALGORITHM_NAME);
                chunk.hash = hash;
                chunk.sign = sign;
            }

            return chunk;
        }

        public DataChunk generateDataChunk(
            byte[] dataOrigin, byte[] hashOrigin, byte[] data, int startByte, int endByte, bool encrypt = false)
        {                        
            DataChunk chunk = new DataChunk(data, hashOrigin, null, null, startByte, endByte);

            if (encrypt)
                encryptDataChunk(ref chunk);
            else
            {
                byte[] hash = HashProvider.ComputeHash(data);
                byte[] sign = RSAProvider.SignHash(hash, HASH_ALGORITHM_NAME);
                chunk.hash = hash;
                chunk.sign = sign;
            }

            return chunk;
        }
        #endregion

        private void encryptDataChunk(ref DataChunk chunk)
        {
            AESProvider.GenerateKey();
            AESProvider.GenerateIV();
            chunk.key = AESProvider.Key;
            chunk.IV = AESProvider.IV;

            //let's encrypt data 
            ICryptoTransform encryptor = AESProvider.CreateEncryptor();                        
            using (MemoryStream msEncrypt = new MemoryStream())            
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))                                            
                        swEncrypt.Write(chunk.data);                    
                    chunk.data = msEncrypt.ToArray();
                }

            //now encrypt AES key
            chunk.key = RSAProvider.Encrypt(chunk.key, USE_AOEP);

            //and update hash&sign
            chunk.hash = HashProvider.ComputeHash(chunk.data);
            chunk.sign = RSAProvider.SignHash(chunk.hash, HASH_ALGORITHM_NAME);
        }

        #region Data Sign Verifiers 
        public static bool verifyData(RSAParameters publicRSA, byte[] data, byte[] sign)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(RSA_KEY_SIZE);
            csp.ImportParameters(publicRSA);

            return csp.VerifyData(data, getHashAlgorithm(), sign);
        }

        public static bool verifyData(RSAParameters publicRSA, byte[] data, byte[] hash, byte[] sign)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(RSA_KEY_SIZE);
            csp.ImportParameters(publicRSA);

            HashAlgorithm hashProvider = getHashAlgorithm();
            byte[] computedHash = hashProvider.ComputeHash(data);

            if (computedHash != hash)
                return false;

            return csp.VerifyHash(hash, HASH_ALGORITHM_NAME, sign);
        }

        public static bool verifyData(RSAParameters publicRSA, DataChunk chunk)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(RSA_KEY_SIZE);
            csp.ImportParameters(publicRSA);

            HashAlgorithm hashProvider = getHashAlgorithm();            

            return csp.VerifyData(chunk.data, hashProvider, chunk.sign);
        }
#endregion
    }

    public struct DataChunk
    {
        public byte[] data, hashOrigin, hash, sign, key, IV;
        public int startByte, endByte; 

        public DataChunk(
            byte[] data, byte[] hashOrigin, byte[] hash, byte[] sign, 
            int startByte, int endByte, byte[] key = null, byte[] IV = null)
        {
            this.data = data;
            this.hashOrigin = hashOrigin;
            this.hash = hash;
            this.sign = sign;
            this.startByte = startByte;
            this.endByte = endByte;
            this.key = key;
            this.IV = IV;
        }

        public DataChunk(byte[] data, byte[] hash, byte[] sign, byte[] key = null, byte[] IV = null)
        {
            this.data = data;
            this.hash = hashOrigin = hash;
            this.sign = sign;
            this.key = key;
            this.IV = IV;
            startByte = 0;
            endByte = data.Length - 1;            
        }
    }
}
