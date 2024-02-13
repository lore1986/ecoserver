namespace webapi.Services.DatabaseService
{
    public interface IDatabaseService
    {
        Task<List<EcodroneUsers>> GetUsersList();
        //Task SaveUserToken(string identification, string token, byte[] userkey);
        //Task<byte[]> GetUserTokenKey(string token);



        /*
         * TEENSY GROUP
         */
        Task<bool> CreateTeensy(char[] idmasked, byte[] sync, string ipaddr, int port, bool setActive = false, string? customergroupid = null);
        Task<EcodroneBoat> GetTeensyInternalSync(byte[] sync);
        Task<EcodroneBoat> GetTeensyByName(char[] idmasked);
    }
}
