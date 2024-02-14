using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Borelli_BriscolaServer {
    public class Program {
        static void Main(string[] args) {
        }
        public static void WriteLineStream(TcpClient socket, string toWrite) {
            byte[] bytes = Encoding.ASCII.GetBytes($"{toWrite}\n");

            socket.GetStream().Write(bytes, 0, bytes.Length);
        }

        public static string ReadLineStream(TcpClient socket) {
            byte[] bytes = new byte[socket.ReceiveBufferSize];
            int numBytes = socket.GetStream().Read(bytes, 0, socket.ReceiveBufferSize);

            return Encoding.ASCII.GetString(bytes, 0, numBytes).Trim();
        }
    }
}
