using System;

namespace Composable.CQRS
{
    public class ReferenceObject<TObject> : UnNamedReferenceObject<TObject>,        
        INamed 
        where TObject : ReferenceObject<TObject>
    {
        protected ReferenceObject(){}
        public ReferenceObject(Guid id, string name = null) : base(id)
        {
            Name = name;
        }

        public static TObject FakePersistentInstance(Guid id)
        {
            var result = (TObject)Activator.CreateInstance(typeof(TObject), nonPublic:true);
            result.SetIdBeVerySureYouKnowWhatYouAreDoing(id);
            return result;
        }
        public virtual string Name { get; set; }
    }
}