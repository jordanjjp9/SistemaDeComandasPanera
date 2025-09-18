using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaEntidad;

namespace CapaNegocio.MetaStore
{
    public interface IMetaStore
    {
        void Save(string numPed, int secItem, List<DetalleMeta> meta, string hash);
        List<DetalleMeta> Load(string numPed, int secItem, string hash);

        // NUEVO: borrar el sidecar del pedido
        void DeletePedido(string numPed);
    }
}
