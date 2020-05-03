using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateFileFinder
{
    public static class Funcoes
    {

        /// <summary>
        /// Gera um hash SHA1 para um arquivo
        /// </summary>
        /// <param name="input">Caminho do arquivo</param>
        /// <param name="limiteBytes">Limite de bytes (final do arquivo) para gerar o hash. Informe 0 para o arquivo completo</param>
        /// <returns>string hash</returns>
        public static string SHA1FromFile(string input, long limiteBytes = 1024*1024)
        {
            string hash = null;
            using (FileStream fop = File.OpenRead(input))
            {
                if (limiteBytes > 0)
                {
                    if (fop.Length > limiteBytes)
                    {
                        fop.Position = fop.Length - limiteBytes;
                    }
                }

                using (var cryptoProvider = new SHA1CryptoServiceProvider())
                {
                    hash = BitConverter.ToString(cryptoProvider.ComputeHash(fop));
                }
            }
            return hash;
        }

        public static string ToSizeString(this long length)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = length;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string result = String.Format("{0:0.##} {1}", len, sizes[order]);
            return result;
        }

    }
}
