using System.Text;

namespace Nakama
{
    public class NoEncryption : ISymmetricEncryption
    {
        public bool IsEnabled => false;
        public string Decrypt(byte[] ciphertext) => Encoding.UTF8.GetString(ciphertext);
        public byte[] Encrypt(string plainText) => Encoding.UTF8.GetBytes(plainText);
    }
}