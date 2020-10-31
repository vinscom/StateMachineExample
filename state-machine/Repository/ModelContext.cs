using System;
using System.Threading.Tasks;
using Foo.TranscriptWorkflow;
using MongoDB.Driver;

namespace Foo.Repository
{
    public class ModelContext
    {
        public IMongoClient Client { get; set; }
        public IMongoDatabase Database { get; set; }

        private static ModelContext _modelContext;

        private ModelContext() { }

        public static ModelContext Create(String connectionString = "mongodb://root:example@localhost:27017/?authSource=admin")
        {
            if (_modelContext == null)
            {
                _modelContext = new ModelContext();
                _modelContext.Client = new MongoClient(connectionString);
                _modelContext.Database = _modelContext.Client.GetDatabase("admin");
                _modelContext.TestConnection();

                // Workflow Collection Setup
                var workflowCollection = _modelContext.Workflow;
                var idxTranscriptId = Builders<Workflow>.IndexKeys.Ascending(item => item.Data.TranscriptId);
                var idxOpen = Builders<Workflow>.IndexKeys.Ascending(item => item.Open);
                var idxCurrentState = Builders<Workflow>.IndexKeys.Ascending(item => item.Data.CurrentState);
                workflowCollection.Indexes.CreateOne(new CreateIndexModel<Workflow>(idxTranscriptId));
                workflowCollection.Indexes.CreateOne(new CreateIndexModel<Workflow>(idxOpen));
                workflowCollection.Indexes.CreateOne(new CreateIndexModel<Workflow>(idxCurrentState));
            }
            return _modelContext;
        }

        public void TestConnection()
        {
            var dbsCursor = _modelContext.Client.ListDatabases();
            var dbsList = dbsCursor.ToList();
            foreach (var db in dbsList)
            {
                Console.WriteLine(db);
            }
        }

        public IMongoCollection<Workflow> Workflow
        {
            get {
                IMongoCollection<Workflow> collection = Database.GetCollection<Workflow>("workflow");

                if (collection == null)
                {
                    Database.CreateCollection("workflow");
                    collection = Database.GetCollection<Workflow>("workflow");
                }

                return collection;
            }
        }
    }
}
