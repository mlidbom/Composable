using System;
using Composable.StuffThatDoesNotBelongHere.Translation;

namespace Composable.CQRS
{
    public class TranslatedReferenceObject<TObject> : ReferenceObject<TObject> where TObject : TranslatedReferenceObject<TObject>
    {
        protected TranslatedReferenceObject() { }
        public TranslatedReferenceObject(Guid id, string name = null): base(id)
        {
            Name = name;
        }

        [Translate]
        public override string Name { get; set; }
    }
}