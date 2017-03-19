using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.Persistence.KeyValueStorage;
using Composable.System;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    partial class DocumentDbSession
    {
        class DocumentItem
        {
            readonly IDocumentDb _backingStore;
            readonly Dictionary<Type, Dictionary<string, string>> _persistentValues;
            DocumentKey Key { get; set; }

            internal DocumentItem(DocumentKey key, IDocumentDb backingStore, Dictionary<Type, Dictionary<string, string>> persistentValues)
            {
                _backingStore = backingStore;
                _persistentValues = persistentValues;
                Key = key;
            }

            Object Document { get; set; }
            public bool IsDeleted { get; private set; }
            bool IsInBackingStore { get; set; }

            bool ScheduledForAdding => !IsInBackingStore && !IsDeleted && Document != null;
            bool ScheduledForRemoval => IsInBackingStore && IsDeleted;
            bool ScheduledForUpdate => IsInBackingStore && !IsDeleted;

            public void Delete()
            {
                IsDeleted = true;
            }

            public void Save(object document)
            {
                Contract.Argument(() => document)
                            .NotNull();
                Document = document;
                IsDeleted = false;
            }

            public void DocumentLoadedFromBackingStore(object document)
            {
                Contract.Argument(() => document)
                            .NotNull();
                Document = document;
                IsInBackingStore = true;
            }

            bool IsCommitting { get; set; }
            public void CommitChangesToBackingStore()
            {
                //Avoid reentrancy issues.
                if(IsCommitting)
                {
                    return;
                }
                IsCommitting = true;
                using(Disposable.Create(() => IsCommitting = false))//Reset IsCommitting to false once we are done committing.
                {
                    if(ScheduledForAdding)
                    {
                        IsInBackingStore = true;
                        _backingStore.Add(Key.Id, Document, _persistentValues);
                    }
                    else if(ScheduledForRemoval)
                    {
                        var docType = Document.GetType();
                        Document = null;
                        IsInBackingStore = false;
                        _backingStore.Remove(Key.Id, docType);

                    }
                    else if(ScheduledForUpdate)
                    {
                        _backingStore.Update(Seq.Create(new KeyValuePair<string, object>(Key.Id, Document)), _persistentValues);
                    }
                }
            }
        }

    }
}
