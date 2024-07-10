using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Inserat_Function_GoogleCloud
{
    public class DBAccess 
    {
        public IMongoDatabase _database { get; private set; }
        public IMongoDatabase CreateConnToDb(string dbname)
        {
            string connectionString =
            @"mongodb://CONSTRING@";
            MongoClientSettings settings = MongoClientSettings.FromUrl(
              new MongoUrl(connectionString)
            );
            //settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            var client = new MongoClient(settings);

            _database = client.GetDatabase(dbname);
            return _database;
        }

        public T getByID<T>(string id) where T : DBClass
        {
            return GetDynamicCollection<T>().Find(x => x.ID == id).First();
        }

        public ICollection<T> GetAll<T>() where T : DBClass
        {
            return GetDynamicCollection<T>().Find(FilterDefinition<T>.Empty).ToList();
        }

        public IMongoCollection<T> GetDynamicCollection<T>() where T : DBClass
        {
            return _database.GetCollection<T>(typeof(T).Name);
        }
        public ICollection<T> GetAllByUserID<T>(string userid) where T : Inserat
        {
            return _database.GetCollection<T>("Inserat").Find(x => x.UserID == userid).ToList();
        }

        public ICollection<T> GetAllByKategorieID<T>(ObjectId kategorieid) where T : Inserat
        {
            return _database.GetCollection<T>("Inserat").Find(x => x.KategorieID == kategorieid).ToList();
        }
    }
}
