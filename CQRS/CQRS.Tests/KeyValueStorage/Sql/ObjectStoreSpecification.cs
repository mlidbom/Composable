using System;
using System.Collections.Generic;
using System.Configuration;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using FluentAssertions;
using NCrunch.Framework;

namespace CQRS.Tests.KeyValueStorage.Sql
{
    public abstract class ObjectStoreSpecification : NSpec.NUnit.nspec
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString;
        private IObservableObjectStore _store = null;

        public abstract void before_each();

        public void starting_from_empty()
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
                                 subscription = _store.DocumentUpdated.Subscribe(updated => { documentUpdated = updated; });
                                 typedSubscription = _store.DocumentUpdated.WithDocumentType<string>().Subscribe(updated => typedDocumentUpdated = updated);
                                 documentSubscription = _store.DocumentUpdated.DocumentsOfType<string>().Subscribe(document => receivedDocument = document);
                             };
                    context["when adding a document with the id \"the_id\" and the value \"the_value\""] =
                        () =>
                        {
                            act = () => _store.Add("the_id", "the_value");
                            it["DocumentUpdated is received"] = () => documentUpdated.Should().NotBeNull();
                            it["documentUpdated.Key is the_id"] = () => documentUpdated.Key.Should().Be("the_id");
                            it["documentUpdated.Document is \"the_value\""] = () => documentUpdated.Document.Should().Be("the_value");
                        };

                    context["after adding a document with the id \"the_id\" and the value \"the value\""] =
                        () =>
                        {
                            before = () =>
                                     {
                                         _store.Add("the_id", "the_value");
                                         nullOutReceived();
                                     };

                            context["when updating the object using the value \"the value\""] =
                                () =>
                                {
                                    act = () => _store.Update(new Dictionary<string, object>()
                                                              {
                                                                  {"the_id", "the_value"}
                                                              });

                                    it["subscriber is not notified"] = () => documentUpdated.Should().BeNull();
                                    it["typed subscriber is not notified"] = () => typedDocumentUpdated.Should().BeNull();
                                    it["no document is received"] = () => receivedDocument.Should().BeNull();
                                };

                            context["when updating the object using the value \"another_value\""] =
                                () =>
                                {
                                    act = () => _store.Update(new Dictionary<string, object>()
                                                              {
                                                                  {"the_id", "another_value"}
                                                              });

                                    it["DocumentUpdated is received"] = () => documentUpdated.Should().NotBeNull();
                                    it["documentUpdated.Key is the_id"] = () => documentUpdated.Key.Should().Be("the_id");
                                    it["documentUpdated.Document is \"another_value\""] = () => documentUpdated.Document.Should().Be("another_value");
                                    it["stored value is \"another_value\""] = () =>
                                                                              {
                                                                                  string storedValue;
                                                                                  _store.TryGet<string>("the_id", out storedValue);
                                                                                  storedValue.Should().Be("another_value");
                                                                              };

                                    it["typedDocumentUpdated is received"] = () => typedDocumentUpdated.Should().NotBeNull();
                                    it["typedDocumentUpdated.Key is the_id"] = () => typedDocumentUpdated.Key.Should().Be("the_id");
                                    it["documentUpdated.DocumentType is \"another_value\""] = () => typedDocumentUpdated.Document.Should().Be("another_value");

                                    it["receivedDocument is \"another_value\""] = () => receivedDocument.Should().Be("another_value");
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
                                                                      });

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
                                    act = () => _store.Add("the_id", "the_value");
                                    it["DocumentUpdated is not received"] = () => documentUpdated.Should().BeNull();
                                    it["typedDocumentUpdated is not received"] = () => typedDocumentUpdated.Should().BeNull();
                                    it["no document is received"] = () => receivedDocument.Should().BeNull();
                                };
                        };
                };
        }

        [ExclusivelyUses(NCrunchExlusivelyUsesResources.DocumentDbMdf)]
        public class SqlServerObjectStoreSpecification : ObjectStoreSpecification
        {
            override public void before_each()
            {
                SqlServerObjectStore.ResetDB(ConnectionString);
                _store = new SqlServerObjectStore(ConnectionString);
            }

            public void Does_not_call_db_in_constructor()
            {
                _store = new SqlServerObjectStore("ANonsensStringThatDoesNotResultInASqlConnection");
            }
        }

        public class SerializingInMemoryObjectStoreSpecification : ObjectStoreSpecification
        {
            override public void before_each()
            {
                _store = new ObservableInMemoryObjectStore();
            }
        }
    }
}
