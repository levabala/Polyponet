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

        public byte[] deviceId = Guid.NewGuid().ToByteArray();
        public RSAParameters publicRSA;
        public bool online = true;

        //temporary public

        public RSAParameters privateRSA;
        public Dictionary<byte[], Node> trustedNodes = new Dictionary<byte[], Node>();
        public Dictionary<byte[], Node> familiarNodes = new Dictionary<byte[], Node>();
        public Dictionary<byte[], Node> knownNodes = new Dictionary<byte[], Node>();

        //local variables 
        RSACryptoServiceProvider RSAProvider = new RSACryptoServiceProvider(KEY_SIZE);
        SHA256CryptoServiceProvider SHAProvider = new SHA256CryptoServiceProvider();

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
            return RSAProvider.SignData(token, SHAProvider);
        }
    }
}
