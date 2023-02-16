using System;
using System.Threading.Tasks;


namespace Virgis
{
    public class MapInitialize : MapInitializePrototype
    {
        protected new void Start()
        {
            base.Start();
            State.instance.map = gameObject;
        }

        public override bool Load(string file)
        {
            throw new NotImplementedException();
        }

        public override void OnLoad()
        {
            throw new NotImplementedException();
        }

        public override Task<RecordSetPrototype> Save(bool all = true)
        {
            throw new NotImplementedException();
        }
    }
}

