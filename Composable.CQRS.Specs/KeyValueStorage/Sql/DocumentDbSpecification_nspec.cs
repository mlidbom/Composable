using System;
using System.Collections.Generic;
using Composable.CQRS.KeyValueStorage;
using Composable.CQRS.KeyValueStorage.SqlServer;
using Composable.Persistence.KeyValueStorage;
using Composable.System.Configuration;
using Composable.Testing;
using FluentAssertions;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

namespace Composable.CQRS.Specs.KeyValueStorage.Sql
{
    public abstract class DocumentDbSpecification : nspec
    {
        IDocumentDb _store = null;
        string _ignoredString;
        Dictionary<Type, Dictionary<string, string>> _persistentValues;

        void before_each()
        {
            _persistentValues = new Dictionary<Type, Dictionary<string, string>>();
        }

        protected abstract void InitStore();
        protected abstract void CleanStore();

        void starting_from_empty()
        {
            context["after subscribing to document updates"] =
                () =>
                {
                    IDocumentUpdated documentUpdated = null;
                    IDocumentUpdated<string> typedDocumentUpdated = null;
                    IDisposable subscription = null;
                    IDisposable typedSubscription = null;
                    IDisposable documentSubscription = null;
                    string receivedDocument = null;
                    Action removeSubscriptions = () =>
                                                 {
                                                     // ReSharper disable PossibleNullReferenceException
                                                     subscription.Dispose();
                                                     typedSubscription.Dispose();
                                                     documentSubscription.Dispose();
                                                     // ReSharper restore PossibleNullReferenceException
                                                 };

                    Action nullOutReceived = () =>
                                             {
                                                 documentUpdated = null;
                                                 typedDocumentUpdated = null;
                                                 receivedDocument = null;
                                             };
                    before = () =>
                             {
                                 nullOutReceived();
                                 InitStore();
                                 subscription = _store.DocumentUpdated.Subscribe(updated => { documentUpdated = updated; });
                                 typedSubscription = _store.DocumentUpdated.WithDocumentType<string>().Subscribe(updated => typedDocumentUpdated = updated);
                                 documentSubscription = _store.DocumentUpdated.DocumentsOfType<string>().Subscribe(document => receivedDocument = document);
                             };
                    after = CleanStore;
                    context["when adding a document with the id \"the_id\" and the value \"the_value\""] =
                        () =>
                        {
                            act = () => _store.Add("the_id", "the_value", _persistentValues);
                            it["DocumentUpdated is received"] = () => documentUpdated.Should().NotBeNull();
                            it["documentUpdated.Key is the_id"] = () => documentUpdated.Key.Should().Be("the_id");
                            it["documentUpdated.Document is \"the_value\""] = () => documentUpdated.Document.Should().Be("the_value");
                        };

                    context["after adding a document with the id \"the_id\" and the value \"the value\""] =
                        () =>
                        {
                            before = () =>
                                     {
                                         _store.Add("the_id", "the_value", _persistentValues);
                                         nullOutReceived();
                                     };

                            context["when updating the object using the value \"the value\""] =
                                () =>
                                {
                                    act = () => _store.Update(new Dictionary<string, object>()
                                                              {
                                                                  {"the_id", "the_value"}
                                                              }, _persistentValues);

                                    it["subscriber is not notified"] = () => documentUpdated.Should().BeNull();
                                    it["typed subscriber is not notified"] = () => typedDocumentUpdated.Should().BeNull();
                                    it["no document is received"] = () => receivedDocument.Should().BeNull();
                                    it["stored value is \"the_value\""] = () => GetStoredValue("the_id").Should().Be("the_value");
                                };

                            context["when updating the object using the value \"another_value\""] =
                                () =>
                                {
                                    act = () => _store.Update(new Dictionary<string, object>()
                                                              {
                                                                  {"the_id", "another_value"}
                                                              }, _persistentValues);

                                    it["DocumentUpdated is received"] = () => documentUpdated.Should().NotBeNull();
                                    it["documentUpdated.Key is the_id"] = () => documentUpdated.Key.Should().Be("the_id");
                                    it["documentUpdated.Document is \"another_value\""] = () => documentUpdated.Document.Should().Be("another_value");
                                    it["stored value is \"another_value\""] = () => GetStoredValue("the_id").Should().Be("another_value");

                                    it["typedDocumentUpdated is received"] = () => typedDocumentUpdated.Should().NotBeNull();
                                    it["typedDocumentUpdated.Key is the_id"] = () => typedDocumentUpdated.Key.Should().Be("the_id");
                                    it["documentUpdated.DocumentType is \"another_value\""] = () => typedDocumentUpdated.Document.Should().Be("another_value");

                                    it["receivedDocument is \"another_value\""] = () => receivedDocument.Should().Be("another_value");
                                };
                            context["after deleting document with key \"the_id\""] =
                                () =>
                                {
                                    act = () => _store.Remove<string>("the_id");
                                    it["store does not contain document with id \"the_id\""] = () => _store.TryGet("the_id", out _ignoredString, _persistentValues).Should().BeFalse();
                                };
                            context["after unsubscribing"] =
                                () =>
                                {
                                    before = removeSubscriptions;
                                    context["when updating the object using the value \"another value\""] =
                                        () =>
                                        {
                                            act = () => _store.Update(new Dictionary<string, object>()
                                                                      {
                                                                          {"the_id", "another_value"}
                                                                      }, _persistentValues);

                                            it["DocumentUpdated is not received"] = () => documentUpdated.Should().BeNull();
                                            it["typedDocumentUpdated is not received"] = () => typedDocumentUpdated.Should().BeNull();
                                            it["no document is received"] = () => receivedDocument.Should().BeNull();
                                        };
                                };
                        };
                    context["after unsubscribing"] =
                        () =>
                        {
                            before = removeSubscriptions;
                            context["when adding a document with the id \"the_id\" and the value \"the_value\""] =
                                () =>
                                {
                                    act = () => _store.Add("the_id", "the_value", _persistentValues);
                                    it["DocumentUpdated is not received"] = () => documentUpdated.Should().BeNull();
                                    it["typedDocumentUpdated is not received"] = () => typedDocumentUpdated.Should().BeNull();
                                    it["no document is received"] = () => receivedDocument.Should().BeNull();
                                };
                        };
                };
        }

        string GetStoredValue(string theID)
        {
            string storedValue;
            _store.TryGet<string>(theID, out storedValue, _persistentValues);
            return storedValue;
        }

        public class SqlServerDocumentDbSpecification : DocumentDbSpecification
        {
            SqlServerDatabasePool _connectionManager;

            protected override void InitStore()
            {
                _connectionManager = new SqlServerDatabasePool(new ConnectionStringConfigurationParameterProvider().GetConnectionString("MasterDB").ConnectionString);
                var connectionString = _connectionManager.ConnectionStringFor($"{nameof(SqlServerDocumentDbSpecification)}DocumentDB");
                SqlServerDocumentDb.ResetDB(connectionString);
                _store = new SqlServerDocumentDb(connectionString);
            }
            protected override void CleanStore()
            {
                _connectionManager.Dispose();
            }

            public void Does_not_call_db_in_constructor()
            {
                SqlServerDocumentDb db;
                act = () => db = new SqlServerDocumentDb("ANonsensStringThatDoesNotResultInASqlConnection");
                it["Throws no exception"] = () => { var blah = new SqlServerDocumentDb("ANonsensStringThatDoesNotResultInASqlConnection"); };
            }
        }

        public class InMemoryDocumentDbSpecification : DocumentDbSpecification
        {
            protected override void InitStore()
            {
                _store = new InMemoryDocumentDb();
            }

            protected override void CleanStore() { _store = null; }
        }
    }
}
