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
        public int chunkSize = 5;

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

        public void putData(byte[] data, bool encrypt)
        {

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

        public List<DataChunk> requestChunks(Node n, byte[] hash)
        {
            List<DataChunk> chunks = n.getChunk(hash);
            if (chunks == null || chunks.Count == 0) return new List<DataChunk>();

            /*for (int i = 0; i < chunks.Count; i++)
                decryptDataChunk(chunks[i]);

            combineChunks(chunks);*/

            return chunks;
        }

        public DataChunk combineChunks(List<DataChunk> chunks)
        {
            /*foreach (DataChunk c in chunks)
                if (c.encryptRounds.Count > 0)                
                    throw new CryptographicUnexpectedOperationException("Chunks are not decrypted");*/

            if (!checkCommonOrigin(chunks))
                throw new Exception("Different hashOrigin");

            List<EncryptRound> encryptRounds = new List<EncryptRound>();
            List<int> indexes = new List<int>();
            foreach (DataChunk c in chunks)
                foreach(EncryptRound round in c.encryptRounds)
                    if (!indexes.Contains(round.index))
                    {
                        encryptRounds.Add(round);
                        indexes.Add(round.index);
                    }
                        

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

            byte[] hash = HashProvider.ComputeHash(data);

            DataChunk firstChunk = chunks.First();
            DataChunk mergedChunk = new DataChunk(
                data, firstChunk.hashOrigin, firstChunk.signOrigin, hash, null, minStartByte, 
                maxEndByte, firstChunk.baseLength, encryptRounds);
            return mergedChunk;
        }

        public bool checkCommonOrigin(List<DataChunk> chunks)
        {
            for (int i = 1; i < chunks.Count; i++)
                if (
                    !chunks[i].hashOrigin.SequenceEqual(chunks[i - 1].hashOrigin) ||
                    chunks[i].baseLength != chunks[i - 1].baseLength)
                    return false;
            return true;
        }

        public DataInstance? chunkToInstance(DataChunk chunk)
        {
            byte[] hash = HashProvider.ComputeHash(chunk.data);
            EncryptRound lastRound = chunk.encryptRounds.Last();
            if (
                !lastRound.hash.SequenceEqual(hash) || 
                !RSAProvider.VerifyHash(hash, HASH_ALGORITHM_NAME, lastRound.sign))
                return null;

            return new DataInstance(chunk.data, chunk.hashOrigin, chunk.signOrigin, chunk.encryptRounds);
        }

        public DataInstance generateDataInstance(byte[] data, int encryptRounds = 0)
        {
            byte[] hash = HashProvider.ComputeHash(data);
            byte[] sign = RSAProvider.SignHash(hash, HASH_ALGORITHM_NAME);

            DataInstance dataInst = new DataInstance(data, hash, sign);

            dataInst = encryptData(dataInst, encryptRounds);

            return dataInst;
        }

        #region DataChunk generators
        public DataChunk generateDataChunk(DataInstance dataInst, int startByte, int endByte)
        {
            byte[] data = dataInst.data.Skip(startByte).Take(endByte - startByte + 1).ToArray();           
            byte[] chunkHash = HashProvider.ComputeHash(data);
            byte[] chunkSign = RSAProvider.SignHash(chunkHash, HASH_ALGORITHM_NAME);

            DataChunk chunk = new DataChunk(
                data, dataInst.hash, dataInst.sign, chunkHash, chunkSign,
                startByte, endByte, dataInst.data.Length, dataInst.encryptRounds);

            return chunk;
        }

        public List<DataChunk> generateDataChunks(DataInstance dataInst, int chunkSize)
        {
            List<DataChunk> chunks = new List<DataChunk>();
            int lastIndex = 0;
            for (int i = 0; i < dataInst.data.Length - chunkSize; i += chunkSize)
            {
                chunks.Add(generateDataChunk(dataInst, i, i + chunkSize));
                lastIndex = i + chunkSize;
            }

            if (lastIndex != dataInst.data.Length - 1)
                chunks.Add(generateDataChunk(dataInst, lastIndex, dataInst.data.Length - 1));

            return chunks;
        }

        /*public DataChunk generateDataChunk(byte[] data, List<EncryptRound> rounds)
        {
            byte[] hashOrigin = HashProvider.ComputeHash(data);
            byte[] chunkHash = HashProvider.ComputeHash(data);
            byte[] chunkSign = RSAProvider.SignHash(chunkHash, HASH_ALGORITHM_NAME);

            DataChunk chunk = new DataChunk(data, chunkHash, chunkSign, rounds);

            return chunk;
        }

        public DataChunk generateDataChunk(byte[] data, byte[] hashOrigin, List<EncryptRound> rounds)
        {
            byte[] chunkHash = HashProvider.ComputeHash(data);
            byte[] chunkSign = RSAProvider.SignHash(chunkHash, HASH_ALGORITHM_NAME);
            DataChunk chunk = new DataChunk(data, chunkHash, chunkSign, rounds);
           
            return chunk;
        }

        public DataChunk generateDataChunk(byte[] dataOrigin, int startByte, int endByte, List<EncryptRound> rounds)
        {
            byte[] data = dataOrigin.Skip(startByte).Take(endByte - startByte + 1).ToArray();            
            byte[] hashOrigin = HashProvider.ComputeHash(dataOrigin);
            byte[] chunkHash = HashProvider.ComputeHash(data);
            byte[] chunkSign = RSAProvider.SignHash(chunkHash, HASH_ALGORITHM_NAME);

            DataChunk chunk = new DataChunk(
                data, hashOrigin, chunkHash, chunkSign, 
                startByte, endByte, dataOrigin.Length, rounds);

            return chunk;
        }

        public DataChunk generateDataChunk(
            byte[] dataOrigin, byte[] hashOrigin, int startByte, int endByte, List<EncryptRound> rounds)
        {
            byte[] data = dataOrigin.Skip(startByte).Take(endByte - startByte + 1).ToArray();
            byte[] chunkHash = HashProvider.ComputeHash(data);
            byte[] chunkSign = RSAProvider.SignHash(chunkHash, HASH_ALGORITHM_NAME);

            DataChunk chunk = new DataChunk(
                data, hashOrigin, chunkHash, chunkSign, 
                startByte, endByte, dataOrigin.Length, rounds);
            
            return chunk;
        }

        public DataChunk generateDataChunk(
            byte[] dataOrigin, byte[] hashOrigin, byte[] data, int startByte, int endByte, List<EncryptRound> rounds)
        {
            byte[] chunkHash = HashProvider.ComputeHash(data);
            byte[] chunkSign = RSAProvider.SignHash(chunkHash, HASH_ALGORITHM_NAME);

            DataChunk chunk = new DataChunk(
                data, hashOrigin, chunkHash, chunkSign, 
                startByte, endByte, dataOrigin.Length, rounds);            

            return chunk;
        }*/
        #endregion

        public DataInstance encryptData(DataInstance dataInst, int roundsCount = 1)
        {
            DataInstance instance = dataInst;
            instance.encryptRounds = new List<EncryptRound>(dataInst.encryptRounds);
            for (int i = 0; i < roundsCount; i++)
            {
                AESProvider.GenerateKey();
                AESProvider.GenerateIV();
                byte[] key = AESProvider.Key;
                byte[] IV = AESProvider.IV;

                //let's encrypt data             
                byte[] encrypted;
                using (MemoryStream mstream = new MemoryStream())
                using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(mstream,
                        aesProvider.CreateEncryptor(key, IV), CryptoStreamMode.Write))
                        cryptoStream.Write(instance.data, 0, instance.data.Length);
                    encrypted = mstream.ToArray();
                }

                instance.data = encrypted;

                //now encrypt AES key
                byte[] enKey = RSAProvider.Encrypt(key, USE_AOEP);

                //and hash&sign
                byte[] hash = HashProvider.ComputeHash(instance.data);
                byte[] sign = RSAProvider.SignHash(hash, HASH_ALGORITHM_NAME);

                //now create Encrypt Round
                EncryptRound round = new EncryptRound(enKey, IV, hash, sign, instance.encryptRounds.Count);

                //and add to rounds list
                instance.encryptRounds.Add(round);
            }

            return instance;
        }
        
        public DataInstance decryptData(DataInstance dataInst)
        {
            int index = dataInst.encryptRounds.Count-1;
            while (index >= 0 && checkIfCanDecrypt(dataInst.encryptRounds[index]))
            {
                EncryptRound round = dataInst.encryptRounds[index];
                byte[] decryptedKey = RSAProvider.Decrypt(round.key, USE_AOEP);

                byte[] plain;
                int count;
                using (MemoryStream mStream = new MemoryStream(dataInst.data))
                {
                    using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
                    {                        
                        using (CryptoStream cryptoStream = new CryptoStream(mStream,
                         aesProvider.CreateDecryptor(decryptedKey, round.IV), CryptoStreamMode.Read))
                        {
                            plain = new byte[dataInst.data.Length];
                            count = cryptoStream.Read(plain, 0, plain.Length);
                        }
                    }
                }
                dataInst.data = new byte[count];
                Array.Copy(plain, dataInst.data, count);

                dataInst.encryptRounds.RemoveAt(index);
                index--;
            }

            return dataInst;
        }

        private bool checkIfCanDecrypt(EncryptRound round)
        {
            return RSAProvider.VerifyHash(round.hash, HASH_ALGORITHM_NAME, round.sign);
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

            byte[] sign = chunk.chunkSign;
            byte[] hash = chunk.chunkHash;
            if (chunk.encryptRounds.Count > 0)
            {
                sign = chunk.encryptRounds.Last().sign;
                hash = hashProvider.ComputeHash(chunk.data);                
                bool hashesAreEqual = hash.SequenceEqual(chunk.encryptRounds[0].hash);
                if (!hashesAreEqual) return false;
            }

            return csp.VerifyHash(hash, HASH_ALGORITHM_NAME, sign);
        }
        #endregion
    }

    public struct DataInstance
    {
        public byte[] data, hash, sign;
        public List<EncryptRound> encryptRounds;

        public DataInstance(byte[] data, byte[] hash, byte[] sign)
        {
            this.data = data;
            this.hash = hash;
            this.sign = sign;
            encryptRounds = new List<EncryptRound>();
        }

        public DataInstance(byte[] data, byte[] hash, byte[] sign, List<EncryptRound> encryptRounds)
        {
            this.data = data;
            this.hash = hash;
            this.sign = sign;
            this.encryptRounds = encryptRounds;
        }
    }

    public struct DataChunk
    {
        public byte[] data, hashOrigin, signOrigin, chunkHash, chunkSign;
        public int startByte, endByte, baseLength;
        public List<EncryptRound> encryptRounds;

        public DataChunk(
            byte[] data, byte[] hashOrigin, byte[] signOrigin, byte[] chunkHash, byte[] chunkSign, 
            int startByte, int endByte, int baseLength, List<EncryptRound> encryptRounds)
        {
            this.data = data;
            this.hashOrigin = hashOrigin;
            this.chunkHash = chunkHash;
            this.chunkSign = chunkSign;
            this.signOrigin = signOrigin;
            this.startByte = startByte;
            this.endByte = endByte;
            this.baseLength = baseLength;
            this.encryptRounds = encryptRounds;
        }

        public DataChunk(byte[] data, byte[] chunkHash, byte[] chunkSign, List<EncryptRound> encryptRounds)
        {
            this.data = data;
            this.chunkHash = hashOrigin = chunkHash;
            this.chunkSign = signOrigin = chunkSign;
            this.encryptRounds = encryptRounds;
            startByte = 0;
            endByte = data.Length - 1;
            baseLength = data.Length;
        }        
    }

    public struct EncryptRound
    {
        public byte[] key, IV, hash, sign;
        public int index;
        public EncryptRound(byte[] key, byte[] IV, byte[] hash, byte[] sign, int index = 0)
        {
            this.key = key;            
            this.IV = IV;
            this.hash = hash;
            this.sign = sign;
            this.index = index;
        }
    }
}
