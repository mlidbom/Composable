using System;
using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public partial class DocumentDbSession
    {        
        internal class DocumentItem
        {
            private readonly IDocumentDb _backingStore;
            private DocumentKey Key { get; set; }

            public DocumentItem(DocumentKey key, IDocumentDb backingStore)
            {
                _backingStore = backingStore;
                Key = key;
            }

            private Object Document { get; set; }
            public bool IsDeleted { get; private set; }
            private bool IsInBackingStore { get; set; }

            private bool ScheduledForAdding { get { return !IsInBackingStore && !IsDeleted && Document != null; } }
            private bool ScheduledForRemoval { get { return IsInBackingStore && IsDeleted; } }
            private bool ScheduledForUpdate { get { return IsInBackingStore && !IsDeleted; } }

            public void Delete()
            {
                IsDeleted = true;
            }

            public void Save(object document)
            {
                if(document == null)
                {
                    throw new ArgumentNullException("document");
                }
                Document = document;
                IsDeleted = false;
            }

            public void DocumentLoadedFromBackingStore(object document)
            {
                if (document == null)
                {
                    throw new ArgumentNullException("document");
                }
                Document = document;
                IsInBackingStore = true;
            }

            public void CommitChangesToBackingStore()
            {
                if (ScheduledForAdding)
                {
                    _backingStore.Add(Key.Id, Document);
                    IsInBackingStore = true;
                }
                else if (ScheduledForRemoval)
                {
                    _backingStore.Remove(Key.Id, Document.GetType());
                    IsInBackingStore = false;
                    Document = null;
                }
                else if (ScheduledForUpdate)
                {
                    _backingStore.Update(Seq.Create(new KeyValuePair<string, object>(Key.Id, Document)));
                }
            }
        }

    }
}
