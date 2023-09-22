using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using DiscordDb.Models;
namespace DiscordDb.DataAccess;

public class DataAccess
{
    private const string ConnectionString = "mongodb://127.0.0.1:27017";
    private const string DatabaseName = "discord";
    private const string UserCollection = "discordusers";

    private IMongoCollection<T> ConnectToMongo<T> (in string collection)
    {
        var client = new MongoClient(ConnectionString);
        var db = client.GetDatabase(DatabaseName);
        return db.GetCollection<T>(collection);
    }

    public async Task<List<UserModel>> GetAllUsers()
    {
        var usersCollection = ConnectToMongo<UserModel>(UserCollection);
        var results = await usersCollection.FindAsync(_ => true);
        return results.ToList();

    }
    public async Task<List<UserModel>> FindDiscordUser(ulong id)
    {
        var usersCollection = ConnectToMongo<UserModel>(UserCollection);
        var results = await usersCollection.FindAsync(e => e.DiscordId == id);
        return results.ToList();
    }
    public Task CreateUser(UserModel user)
    {
        var usersCollection = ConnectToMongo<UserModel>(UserCollection);
        return usersCollection.InsertOneAsync(user);
    }

    public Task UpdateUser(UserModel user)
    {
        var usersCollection = ConnectToMongo<UserModel>(UserCollection);
        var filter = Builders<UserModel>.Filter.Eq("Id", user.Id);
        return usersCollection.ReplaceOneAsync(filter, user, new ReplaceOptions { IsUpsert = true });
    }

    public Task DeleteUser(UserModel user)
    {
        var usersCollection = ConnectToMongo<UserModel>(UserCollection);
        return usersCollection.DeleteOneAsync(c=>c.Id == user.Id); 
    }

}
