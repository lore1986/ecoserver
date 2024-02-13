using MySqlConnector;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;

namespace webapi.Services.DatabaseService
{
    /*
     * ALTER TABLE `ecodrone`.`tokens` ADD PRIMARY KEY (`useridentification`(36));
     * ALTER TABLE `ecodrone`.`tokens` ADD UNIQUE `TOKEN` (`token`);
     * INSERT INTO `drones` (`sync`, `ipaddr`, `port`, `dronegroup`) VALUES (0x161718, '192.168.1.213', '5050', NULL);
     * MERGING TEST
    */

    public class DatabaseService : IDatabaseService
    {
        private readonly string connstring = "Server=localhost;User ID=ecodrone;Password=ecodrone2022;Database=ecodrone";

        public async Task<bool> CreateTeensy(char[] idmasked, byte[] sync, string ipaddr, int port, bool setActive = false, string? customergroupid = null)
        {
            if (ipaddr.Length > 15 || port > 65635 || idmasked.Length != 36)
                throw new ArgumentException("Something wrong with the ipaddr or the port or lenght of idmasked implement");

            using (MySqlConnection _connection = new MySqlConnection(connstring))
            {
                await _connection.OpenAsync(); 

                using (MySqlCommand insertCommand = new MySqlCommand("INSERT INTO `drones` (`idmask` `sync`, `ipaddr`, `port`, `active`, `dronegroup`) VALUES (@mask, @async, @ipadd, @po, @act, @gro);", _connection))
                {
                    /*(0x161718, '192.168.1.213', '5050', NULL);
                    @async, @ipadd, @po, @gro*/
                    insertCommand.Parameters.AddWithValue("@mask", idmasked);
                    insertCommand.Parameters.AddWithValue("@async", sync);
                    insertCommand.Parameters.AddWithValue("@ipadd", ipaddr);
                    insertCommand.Parameters.AddWithValue("@po", port);
                    insertCommand.Parameters.AddWithValue("@act", setActive);
                    insertCommand.Parameters.AddWithValue("@gro", customergroupid);

                    await insertCommand.ExecuteNonQueryAsync();
                    await _connection.CloseAsync();

                    return true;
                }

                
            }
        }

        public async Task<EcodroneBoat> GetTeensyByName(char[] idmasked)
        {
            if (idmasked == null || idmasked.Length != 36)
                throw new ArgumentException("Not Valid idmasked data implement execption");

            using (MySqlConnection _connection = new MySqlConnection(connstring))
            {
                using (MySqlCommand command = new MySqlCommand("SELECT * FROM drones WHERE idmask = @mask;", _connection))
                {
                    command.Parameters.AddWithValue("@mask", idmasked);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string ipadder = reader.GetString(2);
                            int portaddr = reader.GetInt16(3);
                            string? gid = reader.GetString(4);

                            //reader.FieldCount
                            EcodroneBoat ecodroneBoat = new EcodroneBoat((byte[])reader["sync"], ipadder, portaddr);
                            ecodroneBoat.ChangeState(reader.GetBoolean("isActive"));
                            //ecodroneBoat.GroupId = reader.GetString(4);
                            ecodroneBoat.MaskedId = reader.GetString(0);

                            return ecodroneBoat;
                        }
                    }
                }
            }

            return new EcodroneBoat([0x4E, 0x4E, 0x4E], "NNN", 0000);
        }

        public async Task<EcodroneBoat> GetTeensyInternalSync(byte[] sync)
        {
            if (sync == null || sync.Length != 3)
                throw new ArgumentException("Not Valid sync data implement execption");

            using (MySqlConnection _connection = new MySqlConnection(connstring))
            {
                using (MySqlCommand command = new MySqlCommand("SELECT * FROM drones WHERE sync = @async;", _connection))
                {
                    command.Parameters.AddWithValue("@async", sync);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string ipadder = reader.GetString(2);
                            int portaddr = reader.GetInt16(3);
                            string? gid = reader.GetString(4);

                            //reader.FieldCount
                            EcodroneBoat ecodroneBoat = new EcodroneBoat((byte[])reader["sync"], ipadder, portaddr);
                            ecodroneBoat.ChangeState(reader.GetBoolean("isActive"));
                            //ecodroneBoat.GroupId = reader.GetString(4);
                            
                            return ecodroneBoat;
                        }
                    }
                }
            }

            return new EcodroneBoat([0x4E, 0x4E, 0x4E], "NNN", 0000);
        }


        public async Task<List<EcodroneUsers>> GetUsersList() ///user
        {
      
            using(MySqlConnection _connection = new MySqlConnection(connstring))
            {
                await _connection.OpenAsync();

                using MySqlCommand command = new MySqlCommand("SELECT * FROM users;", _connection);
                using var reader = await command.ExecuteReaderAsync();
                List<EcodroneUsers> ecodrones = new List<EcodroneUsers>();

                while (await reader.ReadAsync())
                {
                    //reader.FieldCount
                    EcodroneUsers ecodroneUsers = new EcodroneUsers(reader.GetUInt16(0));
                    ecodroneUsers.Identification = reader.GetString(1);
                    ecodroneUsers.Password = reader.GetString(3);
                    ecodroneUsers.LastLogin = reader.GetDateTime(4);

                    ecodrones.Add(ecodroneUsers);
                }

                await _connection.CloseAsync();

                return ecodrones;
            }
            
        }



       

        //MAKE IT BOOL ON SUCCESS/FAIL
        public async Task SaveUserToken(string identification, string token, byte[] userkey)
        {
            using (MySqlConnection _connection = new MySqlConnection(connstring))
            {
                await _connection.OpenAsync(); 

                using (MySqlCommand checkCommand = new MySqlCommand("SELECT COUNT(*) FROM users WHERE identification = @IdentificationString;", _connection))
                {

                    checkCommand.Parameters.AddWithValue("@IdentificationString", identification);

                    long existingUserCount = (long)await checkCommand.ExecuteScalarAsync();


                    using (MySqlCommand verifyExistCommand = new MySqlCommand("SELECT COUNT(*) FROM tokens WHERE userid = @Uuid;", _connection))
                    {
                        verifyExistCommand.Parameters.AddWithValue("@Uuid", identification);

                        long existingTokenCount = (long)await verifyExistCommand.ExecuteScalarAsync();

                        //MUST BE 0 USERID identification used as a main table index to create
                        if (existingTokenCount == 0)
                        {

                            using (MySqlCommand insertCommand = new MySqlCommand("INSERT INTO tokens (userid, token, tkey) VALUES (@Identification, @Tokne, @UserKey);", _connection))
                            {
                                insertCommand.Parameters.AddWithValue("@Identification", identification);
                                insertCommand.Parameters.AddWithValue("@Tokne", token);
                                insertCommand.Parameters.AddWithValue("@UserKey", userkey);

                                await insertCommand.ExecuteNonQueryAsync();
                            }
                        }
                        else if (existingTokenCount == 1)
                        {

                            using (MySqlCommand updateCommand = new MySqlCommand("UPDATE tokens SET token = @Tokne, tkey = @UserKey WHERE userid = @Identification;", _connection))
                            {
                                updateCommand.Parameters.AddWithValue("@Tokne", token);
                                updateCommand.Parameters.AddWithValue("@UserKey", userkey);
                                updateCommand.Parameters.AddWithValue("@Identification", identification);

                                await updateCommand.ExecuteNonQueryAsync();
                            }

                        }
                        else
                        {
                            Debug.WriteLine("you should never see this");
                        }
                        
                    }
                }

                await _connection.CloseAsync();
            }
        }
        
        public async Task<byte[]> GetUserTokenKey(string token)
        {
            using (MySqlConnection _connection = new MySqlConnection(connstring))
            {
                await _connection.OpenAsync();

                using (MySqlCommand verifyExistCommand = new MySqlCommand("SELECT COUNT(*) FROM tokens WHERE token = @IdentificationToken;", _connection))
                {
                    verifyExistCommand.Parameters.AddWithValue("@IdentificationToken", token);

                    long existingTokenCount = (long)await verifyExistCommand.ExecuteScalarAsync();

                    //MUST BE 1 USERID token must be unique
                    if (existingTokenCount == 1)
                    {
                        using (MySqlCommand command = new MySqlCommand("SELECT tkey FROM tokens WHERE token = @Token;", _connection))
                        {
                            command.Parameters.AddWithValue("@Token", token);

                            using (var reader = await command.ExecuteReaderAsync())
                            {

                                while (await reader.ReadAsync())
                                {
                                    if (reader["tkey"] != DBNull.Value)
                                    {
                                        byte[] userKeyBytes = (byte[])reader["tkey"];
                                        await _connection.CloseAsync();
                                        return userKeyBytes.Skip(2).ToArray();
                                    }
                                }
                            }
                        }

                    } else
                    {
                        Debug.WriteLine("you should never see this");
                    }
                }

                await _connection.CloseAsync();
                return [];
            }
        }

    }
}
