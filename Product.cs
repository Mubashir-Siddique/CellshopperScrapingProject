using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellshopperScrapingTask
{
    class Product
    {
        public string NavigationAddress { get; set; }
        public string ProductInfo { get; set; }
        public string ItemCode { get; set; }
        public int ItemId { get; set; }
        public string Price { get; set; }
        public string Description { get; set; }
        public string Compatibility { get; set; }
        public List<byte[]> Pictures { get; set; }
    }
}
