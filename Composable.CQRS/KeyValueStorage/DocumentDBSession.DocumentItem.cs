using System;
using System.Collections.Generic;

using Composable.Contracts;
using Composable.System;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public partial class DocumentDbSession
    {
        internal class DocumentItem
        {
            readonly IDocumentDb _backingStore;
            readonly Dictionary<Type, Dictionary<string, string>> _persistentValues;
            DocumentKey Key { get; set; }

            public DocumentItem(DocumentKey key, IDocumentDb backingStore, Dictionary<Type, Dictionary<string, string>> persistentValues)
            {
                _backingStore = backingStore;
                _persistentValues = persistentValues;
                Key = key;
            }

            Object Document { get; set; }
            public bool IsDeleted { get; private set; }
            bool IsInBackingStore { get; set; }

            bool ScheduledForAdding { get { return !IsInBackingStore && !IsDeleted && Document != null; } }
            bool ScheduledForRemoval { get { return IsInBackingStore && IsDeleted; } }
            bool ScheduledForUpdate { get { return IsInBackingStore && !IsDeleted; } }

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
