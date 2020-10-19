using Include;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magnus {
    class ByteMsg {
        public static bool TryCast(DataType dataType, object data, int identifier, out byte[] bytes) {
            bytes = null;
            if (dataType == DataType.Bytes) {
                byte[] idArray = new byte[4];
                byte[] dataArray = (byte[])data;
                Array.Copy(dataArray, 0, idArray, 0, 4);

                int id = BitConverter.ToInt32(idArray, 0);
                if (identifier == id) {
                    bytes = new byte[dataArray.Length - 4];
                    Array.Copy(dataArray, 4, bytes, 0, dataArray.Length - 4);
                    return true;
                }
            }
            return false;
        }
    }
}
