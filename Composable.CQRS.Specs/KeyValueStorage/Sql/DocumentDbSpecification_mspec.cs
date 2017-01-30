//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using Composable.KeyValueStorage;
//using Composable.KeyValueStorage.SqlServer;
//using Composable.System.Configuration;
//using Composable.System.Linq;
//using FluentAssertions;
//using Machine.Specifications;
//using Machine.Specifications.Model;
//using NUnit.Framework;

//namespace CQRS.Tests.KeyValueStorage.Sql
//{
//    public abstract class DocumentDbSpecification
//    {
//        static IDocumentDb _store = null;
//        static string _ignoredString;
//        static Dictionary<Type, Dictionary<string, string>> _persistentValues;

//        static void Before()
//        {
//            _persistentValues = new Dictionary<Type, Dictionary<string, string>>();
//            Before_();
//        }

//        public DocumentDbSpecification()
//        {
//            Console.WriteLine("Constructor 1");
//        }

//        static DocumentDbSpecification()
//        {
//            new SqlServerDocumentDbSpecification();//TRick to make sure static constructor runs.
//            //new InMemoryDocumentDbSpecification();//TRick to make sure static constructor runs.
//            Console.WriteLine("static constructor 1");
//        }

//        static protected Action Before_;

//        static protected Action After_;

//        static string GetStoredValue(string theID)
//        {
//            string storedValue;
//            _store.TryGet<string>(theID, out storedValue, _persistentValues);
//            return storedValue;
//        }

//        class starting_from_empty
//        {
//            class after_subscribing_to_document_updates
//            {
//                static IDocumentUpdated documentUpdated = null;
//                static IDocumentUpdated<string> typedDocumentUpdated = null;
//                static IDisposable subscription = null;
//                static IDisposable typedSubscription = null;
//                static IDisposable documentSubscription = null;
//                static string receivedDocument = null;
//                static Action removeSubscriptions = () =>
//                                                 {
//                                                     // ReSharper disable PossibleNullReferenceException
//                                                     subscription.InternalDispose();
//                                                     typedSubscription.InternalDispose();
//                                                     documentSubscription.InternalDispose();
//                                                     // ReSharper restore PossibleNullReferenceException
//                                                 };

//                    static Action nullOutReceived = () =>
//                                             {
//                                                 documentUpdated = null;
//                                                 typedDocumentUpdated = null;
//                                                 receivedDocument = null;
//                                             };
//                    Establish before = () =>
//                             {
//                                 Before();                                 
//                                 nullOutReceived();
//                                 subscription = _store.DocumentUpdated.Subscribe(updated => { documentUpdated = updated; });
//                                 typedSubscription = _store.DocumentUpdated.WithDocumentType<string>().Subscribe(updated => typedDocumentUpdated = updated);
//                                 documentSubscription = _store.DocumentUpdated.DocumentsOfType<string>().Subscribe(document => receivedDocument = document);
//                             };

//                Cleanup after = () => After_();

//                class when_adding_a_document_with_the_id_QUOT_the_id_QUOT_and_the_value_QUOT_the_value_QUOT
//                        {
//                            Because act = () => _store.Add("the_id", "the_value", _persistentValues);
//                            It DocumentUpdated_is_received = () => documentUpdated.Should().NotBeNull();
//                            It documentUpdated_Key_is_the_id = () => documentUpdated.Key.Should().Be("the_id");
//                            It documentUpdated_Document_isQUOTthe_valueQUOT = () => documentUpdated.Document.Should().Be("the_value");
//                        }

//                    class after_adding_a_document_with_the_id_QQUTthe_idQUOT_and_the_value_QUOTthe_value_QUOT
//                    {
//                       Establish before = () =>
//                                     {
//                                         _store.Add("the_id", "the_value", _persistentValues);
//                                         nullOutReceived();
//                                     };

//                            class when_updating_the_object_using_the_value_QUOTthe_valueQUOT
//                            {
//                                    Because act = () => _store.Update(new Dictionary<string, object>()
//                                                              {
//                                                                  {"the_id", "the_value"}
//                                                              }, _persistentValues);

//                                It subscriber_is_not_notified = () => documentUpdated.Should().BeNull();
//                                It typed_subscriber_is_not_notified = () => typedDocumentUpdated.Should().BeNull();
//                                It no_document_is_received = () => receivedDocument.Should().BeNull();
//                                It stored_value_is_QUOTthe_valueQUOT = () => GetStoredValue("the_id").Should().Be("the_value");  
//                           }

//                            class when_updating_the_object_using_the_value_QUOTanother_valueQUOT
//                           {
//                                    Because act = () => _store.Update(new Dictionary<string, object>()
//                                                              {
//                                                                  {"the_id", "another_value"}
//                                                              }, _persistentValues);

//                                    It DocumentUpdated_is_received = () => documentUpdated.Should().NotBeNull();
//                                    It documentUpdated_Key_is_the_id = () => documentUpdated.Key.Should().Be("the_id");
//                                    It documentUpdated_Document_is_QUOT_another_valueQUOT = () => documentUpdated.Document.Should().Be("another_value");
//                                    It stored_value_isQUOTanother_valueQUOT = () => GetStoredValue("the_id").Should().Be("another_value");

//                                    It typedDocumentUpdated_is_received = () => typedDocumentUpdated.Should().NotBeNull();
//                                    It typedDocumentUpdated_Key_is_the_id = () => typedDocumentUpdated.Key.Should().Be("the_id");
//                                    It documentUpdated_DocumentType_is_QUOTanother_valueQUOT = () => typedDocumentUpdated.Document.Should().Be("another_value");

//                                    It receivedDocument_is_QUOTanother_valueQUOT = () => receivedDocument.Should().Be("another_value");
//                                };
//                            class after_deleting_document_with_key_QUOTthe_idQUOT
//                                {
//                                    Because act = () => _store.Remove<string>("the_id");
//                                    It store_does_not_contain_document_with_id_QUOTthe_idQUOT = () => _store.TryGet("the_id", out _ignoredString, _persistentValues).Should().BeFalse();
//                                }

//                            class after_unsubscribing
//                            {
//                                   Establish before = () => removeSubscriptions();

//                                    class when_updating_the_object_using_the_value_QUOTanother_value
//                                        {
//                                            Because act = () => _store.Update(new Dictionary<string, object>()
//                                                                      {
//                                                                          {"the_id", "another_value"}
//                                                                      }, _persistentValues);

//                                            It DocumentUpdated_is_not_received = () => documentUpdated.Should().BeNull();
//                                            It typedDocumentUpdated_is_not_received = () => typedDocumentUpdated.Should().BeNull();
//                                            It no_document_is_received = () => receivedDocument.Should().BeNull();
//                                        }
//                                }
//                        }

//                    class after_unsubscribing
//                    {
//                        Establish before = () => removeSubscriptions();
//                        class when_adding_a_document_with_the_id_QUOTthe_idQUOT_and_the_value_QUOTthe_valueQUOT
//                            {
//                                Because act = () => _store.Add("the_id", "the_value", _persistentValues);
//                                It DocumentUpdated_is_not_received = () => documentUpdated.Should().BeNull();
//                                It typedDocumentUpdated_is_not_received = () => typedDocumentUpdated.Should().BeNull();
//                                It no_document_is_received = () => receivedDocument.Should().BeNull();
//                            };
//                    };
//                };
//        }


//        [Subject("")]
//        public class SqlServerDocumentDbSpecification
//        {
//            static SqlServerDatabasePool _dbPool;
//            static string _connectionString;

//            public SqlServerDocumentDbSpecification()
//            {
//                Console.WriteLine("static constructor 2");

//                Before_ = () =>
//                          {
//                              _dbPool =
//                                  new SqlServerDatabasePool(
//                                      new ConnectionStringConfigurationParameterProvider().GetConnectionString("MasterDB").ConnectionString);
//                              _connectionString = _dbPool.ConnectionStringFor($"{nameof(SqlServerDocumentDbSpecification)}DocumentDB");
//                              SqlServerDocumentDb.ResetDB(_connectionString);
//                              _store = new SqlServerDocumentDb(_connectionString);
//                          };

//                After_ = () =>
//                         {
//                             _dbPool.InternalDispose();
//                         };
//            }


//            public void Does_not_call_db_in_constructor()
//            {
//                _store = new SqlServerDocumentDb("ANonsensStringThatDoesNotResultInASqlConnection");
//            }
//        }

//        public class InMemoryDocumentDbSpecification : DocumentDbSpecification
//        {
//            public InMemoryDocumentDbSpecification()
//            {
//                Console.WriteLine("static constructor 3");

//                Before_ = () =>
//                {
//                    _store = new InMemoryDocumentDb();
//                };

//                After_ = () => { };
//            }
//        }
//    }
//}
