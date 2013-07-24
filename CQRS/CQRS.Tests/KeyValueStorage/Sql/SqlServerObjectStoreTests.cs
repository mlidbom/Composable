using System;
using System.Collections.Generic;
using System.Configuration;
using Composable.KeyValueStorage.SqlServer;
using FluentAssertions;

namespace CQRS.Tests.KeyValueStorage.Sql
{
    public class SqlServerObjectStoreSpecification : NSpec.NUnit.nspec
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString;


        public void starting_from_empty()
        {
            SqlServerObjectStore store = null;
            before = () =>
                     {
                         var db = new SqlServerDocumentDb(ConnectionString, SqlServerDocumentDbConfig.Default);
                         SqlServerDocumentDb.ResetDB(ConnectionString);
                         store = new SqlServerObjectStore(db);
                     };


            context["after subscribing to document updates"] =
                () =>
                {
                    IDocumentUpdated documentUpdated = null;
                    IDisposable subscription = null;
                    before = () =>
                             {
                                 documentUpdated = null;
                                 subscription = store.Subscribe(updated => { documentUpdated = updated; });
                             };
                    context["when adding a document with the id \"the_id\" and the value \"the_value\""] =
                        () =>
                        {
                            act = () => store.Add("the_id", "the_value");
                            it["DocumentUpdated is received"] = () => documentUpdated.Should().NotBeNull();
                            it["documentUpdated.Key is the_id"] = () => documentUpdated.Key.Should().Be("the_id");
                            it["documentUpdated.DocumentType is string"] = () => documentUpdated.DocumentType.Should().Be<string>();
                        };

                    context["after adding a document with the id \"the_id\" and the value \"the value\""] =
                        () =>
                        {
                            before = () =>
                                     {                                         
                                         store.Add("the_id", "the_value");
                                         documentUpdated = null;
                                     };

                            context["when updating the object using the value \"the value\""] =
                                () =>
                                {
                                    act = () => store.Update(new Dictionary<string, object>()
                                                             {
                                                                 {"the_id", "the_value"}
                                                             });

                                    it["subscriber is not notified"] = () => documentUpdated.Should().BeNull();
                                };

                            context["when updating the object using the value \"another value\""] =
                                () =>
                                {
                                    act = () => store.Update(new Dictionary<string, object>()
                                                             {
                                                                 {"the_id", "another_value"}
                                                             });

                                    it["DocumentUpdated is received"] = () => documentUpdated.Should().NotBeNull();
                                    it["documentUpdated.Key is the_id"] = () => documentUpdated.Key.Should().Be("the_id");
                                    it["documentUpdated.DocumentType is string"] = () => documentUpdated.DocumentType.Should().Be<string>();
                                };
                            context["after unsubscribing"] =
                                () =>
                                {
                                    before = () => subscription.Dispose();
                                    context["when updating the object using the value \"another value\""] =
                                        () =>
                                        {
                                            act = () => store.Update(new Dictionary<string, object>()
                                                                     {
                                                                         {"the_id", "another_value"}
                                                                     });

                                            it["DocumentUpdated is not received"] = () => documentUpdated.Should().BeNull();
                                        };
                                };
                        };
                    context["after unsubscribing"] =
                        () =>
                        {
                            before = () => subscription.Dispose();
                            context["when adding a document with the id \"the_id\" and the value \"the_value\""] =
                                () =>
                                {
                                    act = () => store.Add("the_id", "the_value");
                                    it["DocumentUpdated is not received"] = () => documentUpdated.Should().BeNull();
                                };
                        };
                };
        }
    }
}
