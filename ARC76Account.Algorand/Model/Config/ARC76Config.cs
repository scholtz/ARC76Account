using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARC76Account.Model.Config
{
    /// <summary>
    /// Class for config parsing for usage in Signer : IOptionsMonitor<ARC76Config> 
    /// </summary>
    public class ARC76Config
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data"></param>
        public ARC76Config(string data)
        {
            this.Data = data;
        }
        /// <summary>
        /// ARC76 Data Password
        /// </summary>
        public string Data { get; set; }
    }
}
