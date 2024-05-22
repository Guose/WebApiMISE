namespace WebApiMISE.Types
{
    public enum ClientAuthType : byte
    {
        Unknown = 0,
        Key = 1,
        Certificate = 2,
        KeyIdentity = 3,
        ManagedIdentity = 4
    }
}
