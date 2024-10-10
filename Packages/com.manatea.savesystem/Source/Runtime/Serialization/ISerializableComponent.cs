using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manatea.SaveSystem
{
    public interface ISerializableComponent
    {
        public object Serialize();
        public void Serialize(object obj);
    }
}
