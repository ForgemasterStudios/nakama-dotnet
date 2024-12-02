
namespace Nakama
{
    public interface ISymmetricEncryption
    {
        bool IsEnabled { get; }
        byte[] Encrypt(string plainText);
        string Decrypt(byte[] ciphertext);
    }
}